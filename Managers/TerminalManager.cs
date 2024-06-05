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
                terminalParser = new TerminalParser(InternManager.Instance.NbInternsToDropShip);
            }
            return terminalParser.ParseCommand(command, ref terminal);
        }

        public void SyncPurchaseAndCredits(int nbInternsBought, int credits)
        {
            Plugin.Logger.LogInfo($"base.IsOwner {base.IsOwner}, base.IsHost {base.IsHost}, IsHost {NetworkManager.IsHost}");
            if (base.IsOwner)
            {
                SyncPurchaseAndCreditsFromServerToClientRpc(nbInternsBought, credits);
            }
            else
            {
                SyncPurchaseAndCreditsFromClientToServerRpc(nbInternsBought, credits);
            }
        }

        private void UpdatePurchaseAndCredits(int nbInternsBought, int credits)
        {
            InternManager.Instance.NbInternsToDropShip += nbInternsBought;
            terminalParser.NbInternsAlreadyBought = InternManager.Instance.NbInternsToDropShip;
            Terminal.groupCredits = credits;
        }


        [ServerRpc(RequireOwnership = false)]
        private void SyncPurchaseAndCreditsFromClientToServerRpc(int nbInternsBought, int credits)
        {
            Plugin.Logger.LogInfo($"Client send to server to sync credits to ${credits}, calling ClientRpc...");
            SyncPurchaseAndCreditsFromServerToClientRpc(nbInternsBought, credits);
        }

        [ClientRpc]
        private void SyncPurchaseAndCreditsFromServerToClientRpc(int nbInternsBought, int newCredits)
        {
            Plugin.Logger.LogInfo($"Server send to clients to sync credits to ${newCredits}, client execute...");
            UpdatePurchaseAndCredits(nbInternsBought, newCredits);
        }
    }
}
