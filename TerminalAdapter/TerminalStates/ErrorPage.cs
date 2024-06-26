﻿using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.TerminalAdapter.TerminalStates
{
    internal class ErrorPage : TerminalState
    {
        private static readonly EnumTerminalStates STATE = EnumTerminalStates.Error;
        public override EnumTerminalStates GetTerminalState() { return STATE; }

        private readonly EnumErrorTypeTerminalPage enumErrorType;

        public ErrorPage(TerminalState newState, EnumErrorTypeTerminalPage enumErrorType) : base(newState)
        {
            this.enumErrorType = enumErrorType;
        }

        public override bool ParseCommandValid(string[] words)
        {
            // get back to info page
            terminalParser.TerminalState = new InfoPage(this);
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

            switch (enumErrorType)
            {
                case EnumErrorTypeTerminalPage.CannotPurchase:
                    terminalNode.displayText = Const.TEXT_ERROR_NOT_ENOUGH_CREDITS;
                    break;
                case EnumErrorTypeTerminalPage.ShipLeavingMoon:
                    terminalNode.displayText = Const.TEXT_ERROR_SHIP_LEAVING;
                    break;
                default:
                    terminalNode.displayText = Const.TEXT_ERROR_DEFAULT;
                    break;
            }

            return terminalNode;
        }
    }
}
