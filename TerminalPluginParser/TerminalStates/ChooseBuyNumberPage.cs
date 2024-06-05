using LethalInternship.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.TerminalPluginParser.TerminalStates
{
    internal class ChooseBuyNumberPage : TerminalState
    {
        private static readonly EnumTerminalStates STATE = EnumTerminalStates.ChooseBuyNumber;
        public override EnumTerminalStates GetTerminalState() { return STATE; }

        public ChooseBuyNumberPage(TerminalState newState) : base(newState) { }

        public override bool ParseCommandValid(string[] words)
        {
            string firstWord = words[0];
            if (string.IsNullOrWhiteSpace(firstWord))
            {
                return false;
            }

            if (Const.STRING_INTERNSHIP_PROGRAM_COMMAND.Contains(firstWord)
                || Const.STRING_CANCEL_COMMAND.Contains(firstWord)
                || Const.STRING_BACK_COMMAND.Contains(firstWord))
            {
                // get back to info page
                terminalParser.TerminalState = new InfoPage(this);
                return true;
            }

            if (int.TryParse(firstWord, out int nbOrdered) && nbOrdered > 0)
            {
                terminalParser.TerminalState = new ConfirmCancelPurchasePage(this, nbOrdered);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override TerminalNode? DisplayNode()
        {
            if (!dictTerminalNodeByState.TryGetValue(this.GetTerminalState(), out TerminalNode terminalNode))
            {
                terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
                dictTerminalNodeByState[this.GetTerminalState()] = terminalNode;
            }
            terminalNode.clearPreviousText = true;

            terminalNode.displayText = Const.TEXT_CHOOSE_BUY_NUMBER_PAGE;
            return terminalNode;
        }
    }
}
