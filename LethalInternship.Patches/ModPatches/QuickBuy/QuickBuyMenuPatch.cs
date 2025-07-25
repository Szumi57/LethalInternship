﻿using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.ManagerProviders;

namespace LethalInternship.Patches.ModPatches.QuickBuy
{
    public class QuickBuyMenuPatch
    {
        public static bool RunQuickBuy_Prefix(Terminal __0, ref TerminalNode __result)
        {
            if (TerminalManagerProvider.Instance.GetTerminalPage() == EnumTerminalStates.Info)
            {
                string command = __0.screenText.text.Substring(__0.screenText.text.Length - __0.textAdded);
                TerminalNode? lethalInternshipTerminalNode = TerminalManagerProvider.Instance.ParseLethalInternshipCommands(command, ref __0);
                if (lethalInternshipTerminalNode != null)
                {
                    __result = lethalInternshipTerminalNode;
                }

                return false;
            }
            return true;
        }
    }
}
