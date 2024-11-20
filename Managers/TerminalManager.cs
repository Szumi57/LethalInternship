using LethalInternship.Constants;
using LethalInternship.TerminalAdapter;
using LethalInternship.TerminalAdapter.TerminalStates;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Managers
{
    /// <summary>
    /// Manager in charge of initializing the terminal parser (for adding LethalInternship pages to the terminal) and synchronize clients
    /// </summary>
    internal class TerminalManager : NetworkBehaviour
    {
        public static TerminalManager Instance { get; private set; } = null!;

        public string StringIntershipProgram = null!;
        public string CommandIntershipProgram = null!;

        private Terminal Terminal = null!;
        private TerminalParser terminalParser = null!;

        private void Awake()
        {
            Instance = this;
            this.CommandIntershipProgram = Plugin.Config.TitleInHelpMenu.Value.ToLower();
            this.StringIntershipProgram = Plugin.Config.GetTitleInternshipProgram();
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
                Plugin.LogError("LethalInternship.Managers.TerminalManager could not find the help terminal node in AddTextToHelpTerminalNode");
                return;
            }

            int indexOther = helpTerminalNode.displayText.IndexOf(TerminalConst.STRING_OTHER_HELP);
            if (indexOther < 0)
            {
                Plugin.LogError($"LethalInternship.Managers.TerminalManager could not find the text {TerminalConst.STRING_OTHER_HELP} in AddTextToHelpTerminalNode");
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

        public Enums.EnumTerminalStates GetTerminalPage()
        {
            if (terminalParser == null)
            {
                return Enums.EnumTerminalStates.WaitForMainCommand;
            }
            else
            {
                return terminalParser.TerminalState.GetTerminalState();
            }
        }

        #region Sync UpdatePurchaseAndCredits

        /// <summary>
        /// Server side, udpate to the client the group credits and the interns ordered after purchase
        /// </summary>
        /// <param name="nbInternsOwned"></param>
        /// <param name="nbInternToDropShip"></param>
        /// <param name="newCredits"></param>
        [ServerRpc(RequireOwnership = false)]
        public void UpdatePurchaseAndCreditsServerRpc(int nbInternsOwned, int nbInternToDropShip, int newCredits)
        {
            UpdatePurchaseAndCreditsClientRpc(nbInternsOwned, nbInternToDropShip, newCredits);
        }

        /// <summary>
        /// Client side, udpate the group credits and the interns ordered after purchase
        /// </summary>
        /// <param name="nbInternsOwned"></param>
        /// <param name="nbInternToDropShip"></param>
        /// <param name="newCredits"></param>
        [ClientRpc]
        private void UpdatePurchaseAndCreditsClientRpc(int nbInternsOwned, int nbInternToDropShip, int newCredits)
        {
            UpdatePurchaseAndCredits(nbInternsOwned, nbInternToDropShip, newCredits);
        }

        /// <summary>
        /// Udpate the group credits and the interns ordered after purchase
        /// </summary>
        /// <param name="nbInternsOwned"></param>
        /// <param name="nbInternToDropShip"></param>
        /// <param name="newCredits"></param>
        private void UpdatePurchaseAndCredits(int nbInternsOwned, int nbInternToDropShip, int newCredits)
        {
            InternManager.Instance.UpdateInternsOrdered(nbInternsOwned, nbInternToDropShip);
            GetTerminal().groupCredits = newCredits;
            GetTerminal().terminalAudio.PlayOneShot(GetTerminal().syncedAudios[TerminalConst.INDEX_AUDIO_BOUGHT_ITEM]);
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
            Plugin.LogInfo($"Client: sync landing status to allowed : {landingAllowed}, client execute...");
            InternManager.Instance.LandingStatusAllowed = landingAllowed;
        }

        #endregion
    }
}
