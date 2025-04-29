using LethalInternship.Constants;

namespace LethalInternship.Interns.AI.BT.ConditionNodes
{
    public class EnemySeen
    {
        public bool Condition(InternAI internAI)
        {
            if (internAI.CurrentEnemy != null)
            {
                return true;
            }

            if (internAI.NpcController.IsControllerInCruiser)
            {
                return false;
            }

            // Check for enemies
            EnemyAI? enemyAI = internAI.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (enemyAI == null)
            {
                return false;
            }

            internAI.CurrentEnemy = enemyAI;
            return true;
        }
    }
}
