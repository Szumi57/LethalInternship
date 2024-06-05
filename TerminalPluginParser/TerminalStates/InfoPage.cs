using BepInEx;
using LethalInternship.Enums;
using LethalInternship.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.TerminalPluginParser.TerminalStates
{
    internal class InfoPage : TerminalState
    {
        private static readonly EnumTerminalStates STATE = EnumTerminalStates.Info;
        public override EnumTerminalStates GetTerminalState() { return STATE; }

        public InfoPage(TerminalState newState) : base(newState) { }

        public override bool ParseCommandValid(string[] words)
        {
            string firstWord = words[0];
            if (string.IsNullOrWhiteSpace(firstWord))
            {
                return false;
            }

            if (Const.STRING_INTERNSHIP_PROGRAM_COMMAND.Contains(firstWord)
               || Const.STRING_BACK_COMMAND.Contains(firstWord))
            {
                // stay on info page
                return true;
            }

            // firstWord Buy
            if (!Const.STRING_BUY_COMMAND.Contains(firstWord))
            {
                return false;
            }

            // Can buy ?
            if (TerminalManager.Instance.GetTerminal().groupCredits < Const.PRICE_INTERN)
            {
                terminalParser.TerminalState = new ErrorPage(this, EnumErrorTypeTerminalPage.CannotPurchase);
                return true;
            }

            string secondWord = string.Empty;
            if (words.Length > 1)
            {
                secondWord = words[1];
            }

            // secondWord number
            if (!secondWord.IsNullOrWhiteSpace()
                && int.TryParse(secondWord, out int nbOrdered)
                && nbOrdered > 0)
            {
                terminalParser.TerminalState = new ConfirmCancelPurchasePage(this, nbOrdered);
            }
            else
            {
                terminalParser.TerminalState = new ChooseBuyNumberPage(this);
            }

            return true;
        }

        public override TerminalNode? DisplayNode()
        {
            if (!dictTerminalNodeByState.TryGetValue(this.GetTerminalState(), out TerminalNode terminalNode))
            {
                terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
                dictTerminalNodeByState[this.GetTerminalState()] = terminalNode;
            }
            terminalNode.clearPreviousText = true;

            int internsAvailable = Const.INTERN_AVAILABLE - terminalParser.NbInternsAlreadyBought;

            terminalNode.displayText = string.Format(Const.TEXT_INFO_PAGE, terminalParser.NbInternsAlreadyBought, internsAvailable, terminalParser.NbInternsAlreadyBought);
            return terminalNode;
        }
    }
}
