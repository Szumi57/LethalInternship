using BepInEx;
using LethalInternship.Enums;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.TerminalAdapter.TerminalStates
{
    /// <summary>
    /// Page/state for displaying various infos about the interns owned and to be send by dropship on moon
    /// </summary>
    internal class InfoPage : TerminalState
    {
        private static readonly EnumTerminalStates STATE = EnumTerminalStates.Info;
        /// <summary>
        /// <inheritdoc cref="TerminalState.GetTerminalState"/>
        /// </summary>
        public override EnumTerminalStates GetTerminalState() { return STATE; }

        /// <summary>
        /// <inheritdoc cref="TerminalState(TerminalState)"/>
        /// </summary>
        public InfoPage(TerminalState newState) : base(newState) { }

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

            if (terminalParser.IsMatchWord(firstWord, Const.STRING_INTERNSHIP_PROGRAM_COMMAND)
               || terminalParser.IsMatchWord(firstWord, Const.STRING_BACK_COMMAND))
            {
                // stay on info page
                return true;
            }

            // firstWord Buy
            if (terminalParser.IsMatchWord(firstWord, Const.STRING_BUY_COMMAND))
            {
                return BuyCommandSetNextPage(words);
            }

            // firstWord landing status
            if (terminalParser.IsMatchWord(firstWord, Const.STRING_LAND_COMMAND)
                || terminalParser.IsMatchWord(firstWord, Const.STRING_ABORT_COMMAND))
            {
                return LandingStatusCommand(firstWord);
            }

            return false;
        }

        private bool BuyCommandSetNextPage(string[] words)
        {
            // Can buy ?
            if (TerminalManager.Instance.GetTerminal().groupCredits < Plugin.Config.InternPrice.Value)
            {
                terminalParser.TerminalState = new ErrorPage(this, EnumErrorTypeTerminalPage.NotEnoughCredits);
                return true;
            }
            else if (InternManager.Instance.NbInternsPurchasable <= 0)
            {
                terminalParser.TerminalState = new ErrorPage(this, EnumErrorTypeTerminalPage.NoMoreInterns);
                return true;
            }
            else if (StartOfRound.Instance.shipIsLeaving)
            {
                terminalParser.TerminalState = new ErrorPage(this, EnumErrorTypeTerminalPage.ShipLeavingMoon);
                return true;
            }

            string secondWord;
            if (words.Length > 1)
            {
                secondWord = words[1];
            }
            else
            {
                terminalParser.TerminalState = new ConfirmCancelPurchasePage(this, 1);
                return true;
            }

            // secondWord number
            if (!secondWord.IsNullOrWhiteSpace()
                && int.TryParse(secondWord, out int nbOrdered)
                && nbOrdered > 0)
            {
                terminalParser.TerminalState = new ConfirmCancelPurchasePage(this, nbOrdered);
            }
            else
            {
                terminalParser.TerminalState = new ConfirmCancelPurchasePage(this, 1);
            }

            return true;
        }

        private bool LandingStatusCommand(string command)
        {
            TerminalManager instanceTM = TerminalManager.Instance;
            InternManager instanceIM = InternManager.Instance;

            if (terminalParser.IsMatchWord(command, Const.STRING_LAND_COMMAND))
            {
                instanceIM.LandingStatusAllowed = true;
            }
            else if (terminalParser.IsMatchWord(command, Const.STRING_ABORT_COMMAND))
            {
                instanceIM.LandingStatusAllowed = false;
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

            if (!dictTerminalNodeByState.TryGetValue(this.GetTerminalState(), out TerminalNode terminalNode))
            {
                terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
                dictTerminalNodeByState[this.GetTerminalState()] = terminalNode;
            }
            terminalNode.clearPreviousText = true;

            // Landing status
            string landingStatus = instanceIM.LandingStatusAllowed ? Const.STRING_LANDING_STATUS_ALLOWED : Const.STRING_LANDING_STATUS_ABORTED;
            bool isCurrentMoonCompanyBuilding = instanceSOR.currentLevel.levelID == Const.COMPANY_BUILDING_MOON_ID;
            if (isCurrentMoonCompanyBuilding)
            {
                landingStatus += Const.STRING_LANDING_STATUS_ABORTED_COMPANY_MOON;
            }

            string textInfoPage;

            if (instanceSOR.inShipPhase
                || instanceSOR.shipIsLeaving
                || isCurrentMoonCompanyBuilding)
            {
                // in space or on company building moon
                textInfoPage = string.Format(Const.TEXT_INFO_PAGE_IN_SPACE, 
                                             instanceIM.NbInternsPurchasable, 
                                             Plugin.Config.InternPrice.Value, 
                                             instanceIM.NbInternsToDropShip,
                                             landingStatus);
            }
            else
            {
                // on moon
                string textNbInternsToDropShip = string.Empty;
                int nbInternsToDropShip = instanceIM.NbInternsToDropShip;
                int nbInternsOnThisMoon = instanceIM.NbInternsOwned - nbInternsToDropShip;
                if (nbInternsToDropShip > 0
                    && !instanceSOR.shipIsLeaving)
                {
                    textNbInternsToDropShip = string.Format(Const.TEXT_INFO_PAGE_INTERN_TO_DROPSHIP, nbInternsToDropShip);
                }
                textInfoPage = string.Format(Const.TEXT_INFO_PAGE_ON_MOON, 
                                             instanceIM.NbInternsPurchasable, 
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
