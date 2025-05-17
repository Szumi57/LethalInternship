using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface ITerminalManager
    {
        Terminal GetTerminal();

        void AddTextToHelpTerminalNode(TerminalNodesList terminalNodesList);
        TerminalNode? ParseLethalInternshipCommands(string command, ref Terminal terminal);
        EnumTerminalStates GetTerminalPage();
        void ResetTerminalParser();
    }
}
