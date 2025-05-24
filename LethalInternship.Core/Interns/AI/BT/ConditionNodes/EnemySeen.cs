using LethalInternship.SharedAbstractions.Constants;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class EnemySeen : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (ai.CurrentEnemy != null)
            {
                return true;
            }

            if (ai.NpcController.IsControllerInCruiser)
            {
                return false;
            }

            // Check for enemies
            EnemyAI? enemyAI = ai.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (enemyAI == null)
            {
                return false;
            }

            ai.CurrentEnemy = enemyAI;
            return true;
        }
    }
}
