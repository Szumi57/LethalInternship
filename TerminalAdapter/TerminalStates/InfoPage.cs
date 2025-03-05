using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.TerminalAdapter.TerminalStates
{
    /// <summary>
    /// Page/state for displaying various infos about the interns owned and to be send by dropship on moon
    /// </summary>
    public class InfoPage : TerminalState
    {
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

            // firstWord status
            if (terminalParser.IsMatchWord(firstWord, TerminalConst.STRING_STATUS_COMMAND))
            {
                terminalParser.TerminalState = new StatusPage(this);
                return true;
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
            bool isCurrentMoonCompanyBuilding = instanceSOR.currentLevel.levelID == Const.COMPANY_BUILDING_MOON_ID;
            if (isCurrentMoonCompanyBuilding)
            {
                landingStatus += TerminalConst.STRING_LANDING_STATUS_ABORTED_COMPANY_MOON;
            }

            string textInfoPage;
            int nbInternsPurchasable = instanceIDM.GetNbIdentitiesAvailable() + diffNbInternAvailable;
            int nbInternsToDropShip = instanceIDM.GetNbIdentitiesToDrop() + diffNbInternToDrop;

            // Reset values for client number simulation
            this.diffNbInternAvailable = 0;
            this.diffNbInternToDrop = 0;

            if (instanceSOR.inShipPhase
                || instanceSOR.shipIsLeaving
                || isCurrentMoonCompanyBuilding)
            {
                // in space or on company building moon
                textInfoPage = string.Format(TerminalConst.TEXT_INFO_PAGE_IN_SPACE,
                                             nbInternsPurchasable,
                                             Plugin.Config.InternPrice.Value,
                                             nbInternsToDropShip,
                                             landingStatus);
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
                textInfoPage = string.Format(TerminalConst.TEXT_INFO_PAGE_ON_MOON,
                                             nbInternsPurchasable,
                                             Plugin.Config.InternPrice.Value,
                                             textNbInternsToDropShip,
                                             nbInternsOnThisMoon,
                                             landingStatus);
            }

            terminalNode.displayText = textInfoPage;
            return terminalNode;
        }
    }
}
