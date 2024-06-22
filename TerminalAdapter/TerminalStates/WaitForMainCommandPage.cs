using LethalInternship.Enums;

namespace LethalInternship.TerminalAdapter.TerminalStates
{
    internal class WaitForMainCommandPage : TerminalState
    {
        private static readonly EnumTerminalStates STATE = EnumTerminalStates.WaitForMainCommand;
        public override EnumTerminalStates GetTerminalState() { return STATE; }

        public WaitForMainCommandPage(TerminalParser terminalParser) : base(terminalParser) { }

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

        public override TerminalNode? DisplayNode()
        {
            return null;
        }
    }
}
