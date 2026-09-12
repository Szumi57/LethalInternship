using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Abilities
{
    public class ChangeSuitAbility : Ability
    {
        public override bool RequiresTargeting => false;

        public enum SuitSelectionMode
        {
            Previous,
            Next,
            Random,
            Same,
            Selected
        }
        private SuitSelectionMode selectionMode;
        private int suitID = 0;

        public ChangeSuitAbility(SuitSelectionMode mode, IEnumerable<IInternIdentity> identities) : base(identities)
        {
            selectionMode = mode;
        }

        public ChangeSuitAbility(SuitSelectionMode mode, int suitID, IEnumerable<IInternIdentity> identities) : base(identities)
        {
            selectionMode = mode;
            this.suitID = suitID;
        }

        public override void Activate()
        {
            switch (selectionMode)
            {
                case SuitSelectionMode.Previous:
                    InternManager.Instance.ExecuteOrder(new ChangePreviousSuitOrder(IdentitiesToOrder));
                    break;
                case SuitSelectionMode.Next:
                    InternManager.Instance.ExecuteOrder(new ChangeNextSuitOrder(IdentitiesToOrder));
                    break;
                case SuitSelectionMode.Random:
                    InternManager.Instance.ExecuteOrder(new ChangeRandomSuitOrder(IdentitiesToOrder));
                    break;
                case SuitSelectionMode.Same:
                case SuitSelectionMode.Selected:
                    InternManager.Instance.ExecuteOrder(new ChangeSuitOrder(this.suitID, IdentitiesToOrder));
                    break;
                default:
                    break;
            }
        }
    }
}
