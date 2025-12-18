using LethalInternship.Core.BehaviorTree;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class EquipUnequipWeapon : IBTAction
    {
        private readonly bool equip;

        public EquipUnequipWeapon(bool equip)
        {
            this.equip = equip;
        }

        public BehaviourTreeStatus Action(BTContext context)
        {
            if (equip)
            {
                context.InternAI.EquipWeaponAsPrimary();
            }
            else
            {
                context.InternAI.UnequipWeaponAsPrimary();
            }

            return BehaviourTreeStatus.Success;
        }
    }
}
