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
            string firstWord = words[0];
            if (string.IsNullOrWhiteSpace(firstWord))
            {
                // get back to info page
                terminalParser.TerminalState = new InfoPage(this);
                return true;
            }

            // firstWord Buy
            if (terminalParser.IsMatchWord(firstWord, TerminalConst.STRING_BUY_COMMAND))
            {
                return terminalParser.BuyCommandSetNextPage(words);
            }

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
            sb.Append($"{"Name",-30} {"Hp",-3} {"Status",-6}");
            sb.AppendLine();
            sb.Append($"---------------------------------------------------");
            foreach (InternIdentity identity in IdentityManager.Instance.InternIdentities)
            {
                if (identity == null)
                {
                    continue;
                }

                string status = string.Empty;
                switch (identity.Status)
                {
                    case EnumStatusIdentity.Available:
                        break;
                    case EnumStatusIdentity.ToDrop:
                        status = "to drop";
                        break;
                    case EnumStatusIdentity.Spawned:
                        status = "on moon";
                        break;
                }

                if (!identity.Alive)
                {
                    status = "dead";
                }

                sb.AppendLine();
                sb.Append($"{identity.Name,-30} {identity.Hp,-3} {status}");
            }
            terminalNode.displayText = string.Format(TerminalConst.TEXT_STATUS, sb.ToString());

            return terminalNode;
        }
    }
}
