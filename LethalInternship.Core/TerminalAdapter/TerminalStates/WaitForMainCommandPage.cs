using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.Core.TerminalAdapter.TerminalStates
{
    /// <summary>
    /// Page/State default, waiting for the right command to display <c>InfoPage</c>
    /// </summary>
    public class WaitForMainCommandPage : TerminalState
    {
        /// <summary>
        /// <inheritdoc cref="TerminalState(TerminalParser)"/>
        /// </summary>
        public WaitForMainCommandPage(TerminalParser terminalParser) : base(terminalParser) 
        { 
            CurrentState = EnumTerminalStates.WaitForMainCommand;
        }

        /// <summary>
        /// <inheritdoc cref="TerminalState(TerminalState)"/>
        /// </summary>
        public WaitForMainCommandPage(TerminalState newState) : base(newState)
        {
            CurrentState = EnumTerminalStates.WaitForMainCommand;
        }

        /// <summary>
        /// <inheritdoc cref="TerminalState.ParseCommandValid"/><br/>
        /// </summary>
        public override bool ParseCommandValid(string[] words)
        {
            string firstWord = words[0];
            if (string.IsNullOrWhiteSpace(firstWord)
                || !terminalParser.IsMatchWord(firstWord, TerminalManager.Instance.CommandIntershipProgram))
            {
                return false;
            }

            terminalParser.TerminalState = new InfoPage(this);
            return true;
        }

        /// <summary>
        /// <inheritdoc cref="TerminalState.DisplayNode"/><br/>
        /// - for <c>WaitForMainCommandPage</c>, no display, let the normal original game pages to be displayed
        /// </summary>
        public override TerminalNode? DisplayNode()
        {
            return null;
        }
    }
}
