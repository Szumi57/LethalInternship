using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class AttackOrder : Order
    {
        private readonly EnemyAI enemyToAttack;

        public AttackOrder(EnemyAI enemyToAttack)
        {
            this.enemyToAttack = enemyToAttack;
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.SetCommandToAttackEnemy(enemyToAttack);
        }
    }
}
