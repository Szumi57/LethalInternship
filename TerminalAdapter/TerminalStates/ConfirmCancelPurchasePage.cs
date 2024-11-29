using LethalInternship.AI;
using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.Managers;
using System;
using UnityEngine;

namespace LethalInternship.TerminalAdapter.TerminalStates
{
    /// <summary>
    /// Page/State to confirm or cancel after the purchase of an intern
    /// </summary>
    internal class ConfirmCancelPurchasePage : TerminalState
    {
        private int nbOrdered;
        private int idIdentityChosen = -1;

        /// <summary>
        /// <inheritdoc cref="TerminalState(TerminalState)"/>
        /// </summary>
        public ConfirmCancelPurchasePage(TerminalState oldState, int nbOrdered) : base(oldState)
        {
            CurrentState = EnumTerminalStates.ConfirmCancelPurchase;

            int internPrice = Plugin.Config.InternPrice.Value;
            if (internPrice <= 0)
            {
                this.nbOrdered = nbOrdered;
            }
            else
            {
                int maxOrder = (int)Math.Floor((float)TerminalManager.Instance.GetTerminal().groupCredits / (float)internPrice);
                this.nbOrdered = nbOrdered < maxOrder ? nbOrdered : maxOrder;
            }
        }

        public ConfirmCancelPurchasePage(TerminalState oldState, int nbOrdered, int idIdentityChosen)
            : this(oldState, nbOrdered)
        {
            this.idIdentityChosen = idIdentityChosen;
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

            TerminalManager instanceTM = TerminalManager.Instance;

            if (terminalParser.IsMatchWord(firstWord, instanceTM.CommandIntershipProgram)
                || terminalParser.IsMatchWord(firstWord, TerminalConst.STRING_CANCEL_COMMAND)
                || terminalParser.IsMatchWord(firstWord, TerminalConst.STRING_BACK_COMMAND))
            {
                // get back to info page
                terminalParser.TerminalState = new InfoPage(this);
                return true;
            }

            if (terminalParser.IsMatchWord(firstWord, TerminalConst.STRING_CONFIRM_COMMAND))
            {
                InternManager instanceIM = InternManager.Instance;

                // Confirm
                int newCredits = instanceTM.GetTerminal().groupCredits - (Plugin.Config.InternPrice.Value * this.nbOrdered);
                instanceIM.AddNewCommandOfInterns(this.nbOrdered);
                instanceTM.GetTerminal().groupCredits = newCredits;

                instanceTM.UpdatePurchaseAndCreditsServerRpc(instanceIM.NbInternsOwned, instanceIM.NbInternsToDropShip, newCredits, idIdentityChosen);

                terminalParser.TerminalState = new InfoPage(this);
                return true;
            }

            return false;
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

            int internsAvailable = InternManager.Instance.NbInternsPurchasable;
            string textIfTooMuchOrdered = string.Empty;
            if (this.nbOrdered > internsAvailable)
            {
                textIfTooMuchOrdered = TerminalConst.TEXT_CONFIRM_CANCEL_PURCHASE_MAXIMUM;
                this.nbOrdered = internsAvailable;
            }

            if (idIdentityChosen < 0)
            {
                terminalNode.displayText = string.Format(TerminalConst.TEXT_CONFIRM_CANCEL_PURCHASE,
                                                         this.nbOrdered,
                                                         textIfTooMuchOrdered,
                                                         Plugin.Config.InternPrice.Value * this.nbOrdered);
            }
            else
            {
                terminalNode.displayText = string.Format(TerminalConst.TEXT_CONFIRM_CANCEL_SPECIFIC_PURCHASE,
                                                         IdentityManager.Instance.InternIdentities[idIdentityChosen].Name,
                                                         Plugin.Config.InternPrice.Value * this.nbOrdered);
            }

            return terminalNode;
        }
    }
}
