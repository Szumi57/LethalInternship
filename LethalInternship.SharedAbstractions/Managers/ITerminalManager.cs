using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface ITerminalManager
    {
        GameObject ManagerGameObject { get; }

        Terminal GetTerminal();

        void AddTextToHelpTerminalNode(TerminalNodesList terminalNodesList);
        TerminalNode? ParseLethalInternshipCommands(string command, ref Terminal terminal);
        EnumTerminalStates GetTerminalPage();
        void ResetTerminalParser();
    }
}
