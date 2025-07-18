namespace LethalInternship.SharedAbstractions.Hooks.ReviveCompanyHooks
{
    public delegate void ReviveCompanySetPlayerDiedAtDelegate(int playerClientId);
    public delegate void UpdateReviveCompanyRemainingRevivesDelegate(string identityName);

    public class ReviveCompanyHook
    {
        /// <summary>
        /// Method separate to not load type of plugin of revive company if mod is not loaded in modpack
        /// </summary>
        /// <param name="playerClientId"></param>
        public static ReviveCompanySetPlayerDiedAtDelegate? ReviveCompanySetPlayerDiedAt;
        public static UpdateReviveCompanyRemainingRevivesDelegate? UpdateReviveCompanyRemainingRevives;
    }
}
