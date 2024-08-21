using LethalInternship.Patches.TerminalPatches;
using LethalInternship.TerminalAdapter.TerminalStates;
using System;

namespace LethalInternship.TerminalAdapter
{
    /// <summary>
    /// Class used for holding the current state of the intern shop terminal pages<br/>
    /// and calling state method for parsing command and displaying current page.
    /// </summary>
    internal class TerminalParser
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

            command = TerminalPatch.RemovePunctuation_ReversePatch(terminal, command);
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
    }
}
