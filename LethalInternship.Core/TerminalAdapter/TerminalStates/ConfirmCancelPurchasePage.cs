using LethalInternship.Core.Interns;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using UnityEngine;

namespace LethalInternship.Core.TerminalAdapter.TerminalStates
{
    /// <summary>
    /// Page/State to confirm or cancel after the purchase of an intern
    /// </summary>
    public class ConfirmCancelPurchasePage : TerminalState
    {
        private int nbOrdered;
        private int idIdentityChosen = -1;

        /// <summary>
        /// <inheritdoc cref="TerminalState(TerminalState)"/>
        /// </summary>
        public ConfirmCancelPurchasePage(TerminalState oldState, int nbOrdered) : base(oldState)
        {
            CurrentState = EnumTerminalStates.ConfirmCancelPurchase;

            int nbIdentitiesAvailable = IdentityManager.Instance.GetNbIdentitiesAvailable();
            if (nbOrdered > nbIdentitiesAvailable)
            {
                // nbIdentitiesAvailable == 0 alreay check before arriving here
                nbOrdered = nbIdentitiesAvailable;
            }

            int internPrice = PluginRuntimeProvider.Context.Config.InternPrice;
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
                // Confirm
                int newCredits = instanceTM.GetTerminal().groupCredits - (PluginRuntimeProvider.Context.Config.InternPrice * this.nbOrdered);
                instanceTM.GetTerminal().groupCredits = newCredits;

                if (idIdentityChosen == -1)
                {
                    instanceTM.BuyRandomInternsServerRpc(newCredits, this.nbOrdered);
                }
                else
                {
                    instanceTM.BuySpecificInternServerRpc(newCredits, idIdentityChosen);
                }

                if (instanceTM.IsServer)
                {
                    terminalParser.TerminalState = new InfoPage(this);
                }
                else
                {
                    int diffNbInternAvailable = -this.nbOrdered;
                    int diffNbInternToDrop = this.nbOrdered;

                    terminalParser.TerminalState = new InfoPage(this, diffNbInternAvailable, diffNbInternToDrop);
                }
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

            int internsAvailable = IdentityManager.Instance.GetNbIdentitiesAvailable();
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
                                                         PluginRuntimeProvider.Context.Config.InternPrice * this.nbOrdered);
            }
            else
            {
                InternIdentity internIdentity = IdentityManager.Instance.InternIdentities[idIdentityChosen];
                string textRevivedIntern = internIdentity.Alive ? string.Empty : TerminalConst.TEXT_CONFIRM_CANCEL_REVIVE_INTERN;
                terminalNode.displayText = string.Format(TerminalConst.TEXT_CONFIRM_CANCEL_SPECIFIC_PURCHASE,
                                                         internIdentity.Name,
                                                         textRevivedIntern,
                                                         PluginRuntimeProvider.Context.Config.InternPrice * this.nbOrdered);
            }

            return terminalNode;
        }
    }
}
