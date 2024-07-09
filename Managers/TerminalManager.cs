using LethalInternship.TerminalAdapter;
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

        private Terminal Terminal = null!;
        private TerminalParser terminalParser = null!;
        private bool helpTextAlreadyAdded;

        private void Awake()
        {
            Instance = this;
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
            if (helpTextAlreadyAdded)
            {
                return;
            }

            TerminalNode helpTerminalNode = terminalNodesList.specialNodes[Const.INDEX_HELP_TERMINALNODE];
            if (helpTerminalNode == null)
            {
                Plugin.Logger.LogError("LethalInternship.Managers.TerminalManager could not find the help terminal node in AddTextToHelpTerminalNode");
                return;
            }

            int indexOther = helpTerminalNode.displayText.IndexOf(Const.STRING_OTHER_HELP);
            if (indexOther < 0)
            {
                Plugin.Logger.LogError($"LethalInternship.Managers.TerminalManager could not find the text {Const.STRING_OTHER_HELP} in AddTextToHelpTerminalNode");
                return;
            }

            helpTerminalNode.displayText = helpTerminalNode.displayText.Insert(indexOther, Const.STRING_INTERNSHIP_PROGRAM_HELP);
            helpTextAlreadyAdded = true;
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

        /// <summary>
        /// Server side, udpate to the client the group credits and the interns ordered after purchase
        /// </summary>
        /// <param name="nbInternsOwned"></param>
        /// <param name="nbInternToDropShip"></param>
        /// <param name="newCredits"></param>
        [ServerRpc(RequireOwnership = false)]
        public void UpdatePurchaseAndCreditsServerRpc(int nbInternsOwned, int nbInternToDropShip, int newCredits)
        {
            Plugin.Logger.LogInfo($"Client send to server to sync credits to ${newCredits}, calling ClientRpc...");
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
            Plugin.Logger.LogInfo($"Server send to clients to sync credits to ${newCredits}, client execute...");
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
        }
    }
}
