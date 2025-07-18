using LethalInternship.Core.Managers;
using LethalInternship.Core.TerminalAdapter.TerminalStates;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.TerminalHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;

namespace LethalInternship.Core.TerminalAdapter
{
    /// <summary>
    /// Class used for holding the current state of the intern shop terminal pages<br/>
    /// and calling state method for parsing command and displaying current page.
    /// </summary>
    public class TerminalParser
    {
        public TerminalState TerminalState = null!;

        /// <summary>
        /// Constructor, set the state/page to the default <c>WaitForMainCommandPage</c>
        /// </summary>
        public TerminalParser()
        {
            TerminalState = new WaitForMainCommandPage(this);
        }

        /// <summary>
        /// Main method, using the current state/page for parsing and displaying on the terminal
        /// </summary>
        /// <returns></returns>
        public TerminalNode? ParseCommand(string command, ref Terminal terminal)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return null;
            }

            command = TerminalHook.RemovePunctuation_ReversePatch?.Invoke(terminal, command) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(command))
            {
                return null;
            }

            string[] words = command.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return null;
            }

            if (TerminalState.ParseCommandValid(words))
            {
                TerminalNode? terminalNode = TerminalState.DisplayNode();
                if (terminalNode != null)
                {
                    terminalNode.displayText += "\n";
                }
                return terminalNode;
            }
            else
            {
                TerminalState = new WaitForMainCommandPage(this);
                return null;
            }
        }

        public bool IsMatchWord(string word, string match)
        {
            return match.Contains(word) && match[0] == word[0];
        }

        public bool BuyCommandSetNextPage(string[] words)
        {
            // Can buy ?
            if (TerminalManager.Instance.GetTerminal().groupCredits < PluginRuntimeProvider.Context.Config.InternPrice)
            {
                TerminalState = new ErrorPage(TerminalState, EnumErrorTypeTerminalPage.NotEnoughCredits);
                return true;
            }
            else if (IdentityManager.Instance.GetNbIdentitiesAvailable() <= 0)
            {
                TerminalState = new ErrorPage(TerminalState, EnumErrorTypeTerminalPage.NoMoreInterns);
                return true;
            }
            else if (StartOfRound.Instance.shipIsLeaving)
            {
                TerminalState = new ErrorPage(TerminalState, EnumErrorTypeTerminalPage.ShipLeavingMoon);
                return true;
            }

            if (words.Length <= 1)
            {
                TerminalState = new ConfirmCancelPurchasePage(TerminalState, 1);
                return true;
            }

            // secondWord number
            if (!string.IsNullOrWhiteSpace(words[1])
                && int.TryParse(words[1], out int nbOrdered)
                && nbOrdered > 0)
            {
                TerminalState = new ConfirmCancelPurchasePage(TerminalState, nbOrdered);
                return true;
            }

            // secondWord word
            // Looking for identities
            string sentence = string.Empty;
            for (int i = 1; i < words.Length; i++)
            {
                sentence += words[i].Trim();
            }

            if (sentence.Length <= 2)
            {
                TerminalState = new ConfirmCancelPurchasePage(TerminalState, 1);
                return true;
            }

            string[] nameIdentities = IdentityManager.Instance.GetIdentitiesNamesLowerCaseWithoutSpace();
            for (int i = sentence.Length - 1; i >= 1; i--)
            {
                string search = sentence.Substring(0, i + 1);
                for (int j = 0; j < nameIdentities.Length; j++)
                {
                    if (nameIdentities[j].Contains(search))
                    {
                        if (!IdentityManager.Instance.InternIdentities[j].Alive
                            && !StartOfRound.Instance.inShipPhase)
                        {
                            // No revive on moon
                            TerminalState = new ErrorPage(TerminalState, EnumErrorTypeTerminalPage.InternDead);
                            return true;
                        }
                        if (IdentityManager.Instance.InternIdentities[j].Status == EnumStatusIdentity.ToDrop)
                        {
                            TerminalState = new ErrorPage(TerminalState, EnumErrorTypeTerminalPage.InternAlreadySelected);
                            return true;
                        }

                        TerminalState = new ConfirmCancelPurchasePage(TerminalState, 1, j);
                        return true;
                    }
                }
            }

            TerminalState = new ConfirmCancelPurchasePage(TerminalState, 1);
            return true;
        }
    }
}
