using LethalInternship.Core.TerminalAdapter;
using LethalInternship.Core.TerminalAdapter.TerminalStates;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Managers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Managers
{
    /// <summary>
    /// Manager in charge of initializing the terminal parser (for adding LethalInternship pages to the terminal) and synchronize clients
    /// </summary>
    public class TerminalManager : NetworkBehaviour, ITerminalManager
    {
        public static TerminalManager Instance { get; private set; } = null!;

        public string StringIntershipProgram = null!;
        public string CommandIntershipProgram = null!;

        private Terminal Terminal = null!;
        private TerminalParser terminalParser = null!;

        private void Awake()
        {
            Instance = this;
            this.CommandIntershipProgram = PluginRuntimeProvider.Context.Config.TitleInHelpMenu.ToLower();
            this.StringIntershipProgram = PluginRuntimeProvider.Context.Config.GetTitleInternshipProgram();
        }

        /// <summary>
        /// Get the base game component Terminal actually loaded.
        /// </summary>
        /// <returns>Terminal if found, else returns null</returns>
        public Terminal GetTerminal()
        {
            if (Terminal == null)
            {
                Terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            }
            return Terminal;
        }

        /// <summary>
        /// Insert the lines <see cref="Const.STRING_INTERNSHIP_PROGRAM_HELP"><c>Const.STRING_INTERNSHIP_PROGRAM_HELP</c></see>
        /// in the help page of the terminal
        /// </summary>
        /// <param name="terminalNodesList">List of all terminal nodes from the base game terminal</param>
        public void AddTextToHelpTerminalNode(TerminalNodesList terminalNodesList)
        {
            TerminalNode helpTerminalNode = terminalNodesList.specialNodes[TerminalConst.INDEX_HELP_TERMINALNODE];
            if (helpTerminalNode == null)
            {
                PluginLoggerHook.LogError?.Invoke("LethalInternship.Managers.TerminalManager could not find the help terminal node in AddTextToHelpTerminalNode");
                return;
            }

            int indexOther = helpTerminalNode.displayText.IndexOf(TerminalConst.STRING_OTHER_HELP);
            if (indexOther < 0)
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Managers.TerminalManager could not find the text {TerminalConst.STRING_OTHER_HELP} in AddTextToHelpTerminalNode");
                return;
            }

            int indexMenuInternAlreadyAdded = helpTerminalNode.displayText.IndexOf(StringIntershipProgram);
            if (indexMenuInternAlreadyAdded > 0)
            {
                // Text already added
                return;
            }

            helpTerminalNode.displayText = helpTerminalNode.displayText.Insert(indexOther, StringIntershipProgram);
        }

        /// <summary>
        /// Read and interpret the command using the <c>TerminalParser</c>
        /// </summary>
        /// <param name="command"></param>
        /// <param name="terminal"></param>
        /// <returns>A <c>TerminalNode</c> from the pages for LethalInternship, or null which default for the base game terminal</returns>
        public TerminalNode? ParseLethalInternshipCommands(string command, ref Terminal terminal)
        {
            if (terminalParser == null)
            {
                terminalParser = new TerminalParser();
            }
            return terminalParser.ParseCommand(command, ref terminal);
        }

        public void ResetTerminalParser()
        {
            if (terminalParser == null)
            {
                terminalParser = new TerminalParser();
            }
            else
            {
                terminalParser.TerminalState = new WaitForMainCommandPage(terminalParser.TerminalState);
            }
        }

        public EnumTerminalStates GetTerminalPage()
        {
            if (terminalParser == null)
            {
                return EnumTerminalStates.WaitForMainCommand;
            }
            else
            {
                return terminalParser.TerminalState.GetTerminalState();
            }
        }

        #region Sync UpdatePurchaseAndCredits

        [ServerRpc(RequireOwnership = false)]
        public void BuyRandomInternsServerRpc(int newCredits, int nbInternsBought)
        {
            int[] idsRandomIdentities = new int[nbInternsBought];
            for (int i = 0; i < nbInternsBought; i++)
            {
                int newIdentityToSpawn = IdentityManager.Instance.GetNewIdentityToSpawn();
                if (newIdentityToSpawn < 0)
                {
                    PluginLoggerHook.LogInfo?.Invoke($"Try to buy number {i + 1} intern error, no more intern identities available.");
                    return;
                }

                IInternIdentity internIdentity = IdentityManager.Instance.InternIdentities[newIdentityToSpawn];
                internIdentity.Status = EnumStatusIdentity.ToDrop;
                internIdentity.Hp = internIdentity.Alive ? internIdentity.Hp : internIdentity.HpMax;
                idsRandomIdentities[i] = newIdentityToSpawn;
            }

            BuyRandomInternsClientRpc(newCredits, idsRandomIdentities);
        }

        [ClientRpc]
        private void BuyRandomInternsClientRpc(int newCredits, int[] idsRandomIdentities)
        {
            BuyIntern(newCredits, idsRandomIdentities);
        }

        /// <summary>
        /// Server side, udpate to the client the group credits and the interns ordered
        /// </summary>
        /// <param name="newCredits"></param>
        [ServerRpc(RequireOwnership = false)]
        public void BuySpecificInternServerRpc(int newCredits, int idIdentityIntern)
        {
            BuySpecificInternClientRpc(newCredits, idIdentityIntern);
        }

        /// <summary>
        /// Client side, udpate the group credits and the interns ordered 
        /// </summary>
        /// <param name="newCredits"></param>
        [ClientRpc]
        private void BuySpecificInternClientRpc(int newCredits, int idIdentityIntern)
        {
            BuyIntern(newCredits, new int[] { idIdentityIntern });
        }

        private void BuyIntern(int newCredits, int[] idsRandomIdentities)
        {
            GetTerminal().groupCredits = newCredits;
            GetTerminal().terminalAudio.PlayOneShot(GetTerminal().syncedAudios[TerminalConst.INDEX_AUDIO_BOUGHT_ITEM]);

            if (!IsServer)
            {
                // Check for size or identities
                int idMax = idsRandomIdentities.Max();
                if (idMax + 1 > IdentityManager.Instance.InternIdentities.Length)
                {
                    IdentityManager.Instance.ExpandWithNewDefaultIdentities(idMax + 1 - IdentityManager.Instance.InternIdentities.Length);
                }
            }

            for (int i = 0; i < idsRandomIdentities.Length; i++)
            {
                IInternIdentity internIdentity = IdentityManager.Instance.InternIdentities[idsRandomIdentities[i]];
                internIdentity.Status = EnumStatusIdentity.ToDrop;
                internIdentity.Hp = internIdentity.Alive ? internIdentity.Hp : internIdentity.HpMax;
            }
        }

        #endregion

        #region Sync landing status

        [ServerRpc(RequireOwnership = false)]
        public void SyncLandingStatusServerRpc(bool landingAllowed)
        {
            SyncLandingStatusClientRpc(landingAllowed);
        }

        [ClientRpc]
        private void SyncLandingStatusClientRpc(bool landingAllowed)
        {
            PluginLoggerHook.LogInfo?.Invoke($"Client: sync landing status to allowed : {landingAllowed}, client execute...");
            InternManager.Instance.LandingStatusAllowed = landingAllowed;
        }

        #endregion
    }
}
