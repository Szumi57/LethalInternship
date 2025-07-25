﻿namespace LethalInternship.SharedAbstractions.Enums
{
    /// <summary>
    /// Enumeration for the different terminal state, for navigating in the menu LethalInternship on the terminal
    /// </summary>
    public enum EnumTerminalStates
    {
        Error,
        WaitForMainCommand,
        Info,
        ConfirmCancelPurchase,
        AbortLanding,
        ConfirmCancelAbortLanding,
        Status,
    }
}
