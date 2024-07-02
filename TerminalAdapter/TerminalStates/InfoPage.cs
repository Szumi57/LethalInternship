using BepInEx;
using LethalInternship.Enums;
using LethalInternship.Managers;
using UnityEngine;

namespace LethalInternship.TerminalAdapter.TerminalStates
{
    internal class InfoPage : TerminalState
    {
        private static readonly EnumTerminalStates STATE = EnumTerminalStates.Info;
        public override EnumTerminalStates GetTerminalState() { return STATE; }

        public InfoPage(TerminalState newState) : base(newState) { }

        public override bool ParseCommandValid(string[] words)
        {
            string firstWord = words[0];
            if (string.IsNullOrWhiteSpace(firstWord))
            {
                return false;
            }

            if (Const.STRING_INTERNSHIP_PROGRAM_COMMAND.Contains(firstWord)
               || Const.STRING_BACK_COMMAND.Contains(firstWord))
            {
                // stay on info page
                return true;
            }

            // firstWord Buy
            if (!Const.STRING_BUY_COMMAND.Contains(firstWord))
            {
                return false;
            }

            // Can buy ?
            if (TerminalManager.Instance.GetTerminal().groupCredits < Const.PRICE_INTERN)
            {
                terminalParser.TerminalState = new ErrorPage(this, EnumErrorTypeTerminalPage.CannotPurchase);
                return true;
            }
            else if (StartOfRound.Instance.shipIsLeaving)
            {
                terminalParser.TerminalState = new ErrorPage(this, EnumErrorTypeTerminalPage.ShipLeavingMoon);
                return true;
            }

            string secondWord = string.Empty;
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

            string textInfoPage;
            if (instanceSOR.inShipPhase 
                || instanceSOR.shipIsLeaving
                || instanceSOR.currentLevel.levelID == Const.COMPANY_BUILDING_MOON_ID)
            {
                // in space or on company building moon
                textInfoPage = string.Format(Const.TEXT_INFO_PAGE_IN_SPACE, instanceIM.NbInternsPurchasable, instanceIM.NbInternsToDropShip);
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
                textInfoPage = string.Format(Const.TEXT_INFO_PAGE_ON_MOON, instanceIM.NbInternsPurchasable, textNbInternsToDropShip, nbInternsOnThisMoon);
            }

            terminalNode.displayText = textInfoPage;
            return terminalNode;
        }
    }
}
