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
        private static readonly EnumTerminalStates STATE = EnumTerminalStates.Error;
        /// <summary>
        /// <inheritdoc cref="TerminalState.GetTerminalState"/>
        /// </summary>
        public override EnumTerminalStates GetTerminalState() { return STATE; }

        private readonly EnumErrorTypeTerminalPage enumErrorType;

        /// <summary>
        /// <inheritdoc cref="TerminalState(TerminalState)"/>
        /// </summary>
        public ErrorPage(TerminalState newState, EnumErrorTypeTerminalPage enumErrorType) : base(newState)
        {
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
                    terminalNode.displayText = Const.TEXT_ERROR_NOT_ENOUGH_CREDITS;
                    break;
                case EnumErrorTypeTerminalPage.NoMoreInterns:
                    terminalNode.displayText = Const.TEXT_NO_MORE_INTERNS_PURCHASABLE;
                    break;
                case EnumErrorTypeTerminalPage.ShipLeavingMoon:
                    terminalNode.displayText = Const.TEXT_ERROR_SHIP_LEAVING;
                    break;
                default:
                    terminalNode.displayText = Const.TEXT_ERROR_DEFAULT;
                    break;
            }

            // Play sound
            TerminalManager.Instance.GetTerminal()
                .terminalAudio.PlayOneShot(TerminalManager.Instance.GetTerminal()
                                            .syncedAudios[Const.INDEX_AUDIO_ERROR]);

            return terminalNode;
        }
    }
}
