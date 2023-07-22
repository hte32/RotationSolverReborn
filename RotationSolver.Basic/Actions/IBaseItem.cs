﻿namespace RotationSolver.Basic.Actions;

/// <summary>
/// The base item.
/// </summary>
public interface IBaseItem : IAction
{
    /// <summary>
    /// The item can be used.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    bool CanUse(out IAction item);
}
