using LethalInternship.Core.Interns;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System.Text;
using UnityEngine;

namespace LethalInternship.Core.TerminalAdapter.TerminalStates
{
    public class StatusPage : TerminalState
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
            sb.Append($"{"Name",-20} {"Hp",-3} {"Status",-7}  {"Suit", -4}");
            sb.AppendLine();
            sb.Append($"---------------------------------------------------"); // 51
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

                string? identityName = identity.Name.Truncate(19);
                string? suit = identity.Suit.Truncate(16);

                sb.AppendLine();
                sb.Append($"{identityName,-20} {identity.Hp,-3} {status, -7}  {suit}");
            }
            terminalNode.displayText = string.Format(TerminalConst.TEXT_STATUS, sb.ToString());

            return terminalNode;
        }
    }

    public static class StringExt
    {
        // https://stackoverflow.com/questions/2776673/how-do-i-truncate-a-net-string
        public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "…")
        {
            return value?.Length > maxLength
                ? value.Substring(0, maxLength) + truncationSuffix
                : value;
        }
    }
}
