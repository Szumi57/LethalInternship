using LethalInternship.TerminalAdapter;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Managers
{
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

        public Terminal GetTerminal()
        {
            if (Terminal == null)
            {
                Terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            }
            return Terminal;
        }

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

        public TerminalNode? ParseLethalInternshipCommands(string command, ref Terminal terminal)
        {
            if (terminalParser == null)
            {
                terminalParser = new TerminalParser();
            }
            return terminalParser.ParseCommand(command, ref terminal);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PurchaseAndCreditsServerRpc(int nbInternsOwned, int nbInternToDropShip, int newCredits)
        {
            Plugin.Logger.LogInfo($"Client send to server to sync credits to ${newCredits}, calling ClientRpc...");
            PurchaseAndCreditsClientRpc(nbInternsOwned, nbInternToDropShip, newCredits);
        }

        [ClientRpc]
        private void PurchaseAndCreditsClientRpc(int nbInternsOwned, int nbInternToDropShip, int newCredits)
        {
            Plugin.Logger.LogInfo($"Server send to clients to sync credits to ${newCredits}, client execute...");
            UpdatePurchaseAndCredits(nbInternsOwned, nbInternToDropShip, newCredits);
        }

        private void UpdatePurchaseAndCredits(int nbInternsOwned, int nbInternToDropShip, int newCredits)
        {
            InternManager.Instance.UpdateInternsOrdered(nbInternsOwned, nbInternToDropShip);
            GetTerminal().groupCredits = newCredits;
        }
    }
}
