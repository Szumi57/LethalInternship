using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.TerminalAdapter.TerminalStates
{
    /// <summary>
    /// Page/State to display error according to <see cref="EnumErrorTypeTerminalPage"><c>EnumErrorTypeTerminalPage</c></see>
    /// </summary>
    internal class ErrorPage : TerminalState
    {
        private readonly EnumErrorTypeTerminalPage enumErrorType;

        /// <summary>
        /// <inheritdoc cref="TerminalState(TerminalState)"/>
        /// </summary>
        public ErrorPage(TerminalState newState, EnumErrorTypeTerminalPage enumErrorType) : base(newState)
        {
            CurrentState = EnumTerminalStates.Error;
            this.enumErrorType = enumErrorType;
        }

        /// <summary>
        /// <inheritdoc cref="TerminalState.ParseCommandValid"/>
        /// </summary>
        public override bool ParseCommandValid(string[] words)
        {
            // get back to info page
            terminalParser.TerminalState = new InfoPage(this);
            return true;
        }

        /// <summary>
        /// <inheritdoc cref="TerminalState.DisplayNode"/>
        /// </summary>
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
                case EnumErrorTypeTerminalPage.NotEnoughCredits:
                    terminalNode.displayText = TerminalConst.TEXT_ERROR_NOT_ENOUGH_CREDITS;
                    break;
                case EnumErrorTypeTerminalPage.NoMoreInterns:
                    terminalNode.displayText = TerminalConst.TEXT_NO_MORE_INTERNS_PURCHASABLE;
                    break;
                case EnumErrorTypeTerminalPage.ShipLeavingMoon:
                    terminalNode.displayText = TerminalConst.TEXT_ERROR_SHIP_LEAVING;
                    break;
                default:
                    terminalNode.displayText = TerminalConst.TEXT_ERROR_DEFAULT;
                    break;
            }

            // Play sound
            TerminalManager.Instance.GetTerminal()
                .terminalAudio.PlayOneShot(TerminalManager.Instance.GetTerminal()
                                            .syncedAudios[TerminalConst.INDEX_AUDIO_ERROR]);

            return terminalNode;
        }
    }
}
