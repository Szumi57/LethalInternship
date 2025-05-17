using LethalInternship.SharedAbstractions.Hooks.ReviveCompanyHooks;

namespace LethalInternship.Patches.ModPatches.ReviveCompany
{
    public class ReviveCompanyUtils
    {
        public static void Init()
        {
            ReviveCompanyHook.ReviveCompanySetPlayerDiedAt = ReviveCompanySetPlayerDiedAt;
            ReviveCompanyHook.UpdateReviveCompanyRemainingRevives = UpdateReviveCompanyRemainingRevives;
        }

        public static void ReviveCompanySetPlayerDiedAt(int playerClientId)
        {
            if (OPJosMod.ReviveCompany.GlobalVariables.ModActivated)
            {
                OPJosMod.ReviveCompany.GeneralUtil.SetPlayerDiedAt(playerClientId);
            }
        }

        public static void UpdateReviveCompanyRemainingRevives(string identityName)
        {
            OPJosMod.ReviveCompany.GlobalVariables.RemainingRevives--;
            if (OPJosMod.ReviveCompany.GlobalVariables.RemainingRevives < 100)
            {
                HUDManager.Instance.DisplayTip(identityName + " was revived", string.Format("{0} revives remain!", OPJosMod.ReviveCompany.GlobalVariables.RemainingRevives), false, false, "LC_Tip1");
            }
        }
    }
}
