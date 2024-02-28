﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using RotationSolver.Basic.Configuration;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace RotationSolver;

public static class Watcher
{
#if DEBUG
    private unsafe delegate bool OnUseAction(ActionManager* manager, ActionType actionType, uint actionID, ulong targetID, uint a4, uint a5, uint a6, void* a7);
    private static Hook<OnUseAction>? _useActionHook;
#endif

    private unsafe delegate long ProcessObjectEffect(GameObject* a1, ushort a2, ushort a3, long a4);
    private static Hook<ProcessObjectEffect>? _processObjectEffectHook;

    private delegate IntPtr ActorVfxCreate(string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7);
    private static Hook<ActorVfxCreate>? _actorVfxCreateHook;

    private static ICallGateSubscriber<object, object>? IpcSubscriber;

    public static void Enable()
    {
        unsafe
        {
#if DEBUG
            _useActionHook = Svc.Hook.HookFromSignature<OnUseAction>("E8 ?? ?? ?? ?? EB 64 B1 01", UseActionDetour);
            //_useActionHook.Enable();
#endif
            //From https://github.com/PunishXIV/Splatoon/blob/main/Splatoon/Memory/ObjectEffectProcessor.cs#L14
            _processObjectEffectHook = Svc.Hook.HookFromSignature<ProcessObjectEffect>("40 53 55 56 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B7 FA", ProcessObjectEffectDetour);
            _processObjectEffectHook.Enable();

            //From https://github.com/0ceal0t/Dalamud-VFXEditor/blob/main/VFXEditor/Interop/Constants.cs#L12
            _actorVfxCreateHook = Svc.Hook.HookFromSignature<ActorVfxCreate>("40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8", ActorVfxNewHandler);
            _actorVfxCreateHook.Enable();
        }
        IpcSubscriber = Svc.PluginInterface.GetIpcSubscriber<object, object>("PingPlugin.Ipc");
        IpcSubscriber.Subscribe(UpdateRTTDetour);

        ActionEffect.ActionEffectEvent += ActionFromEnemy;
        ActionEffect.ActionEffectEvent += ActionFromSelf;
        MapEffect.Init((a1, position, param1, param2) =>
        {
            if (DataCenter.MapEffects.Count >= 64)
            {
                DataCenter.MapEffects.TryDequeue(out _);
            }

            var effect = new MapEffectData(position, param1, param2);
            DataCenter.MapEffects.Enqueue(effect);
#if DEBUG
            Svc.Log.Debug(effect.ToString());
#endif
        });

        Svc.GameNetwork.NetworkMessage += GameNetwork_NetworkMessage;
        Svc.Chat.ChatMessage += Chat_ChatMessage;
    }

    private static void Chat_ChatMessage(Dalamud.Game.Text.XivChatType type, uint senderId, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        foreach (var item in DataCenter.TimelineItems)
        {
            if (item.Time < DataCenter.RaidTimeRaw) continue;
            if (item.Type is not TimelineType.GameLog) continue;

            var typeString = ((uint)type).ToString("X4");
            if (!new Regex(item["code"]).IsMatch(typeString)) continue;

            //TODO: multi language.
            if (!new Regex(item["line"]).IsMatch(message.TextValue)) continue;
            item.UpdateRaidTimeOffset();
            break;
        }
    }

    private static void GameNetwork_NetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, Dalamud.Game.Network.NetworkMessageDirection direction)
    {
        if (direction != Dalamud.Game.Network.NetworkMessageDirection.ZoneDown) return;
        OpCode op = (OpCode)opCode;

        switch (op)
        {
            case OpCode.SystemLogMessage:
                OnSystemLogMessage(dataPtr);
                break;
            //case OpCode.ActorControlTarget:
            //    var bytes = new byte[32];
            //    Marshal.Copy(dataPtr, bytes, 0, 32);
            //    Svc.Log.Debug("ActorControlTarget: " + HexString(bytes));
            //    break;
            //case OpCode.ActorControlSelf:
            //    bytes = new byte[32];
            //    Marshal.Copy(dataPtr, bytes, 0, 32);
            //    Svc.Log.Debug("ActorControlSelf: " + HexString(bytes));
            //    break;
            //case OpCode.ActorControl:
            //    OnActorControl(dataPtr);
            //    break;
        }
    }

    //private static void OnActorControl(IntPtr dataPtr)
    //{
    //    foreach (var item in DataCenter.TimelineItems)
    //    {
    //        if (item.Time < DataCenter.RaidTimeRaw) continue;
    //        if (item.Type is not TimelineType.ActorControl) continue;
    //        //if (!item.IsIdMatched(ReadNumber(dataPtr, 4))) continue;

    //        //var param1 = item["param1"];
    //        //if (!string.IsNullOrEmpty(param1))
    //        //{
    //        //    if (!new Regex(param1).IsMatch(ReadNumber(dataPtr, 12).ToString("X")))
    //        //    {
    //        //        continue;
    //        //    }
    //        //}
    //        //item.UpdateRaidTimeOffset();
    //        break;
    //    }

    //    var bytes = new byte[32];
    //    Marshal.Copy(dataPtr, bytes, 0, 32);
    //    Svc.Log.Debug("ActorControl: " + HexString(bytes));
    //}

    private static void OnSystemLogMessage(IntPtr dataPtr)
    {
        foreach (var item in DataCenter.TimelineItems)
        {
            if (item.Time < DataCenter.RaidTimeRaw) continue;
            if (item.Type is not TimelineType.SystemLogMessage) continue;
            if (!item.IsIdMatched(ReadNumber(dataPtr, 4))) continue;

            var param1 = item["param1"];
            if (!string.IsNullOrEmpty(param1))
            {
                if(!new Regex(param1).IsMatch(ReadNumber(dataPtr, 12).ToString("X")))
                {
                    continue;
                }
            }
            item.UpdateRaidTimeOffset();
            break;
        }
    }

    private unsafe static uint ReadNumber(IntPtr dataPtr, int offset)
    {
        return *(uint*)(dataPtr + offset);
    }

    private static string HexString(byte[] bytes)
    {
        var str = Convert.ToHexString(bytes);

        string result = string.Empty;
        for (int i = 0; i < str.Length; i++)
        {
            if (i % 4 == 0)
            {
                result += " ";
            }
            result += str[i];
        }
        return result;
    }

    public static void Disable()
    {
#if DEBUG
        _useActionHook?.Dispose();
#endif
        _processObjectEffectHook?.Disable();
        IpcSubscriber?.Unsubscribe(UpdateRTTDetour);
        MapEffect.Dispose();
        ActionEffect.ActionEffectEvent -= ActionFromEnemy;
        ActionEffect.ActionEffectEvent -= ActionFromSelf;
        Svc.GameNetwork.NetworkMessage -= GameNetwork_NetworkMessage;
        Svc.Chat.ChatMessage -= Chat_ChatMessage;
    }

    private static IntPtr ActorVfxNewHandler(string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7)
    {
        try
        {
            if (!path.StartsWith("vfx/common/eff/", StringComparison.OrdinalIgnoreCase))
            {
                if (DataCenter.VfxNewData.Count >= 64)
                {
                    DataCenter.VfxNewData.TryDequeue(out _);
                }

                var obj = Svc.Objects.CreateObjectReference(a2);
                var effect = new VfxNewData(obj?.ObjectId ?? Dalamud.Game.ClientState.Objects.Types.GameObject.InvalidGameObjectId, path);
                DataCenter.VfxNewData.Enqueue(effect);
#if DEBUG
                Svc.Log.Debug(effect.ToString());
#endif
            }
        }
        catch (Exception e)
        {
            Svc.Log.Warning(e, "Failed to load the vfx value!");
        }

        return _actorVfxCreateHook!.Original(path, a2, a3, a4, a5, a6, a7);
    }

    private static unsafe long ProcessObjectEffectDetour(GameObject* a1, ushort a2, ushort a3, long a4)
    {
        try
        {
            if (DataCenter.ObjectEffects.Count >= 64)
            {
                DataCenter.ObjectEffects.TryDequeue(out _);
            }

            var effect = new ObjectEffectData(a1->ObjectID, a2, a3);
            DataCenter.ObjectEffects.Enqueue(effect);

            Svc.Objects.CreateObjectReference((nint)a1);
#if DEBUG
            Svc.Log.Debug(effect.ToString());
#endif
        }
        catch (Exception e)
        {
            Svc.Log.Warning(e, "Failed to execute the object effect!");
        }
        return _processObjectEffectHook!.Original(a1, a2, a3, a4);
    }

#if DEBUG
    private static unsafe bool UseActionDetour(ActionManager* manager, ActionType actionType, uint actionID, ulong targetID, uint a4, uint a5, uint a6, void* a7)
    {
        try
        {
            Svc.Chat.Print($"Type: {actionType}, ID: {actionID}, Tar: {targetID}, 4: {a4}, 5: {a5}, 6: {a6}");
        }
        catch (Exception e)
        {
            Svc.Log.Warning(e, "Failed to detour actions");
        }
        return _useActionHook!.Original(manager, actionType, actionID, targetID, a4, a5, a6, a7);
    }
#endif

    private static void UpdateRTTDetour(dynamic obj)
    {
        Svc.Log.Verbose($"LastRTT:{obj.LastRTT}");
        DataCenter.RTT = (long)obj.LastRTT / 1000f;
    }

    public static string ShowStrSelf { get; private set; } = string.Empty;
    public static string ShowStrEnemy { get; private set; } = string.Empty;

    private static void ActionFromEnemy(ActionEffectSet set)
    {
        foreach (var item in DataCenter.TimelineItems)
        {
            if (item.Time < DataCenter.RaidTimeRaw) continue;
            if (item.Type is not TimelineType.Ability) continue;
            if (!item.IsIdMatched(set.Action?.RowId ?? 0)) continue;

            item.UpdateRaidTimeOffset();
            break;
        }

        //Check Source.
        var source = set.Source;
        if (source == null) return;
        if (source is not BattleChara battle) return;
        if (battle is PlayerCharacter) return;
        if (battle.SubKind == 9) return; //Friend!
        if (Svc.Objects.SearchById(battle.ObjectId) is PlayerCharacter) return;

        var damageRatio = set.TargetEffects
            .Where(e => e.TargetID == Player.Object.ObjectId)
            .SelectMany(e => new EffectEntry[]
            {
                e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7]
            })
            .Where(e => e.type == ActionEffectType.Damage)
            .Sum(e => (float)e.value / Player.Object.MaxHp);

        DataCenter.AddDamageRec(damageRatio);

        ShowStrEnemy = $"Damage Ratio: {damageRatio}\n{set}";

        foreach (var effect in set.TargetEffects)
        {
            if (effect.TargetID != Player.Object.ObjectId) continue;
            if (effect.GetSpecificTypeEffect(ActionEffectType.Knockback, out var entry))
            {
                var knock = Svc.Data.GetExcelSheet<Knockback>()?.GetRow(entry.value);
                if (knock != null)
                {
                    DataCenter.KnockbackStart = DateTime.Now;
                    DataCenter.KnockbackFinished = DateTime.Now + TimeSpan.FromSeconds(knock.Distance / (float)knock.Speed);
                }
                break;
            }
        }

        if (set.Header.ActionType == ActionType.Action && DataCenter.PartyMembers.Length >= 4 && set.Action?.Cast100ms > 0)
        {
            var type = set.Action.GetActionCate();

            if (type is ActionCate.Spell or ActionCate.Weaponskill or ActionCate.Ability)
            {
                if (set.TargetEffects.Count(e =>
                    DataCenter.PartyMembers.Any(p => p.ObjectId == e.TargetID)
                    && e.GetSpecificTypeEffect(ActionEffectType.Damage, out var effect)
                    && (effect.value > 0 || (effect.param0 & 6) == 6))
                    == DataCenter.PartyMembers.Length)
                {
                    if (Service.Config.RecordCastingArea)
                    {
                        OtherConfiguration.HostileCastingArea.Add(set.Action.RowId);
                        OtherConfiguration.SaveHostileCastingArea();
                    }
                }
            }
        }
    }

    private static void ActionFromSelf(ActionEffectSet set)
    {
        if (set.Source.ObjectId != Player.Object.ObjectId) return;
        if (set.Header.ActionType != ActionType.Action && set.Header.ActionType != ActionType.Item) return;
        if (set.Action == null) return;
        if ((ActionCate)set.Action.ActionCategory.Value!.RowId == ActionCate.Autoattack) return;

        var id = set.Action.RowId;
        if (!set.Action.IsRealGCD() && (set.Action.ClassJob.Row > 0 || Enum.IsDefined((ActionID)id)))
        {
            OtherConfiguration.AnimationLockTime[id] = set.Header.AnimationLockTime;
        }

        if (!set.TargetEffects.Any()) return;

        var action = set.Action;
        var tar = set.Target;

        if (tar == null || action == null) return;

        //Record
        DataCenter.AddActionRec(action);
        ShowStrSelf = set.ToString();

        DataCenter.HealHP = set.GetSpecificTypeEffect(ActionEffectType.Heal);
        DataCenter.ApplyStatus = set.GetSpecificTypeEffect(ActionEffectType.ApplyStatusEffectTarget);
        foreach (var effect in set.GetSpecificTypeEffect(ActionEffectType.ApplyStatusEffectSource))
        {
            DataCenter.ApplyStatus[effect.Key] = effect.Value;
        }
        DataCenter.MPGain = (uint)set.GetSpecificTypeEffect(ActionEffectType.MpGain).Where(i => i.Key == Player.Object.ObjectId).Sum(i => i.Value);
        DataCenter.EffectTime = DateTime.Now;
        DataCenter.EffectEndTime = DateTime.Now.AddSeconds(set.Header.AnimationLockTime + 1);

        foreach (var effect in set.TargetEffects)
        {
            if (!effect.GetSpecificTypeEffect(ActionEffectType.Damage, out _)) continue;

            if (DataCenter.AttackedTargets.Any(i => i.id == effect.TargetID)) continue;

            if (DataCenter.AttackedTargets.Count >= DataCenter.ATTACKED_TARGETS_COUNT)
            {
                DataCenter.AttackedTargets.Dequeue();
            }
            DataCenter.AttackedTargets.Enqueue((effect.TargetID, DateTime.Now));
        }

        //Macro
        foreach (var item in Service.Config.Events)
        {
            if (!new Regex(item.Name).Match(action.Name).Success) continue;
            if (item.AddMacro(tar)) break;
        }
    }
}
