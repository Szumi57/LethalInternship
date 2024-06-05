using LethalInternship.Enums;
using LethalInternship.Patches.TerminalPatches;
using LethalInternship.TerminalPluginParser.TerminalStates;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalInternship.TerminalPluginParser
{
    internal class TerminalParser
    {
        public TerminalState TerminalState = null!;
        public int NbInternsAlreadyBought;

        public TerminalParser()
        {
            TerminalState = new WaitForMainCommandPage(this);
        }

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
    }
}
