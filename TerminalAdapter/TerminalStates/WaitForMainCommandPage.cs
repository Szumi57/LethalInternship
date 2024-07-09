using LethalInternship.Enums;

namespace LethalInternship.TerminalAdapter.TerminalStates
{
    /// <summary>
    /// Page/State default, waiting for the right command to display <c>InfoPage</c>
    /// </summary>
    internal class WaitForMainCommandPage : TerminalState
    {
        private static readonly EnumTerminalStates STATE = EnumTerminalStates.WaitForMainCommand;
        /// <summary>
        /// <inheritdoc cref="TerminalState.GetTerminalState"/><br/>
        /// </summary>
        public override EnumTerminalStates GetTerminalState() { return STATE; }

        public WaitForMainCommandPage(TerminalParser terminalParser) : base(terminalParser) { }

        /// <summary>
        /// <inheritdoc cref="TerminalState.ParseCommandValid"/><br/>
        /// </summary>
        public override bool ParseCommandValid(string[] words)
        {
            string firstWord = words[0];
            if (string.IsNullOrWhiteSpace(firstWord)
                || !Const.STRING_INTERNSHIP_PROGRAM_COMMAND.Contains(firstWord))
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
