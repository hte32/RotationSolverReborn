﻿using ECommons.DalamudServices;
using ECommons.ExcelServices;
using RotationSolver.Basic.Traits;

namespace RotationSolver.Basic.Rotations.Basic;

/// <summary>
/// The base class of Dancer.
/// </summary>
public abstract class DNC_Base : CustomRotation
{
    /// <summary>
    /// 
    /// </summary>
    public override MedicineType MedicineType => MedicineType.Dexterity;

    /// <summary>
    /// 
    /// </summary>
    public sealed override Job[] Jobs => new[] { Job.DNC };

    #region Job Gauge
    static DNCGauge JobGauge => Svc.Gauges.Get<DNCGauge>();

    /// <summary>
    /// 
    /// </summary>
    public static bool IsDancing => JobGauge.IsDancing;

    /// <summary>
    /// 
    /// </summary>
    public static byte Esprit => JobGauge.Esprit;

    /// <summary>
    /// 
    /// </summary>
    public static byte Feathers => JobGauge.Feathers;

    /// <summary>
    /// 
    /// </summary>
    public static byte CompletedSteps => JobGauge.CompletedSteps;
    #endregion

    #region Attack Single
    /// <summary>
    /// 1
    /// </summary>
    public static IBaseAction Cascade { get; } = new BaseAction(ActionID.Cascade)
    {
        StatusProvide = new[] { StatusID.SilkenSymmetry }
    };

    /// <summary>
    /// 2
    /// </summary>
    public static IBaseAction Fountain { get; } = new BaseAction(ActionID.Fountain)
    {
        StatusProvide = new[] { StatusID.SilkenFlow }
    };

    /// <summary>
    /// 3
    /// </summary>
    public static IBaseAction ReverseCascade { get; } = new BaseAction(ActionID.ReverseCascade)
    {
        StatusNeed = new[] { StatusID.SilkenSymmetry, StatusID.FlourishingSymmetry },
    };

    /// <summary>
    /// 4
    /// </summary>
    public static IBaseAction FountainFall { get; } = new BaseAction(ActionID.Fountainfall)
    {
        StatusNeed = new[] { StatusID.SilkenFlow, StatusID.FlourishingFlow }
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction FanDance { get; } = new BaseAction(ActionID.FanDance, ActionOption.UseResources)
    {
        ActionCheck = (b, m) => Feathers > 0,
        StatusProvide = new[] { StatusID.ThreefoldFanDance },
    };
    #endregion

    #region Attack Area
    /// <summary>
    /// 1
    /// </summary>
    public static IBaseAction Windmill { get; } = new BaseAction(ActionID.Windmill)
    {
        StatusProvide = Cascade.StatusProvide,
    };

    /// <summary>
    /// 2
    /// </summary>
    public static IBaseAction BladeShower { get; } = new BaseAction(ActionID.Bladeshower)
    {
        StatusProvide = Fountain.StatusProvide,
    };

    /// <summary>
    /// 3
    /// </summary>
    public static IBaseAction RisingWindmill { get; } = new BaseAction(ActionID.RisingWindmill)
    {
        StatusNeed = ReverseCascade.StatusNeed,
    };

    /// <summary>
    /// 4
    /// </summary>
    public static IBaseAction BloodShower { get; } = new BaseAction(ActionID.Bloodshower)
    {
        StatusNeed = FountainFall.StatusNeed,
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction FanDance2 { get; } = new BaseAction(ActionID.FanDanceIi, ActionOption.UseResources)
    {
        ActionCheck = (b, m) => Feathers > 0,
        StatusProvide = new[] { StatusID.ThreefoldFanDance },
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction FanDance3 { get; } = new BaseAction(ActionID.FanDanceIii)
    {
        StatusNeed = FanDance2.StatusProvide,
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction FanDance4 { get; } = new BaseAction(ActionID.FanDanceIv)
    {
        StatusNeed = new[] { StatusID.FourfoldFanDance },
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction SaberDance { get; } = new BaseAction(ActionID.SaberDance, ActionOption.UseResources)
    {
        ActionCheck = (b, m) => Esprit >= 50,
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction StarFallDance { get; } = new BaseAction(ActionID.StarfallDance)
    {
        StatusNeed = new[] { StatusID.FlourishingStarfall },
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction Tillana { get; } = new BaseAction(ActionID.Tillana)
    {
        StatusNeed = new[] { StatusID.FlourishingFinish },
    };
    #endregion

    #region Support
    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction EnAvant { get; } = new BaseAction(ActionID.EnAvant, ActionOption.Friendly | ActionOption.EndSpecial);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction ShieldSamba { get; } = new BaseAction(ActionID.ShieldSamba, ActionOption.Defense)
    {
        ActionCheck = (b, m) => !Player.HasStatus(false, StatusID.Troubadour,
            StatusID.Tactician_1951,
            StatusID.Tactician_2177,
            StatusID.ShieldSamba),
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction CuringWaltz { get; } = new BaseAction(ActionID.CuringWaltz, ActionOption.Heal);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction Improvisation { get; } = new BaseAction(ActionID.Improvisation, ActionOption.Heal);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction ClosedPosition { get; } = new BaseAction(ActionID.ClosedPosition, ActionOption.Buff)
    {
        ChoiceTarget = (Targets, mustUse) =>
        {
            Targets = Targets.Where(b => b.ObjectId != Player.ObjectId && b.CurrentHp != 0 &&
            //Remove Weak
            !b.HasStatus(false, StatusID.Weakness, StatusID.BrinkOfDeath)
            //Remove other partner.
            && (!b.HasStatus(false, StatusID.ClosedPosition_2026) || b.HasStatus(true, StatusID.ClosedPosition_2026)));

            return Targets.GetJobCategory(JobRole.Melee, JobRole.RangedMagical, JobRole.RangedPhysical).FirstOrDefault();
        },
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction Devilment { get; } = new BaseAction(ActionID.Devilment)
    {
        ActionCheck = (b, m) => IsLongerThan(10)
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction Flourish { get; } = new BaseAction(ActionID.Flourish)
    {
        StatusNeed = new[] { StatusID.StandardFinish },
        StatusProvide = new[]
        {
            StatusID.ThreefoldFanDance,
            StatusID.FourfoldFanDance,
        },
        ActionCheck = (b, m) => InCombat,
    };
    #endregion

    #region Step
    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction StandardStep { get; } = new BaseAction(ActionID.StandardStep)
    {
        StatusProvide = new[]
        {
            StatusID.StandardStep,
            StatusID.TechnicalStep,
        },
    };

    /// <summary>
    /// 
    /// </summary>
    public static IBaseAction TechnicalStep { get; } = new BaseAction(ActionID.TechnicalStep)
    {
        StatusNeed = new[]
        {
            StatusID.StandardFinish,
        },
        StatusProvide = StandardStep.StatusProvide,
        ActionCheck = (b, m) => IsLongerThan(20),
    };

    /// <summary>
    /// 
    /// </summary>
    protected static IBaseAction StandardFinish { get; } = new BaseAction(ActionID.DoubleStandardFinish)
    {
        StatusNeed = new[] { StatusID.StandardStep },
        ActionCheck = (b, m) => IsDancing && CompletedSteps == 2 && Service.GetAdjustedActionId(ActionID.StandardStep) == ActionID.DoubleStandardFinish,
    };

    /// <summary>
    /// 
    /// </summary>
    protected static IBaseAction TechnicalFinish { get; } = new BaseAction(ActionID.QuadrupleTechnicalFinish)
    {
        StatusNeed = new[] { StatusID.TechnicalStep },
        ActionCheck = (b, m) => IsDancing && CompletedSteps == 4 && Service.GetAdjustedActionId(ActionID.TechnicalStep) == ActionID.QuadrupleTechnicalFinish,
    };

    private static IBaseAction Emboite { get; } = new BaseAction(ActionID.Emboite)
    {
        ActionCheck = (b, m) => (ActionID)JobGauge.NextStep == ActionID.Emboite,
    };

    private static IBaseAction Entrechat { get; } = new BaseAction(ActionID.Entrechat)
    {
        ActionCheck = (b, m) => (ActionID)JobGauge.NextStep == ActionID.Entrechat,
    };

    private static IBaseAction Jete { get; } = new BaseAction(ActionID.Jete)
    {
        ActionCheck = (b, m) => (ActionID)JobGauge.NextStep == ActionID.Jete,
    };

    private static IBaseAction Pirouette { get; } = new BaseAction(ActionID.Pirouette)
    {
        ActionCheck = (b, m) => (ActionID)JobGauge.NextStep == ActionID.Pirouette,
    };

    /// <summary>
    /// Finish the dance.
    /// </summary>
    /// <param name="act"></param>
    /// <param name="finishNow">Finish the dance as soon as possible</param>
    /// <returns></returns>
    protected static bool DanceFinishGCD(out IAction act, bool finishNow = false)
    {
        if (Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2)
        {
            if (StandardFinish.CanUse(out act, CanUseOption.MustUse))
            {
                return true;
            }
            if (Player.WillStatusEnd(1, true, StatusID.StandardStep, StatusID.StandardFinish) || finishNow)
            {
                act = StandardStep;
                return true;
            }
            return false;
        }

        if (Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4)
        {
            if (TechnicalFinish.CanUse(out act, CanUseOption.MustUse))
            {
                return true;
            }
            if (Player.WillStatusEnd(1, true, StatusID.TechnicalStep) || finishNow)
            {
                act = TechnicalStep;
                return true;
            }
            return false;
        }

        act = null;
        return false;
    }

    /// <summary>
    /// Do the dancing steps.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    protected static bool ExecuteStepGCD(out IAction act)
    {
        if (!IsDancing)
        {
            act = null;
            return false;
        }

        if (Emboite.CanUse(out act)) return true;
        if (Entrechat.CanUse(out act)) return true;
        if (Jete.CanUse(out act)) return true;
        if (Pirouette.CanUse(out act)) return true;
        return false;
    }
    #endregion

    #region Traits
    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait IncreasedActionDamage { get; } = new BaseTrait(251);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait FourfoldFantasy { get; } = new BaseTrait(252);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait IncreasedActionDamage2 { get; } = new BaseTrait(253);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait EnhancedEnAvant { get; } = new BaseTrait(254);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait EspritTrait { get; } = new BaseTrait(255);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait EnhancedEnAvant2 { get; } = new BaseTrait(256);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait EnhancedTechnicalFinish { get; } = new BaseTrait(453);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait EnhancedEsprit { get; } = new BaseTrait(454);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait EnhancedFlourish { get; } = new BaseTrait(455);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait EnhancedShieldSamba { get; } = new BaseTrait(456);

    /// <summary>
    /// 
    /// </summary>
    public static IBaseTrait EnhancedDevilment { get; } = new BaseTrait(457);

    #endregion

    private protected override IBaseAction LimitBreak => CrimsonLotus;

    /// <summary>
    /// LB
    /// </summary>
    public static IBaseAction CrimsonLotus { get; } = new BaseAction(ActionID.CrimsonLotus)
    {
        ActionCheck = (b, m) => LimitBreakLevel == 3,
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    [RotationDesc(ActionID.EnAvant)]
    protected sealed override bool MoveForwardAbility(out IAction act)
    {
        if (EnAvant.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    [RotationDesc(ActionID.CuringWaltz, ActionID.Improvisation)]
    protected sealed override bool HealAreaAbility(out IAction act)
    {
        if (CuringWaltz.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
        if (Improvisation.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    [RotationDesc(ActionID.ShieldSamba)]
    protected sealed override bool DefenseAreaAbility(out IAction act)
    {
        if (ShieldSamba.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
        return false;
    }
}
