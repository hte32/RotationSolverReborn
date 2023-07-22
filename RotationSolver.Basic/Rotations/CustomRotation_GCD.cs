﻿namespace RotationSolver.Basic.Rotations;

public abstract partial class CustomRotation
{
    private IAction GCD(bool helpDefenseAOE, bool helpDefenseSingle)
    {
        IAction act = DataCenter.CommandNextAction;
        if (act is IBaseAction a && a != null && a.IsRealGCD && a.CanUse(out _, CanUseOption.MustUse | CanUseOption.SkipDisable | CanUseOption.EmptyOrSkipCombo)) return act;

        if (EmergencyGCD(out act)) return act;

        var specialType = DataCenter.SpecialType;

        if (RaiseSpell(specialType, out act, false)) return act;

        if (specialType == SpecialCommandType.MoveForward && MoveForwardGCD(out act))
        {
            if (act is IBaseAction b && ObjectHelper.DistanceToPlayer(b.Target) > 5) return act;
        }

        //General Heal
        if ((DataCenter.HPNotFull || ClassJob.GetJobRole() != JobRole.Healer)
            && (DataCenter.InCombat || Service.Config.HealOutOfCombat))
        {
            if ((specialType == SpecialCommandType.HealArea || CanHealAreaSpell) && HealAreaGCD(out act)) return act;
            if ((specialType == SpecialCommandType.HealSingle || CanHealSingleSpell) && HealSingleGCD(out act)) return act;
        }
        if (specialType == SpecialCommandType.DefenseArea && DefenseAreaGCD(out act)) return act;
        if (specialType == SpecialCommandType.DefenseSingle && DefenseSingleGCD(out act)) return act;

        //Auto Defense
        if (DataCenter.SetAutoStatus(AutoStatus.DefenseArea, helpDefenseAOE) && DefenseAreaGCD(out act)) return act;
        if (DataCenter.SetAutoStatus(AutoStatus.DefenseSingle, helpDefenseSingle) && DefenseSingleGCD(out act)) return act;

        //Esuna
        if (DataCenter.SetAutoStatus(AutoStatus.Esuna, (specialType == SpecialCommandType.EsunaStanceNorth 
            || !HasHostilesInRange || Service.Config.EsunaAll)
            && DataCenter.WeakenPeople.Any()  || DataCenter.DyingPeople.Any()))
        {
            if (ClassJob.GetJobRole() == JobRole.Healer && Esuna.CanUse(out act, CanUseOption.MustUse)) return act;
        }

        if (GeneralGCD(out var action)) return action;

        if(DataCenter.PartyMembersMinHP < Service.Config.HealWhenNothingTodoBelow && DataCenter.InCombat)
        {
            if (DataCenter.PartyMembersDifferHP < DataCenter.PartyMembersDifferHP && HealAreaGCD(out act)) return act;
            if (HealSingleGCD(out act)) return act;
        }

        if (Service.Config.RaisePlayerByCasting && RaiseSpell(specialType, out act, true)) return act;

        return null;
    }

    private bool RaiseSpell(SpecialCommandType specialType, out IAction act, bool mustUse)
    {
        act = null;
        if (Raise == null || Player.CurrentMp <= Service.Config.LessMPNoRaise)
        {
            return DataCenter.SetAutoStatus(AutoStatus.Raise, false);
        }

        if (specialType == SpecialCommandType.RaiseShirk && DataCenter.DeathPeopleAll.Any())
        {
            if (Raise.CanUse(out act)) return true;
        }

        if ((Service.Config.RaiseAll ? DataCenter.DeathPeopleAll.Any() : DataCenter.DeathPeopleParty.Any())
            && Raise.CanUse(out act, CanUseOption.IgnoreCastCheck))
        {
            if (HasSwift)
            {
                return DataCenter.SetAutoStatus(AutoStatus.Raise, true);
            }
            else if (mustUse)
            {
                if(Swiftcast.CanUse(out act))
                {
                    return DataCenter.SetAutoStatus(AutoStatus.Raise, true);
                }
                else if(!IsMoving)
                {
                    act = Raise;
                    return DataCenter.SetAutoStatus(AutoStatus.Raise, true);
                }
            }
            else if (Service.Config.GetValue(SettingsCommand.RaisePlayerBySwift) && !Swiftcast.IsCoolingDown 
                && DataCenter.NextAbilityToNextGCD > DataCenter.MinAnimationLock + DataCenter.Ping)
            {
                return DataCenter.SetAutoStatus(AutoStatus.Raise, true);
            }
        }
        return DataCenter.SetAutoStatus(AutoStatus.Raise, false);
    }

    /// <summary>
    /// The emergency gcd with highest priority.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    protected virtual bool EmergencyGCD(out IAction act)
    {
        act = null; return false;
    }

    /// <summary>
    /// Moving forward GCD.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    [RotationDesc(DescType.MoveForwardGCD)]
    protected virtual bool MoveForwardGCD(out IAction act)
    {
        act = null; return false;
    }

    /// <summary>
    /// Heal single GCD.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    [RotationDesc(DescType.HealSingleGCD)]
    protected virtual bool HealSingleGCD(out IAction act)
    {
        act = null; return false;
    }

    /// <summary>
    /// Heal area GCD.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    [RotationDesc(DescType.HealAreaGCD)]
    protected virtual bool HealAreaGCD(out IAction act)
    {
        act = null; return false;
    }

    /// <summary>
    /// Defense single gcd.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    [RotationDesc(DescType.DefenseSingleGCD)]
    protected virtual bool DefenseSingleGCD(out IAction act)
    {
        act = null; return false;
    }

    /// <summary>
    /// Defense area gcd.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    [RotationDesc(DescType.DefenseAreaGCD)]
    protected virtual bool DefenseAreaGCD(out IAction act)
    {
        act = null; return false;
    }

    /// <summary>
    /// General GCD.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    protected abstract bool GeneralGCD(out IAction act);
}
