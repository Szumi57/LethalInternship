using LethalInternship.Core.Interns;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Text;
using UnityEngine;

namespace LethalInternship.Core.TerminalAdapter.TerminalStates
{
    /// <summary>
    /// Page/state for displaying various infos about the interns owned and to be send by dropship on moon
    /// </summary>
    public class InfoPage : TerminalState
    {
        private readonly StringBuilder _sb = new StringBuilder();

        private int diffNbInternAvailable;
        private int diffNbInternToDrop;

        /// <summary>
        /// <inheritdoc cref="TerminalState(TerminalState)"/>
        /// </summary>
        public InfoPage(TerminalState newState) : base(newState)
        {
            CurrentState = EnumTerminalStates.Info;
            this.diffNbInternAvailable = 0;
            this.diffNbInternToDrop = 0;
        }

        /// <summary>
        /// Constructor only for client after to simulate new values and print them while waiting for server rpc to update values
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="diffNbInternAvailable"></param>
        /// <param name="diffNbInternToDrop"></param>
        public InfoPage(TerminalState newState, int diffNbInternAvailable, int diffNbInternToDrop) : base(newState)
        {
            CurrentState = EnumTerminalStates.Info;
            this.diffNbInternAvailable = diffNbInternAvailable;
            this.diffNbInternToDrop = diffNbInternToDrop;
        }

        /// <summary>
        /// <inheritdoc cref="TerminalState.ParseCommandValid"/>
        /// </summary>
        public override bool ParseCommandValid(string[] words)
        {
            string firstWord = words[0];
            if (string.IsNullOrWhiteSpace(firstWord))
            {
                return false;
            }

            if (terminalParser.IsMatchWord(firstWord, TerminalManager.Instance.CommandIntershipProgram)
               || terminalParser.IsMatchWord(firstWord, TerminalConst.STRING_BACK_COMMAND))
            {
                // stay on info page
                return true;
            }

            // firstWord Buy
            if (terminalParser.IsMatchWord(firstWord, TerminalConst.STRING_BUY_COMMAND))
            {
                return terminalParser.BuyCommandSetNextPage(words);
            }

            // firstWord land
            if (terminalParser.IsMatchWord(firstWord, TerminalConst.STRING_LAND_COMMAND))
            {
                return LandingStatusCommand(firstWord);
            }

            // firstWord transmit
            if (terminalParser.IsMatchWord(firstWord, TerminalConst.STRING_EVACUATION_COMMAND))
            {
                return EvacuationCommand();
            }

            return false;
        }

        private bool LandingStatusCommand(string command)
        {
            TerminalManager instanceTM = TerminalManager.Instance;
            InternManager instanceIM = InternManager.Instance;

            if (terminalParser.IsMatchWord(command, TerminalConst.STRING_LAND_COMMAND))
            {
                instanceIM.LandingStatusAllowed = !instanceIM.LandingStatusAllowed;
            }

            instanceTM.SyncLandingStatusServerRpc(instanceIM.LandingStatusAllowed);

            // stay on info page
            return true;
        }

        private bool EvacuationCommand()
        {
            EnumErrorTypeTerminalPage errorPageMessage = TerminalManager.Instance.BroadcastRecallInterns();
            if (errorPageMessage != EnumErrorTypeTerminalPage.NoError)
            {
                terminalParser.TerminalState = new ErrorPage(terminalParser.TerminalState, errorPageMessage);
                return true;
            }
            // stay on info page
            return true;
        }

        /// <summary>
        /// <inheritdoc cref="TerminalState.DisplayNode"/>
        /// </summary>
        public override TerminalNode? DisplayNode()
        {
            StartOfRound instanceSOR = StartOfRound.Instance;
            InternManager instanceIM = InternManager.Instance;
            IdentityManager instanceIDM = IdentityManager.Instance;

            if (!dictTerminalNodeByState.TryGetValue(this.GetTerminalState(), out TerminalNode terminalNode))
            {
                terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
                dictTerminalNodeByState[this.GetTerminalState()] = terminalNode;
            }
            terminalNode.clearPreviousText = true;

            // Landing status
            string landingStatus = instanceIM.LandingStatusAllowed ? TerminalConst.STRING_LANDING_STATUS_ALLOWED : TerminalConst.STRING_LANDING_STATUS_ABORTED;
            bool isCurrentMoonCompanyBuilding = instanceIM.IsCurrentMoonCompanyMoon();
            if (isCurrentMoonCompanyBuilding)
            {
                landingStatus += TerminalConst.STRING_LANDING_STATUS_ABORTED_COMPANY_MOON;
            }

            int nbInternsPurchasable = instanceIDM.GetNbIdentitiesAvailable() + diffNbInternAvailable;
            int nbInternsToDropShip = instanceIDM.GetNbIdentitiesToDrop() + diffNbInternToDrop;

            // Reset values for client number simulation
            this.diffNbInternAvailable = 0;
            this.diffNbInternToDrop = 0;
            _sb.Clear();

            if (instanceSOR.inShipPhase
                || instanceSOR.shipIsLeaving
                || isCurrentMoonCompanyBuilding)
            {
                // in space or on company building moon
                _sb.Append(string.Format(TerminalConst.TEXT_INFO_PAGE_IN_SPACE,
                                             nbInternsPurchasable,
                                             PluginRuntimeProvider.Context.Config.InternPrice,
                                             nbInternsToDropShip,
                                             landingStatus,

                                             TerminalConst.STRING_LAND_COMMAND,
                                             TerminalConst.STRING_EVACUATION_COMMAND,
                                             TerminalConst.STRING_BUY_COMMAND
                                             ));
            }
            else
            {
                // on moon
                string textNbInternsToDropShip = string.Empty;
                int nbInternsOnThisMoon = instanceIDM.GetNbIdentitiesSpawned();
                if (nbInternsToDropShip > 0
                    && !instanceSOR.shipIsLeaving)
                {
                    textNbInternsToDropShip = string.Format(TerminalConst.TEXT_INFO_PAGE_INTERN_TO_DROPSHIP, nbInternsToDropShip);
                }
                _sb.Append(string.Format(TerminalConst.TEXT_INFO_PAGE_ON_MOON,
                                             nbInternsPurchasable,
                                             PluginRuntimeProvider.Context.Config.InternPrice,
                                             textNbInternsToDropShip,
                                             nbInternsOnThisMoon,
                                             landingStatus,

                                             TerminalConst.STRING_LAND_COMMAND,
                                             TerminalConst.STRING_EVACUATION_COMMAND,
                                             TerminalConst.STRING_BUY_COMMAND
                                             ));
            }

            // Interns status
            _sb.AppendLine();
            _sb.AppendLine();
            _sb.AppendLine();
            _sb.Append(TerminalConst.TEXT_STATUS);
            _sb.Append($"{"Name",-20} {"Hp",-3} {"Status",-7}  {"Suit",-4}");
            _sb.AppendLine();
            _sb.Append($"---------------------------------------------------"); // 51
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

                _sb.AppendLine();
                _sb.Append($"{identityName,-20} {identity.Hp,-3} {status,-7}  {suit}");
            }

            terminalNode.displayText = _sb.ToString();
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
