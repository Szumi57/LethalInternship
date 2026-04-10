using LethalInternship.Core.CommandsSystem.Orders;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;

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

        public ChangeSuitAbility(SuitSelectionMode mode)
        {
            selectionMode = mode;
        }

        public ChangeSuitAbility(SuitSelectionMode mode, int suitID)
        {
            selectionMode = mode;
            this.suitID = suitID;
        }

        public override void Activate()
        {
            switch (selectionMode)
            {
                case SuitSelectionMode.Previous:
                    InternManager.Instance.ExecuteOrder(new ChangePreviousSuitOrder());
                    break;
                case SuitSelectionMode.Next:
                    InternManager.Instance.ExecuteOrder(new ChangeNextSuitOrder());
                    break;
                case SuitSelectionMode.Random:
                    InternManager.Instance.ExecuteOrder(new ChangeRandomSuitOrder());
                    break;
                case SuitSelectionMode.Same:
                case SuitSelectionMode.Selected:
                    InternManager.Instance.ExecuteOrder(new ChangeSuitOrder(this.suitID));
                    break;
                default:
                    break;
            }
        }
    }
}
