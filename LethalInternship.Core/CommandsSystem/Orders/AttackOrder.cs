using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class AttackOrder : Order
    {
        private readonly EnemyAI enemyToAttack;

        public AttackOrder(EnemyAI enemyToAttack, IReadOnlyList<IInternIdentity> identities)
                : base(identities)
        {
            this.enemyToAttack = enemyToAttack;
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.SetCommandToAttackEnemy(enemyToAttack);
        }
    }
}
