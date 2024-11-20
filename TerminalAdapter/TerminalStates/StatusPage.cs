using LethalInternship.AI;
using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.Managers;
using System.Text;
using UnityEngine;

namespace LethalInternship.TerminalAdapter.TerminalStates
{
    internal class StatusPage : TerminalState
    {
        /// <summary>
        /// <inheritdoc cref="TerminalState(TerminalState)"/>
        /// </summary>
        public StatusPage(TerminalState newState) : base(newState)
        {
            CurrentState = EnumTerminalStates.Status;
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

            StringBuilder sb = new StringBuilder();
            foreach (InternIdentity identity in IdentityManager.Instance.InternIdentities)
            {
                if (identity == null)
                {
                    continue;
                }

                sb.AppendLine();
                sb.Append($"{identity.Name}      Hp {identity.Hp}     {(identity.Alive ? "" : "dead")}");
            }

            terminalNode.displayText = string.Format(TerminalConst.TEXT_STATUS, sb.ToString());

            return terminalNode;
        }
    }
}
