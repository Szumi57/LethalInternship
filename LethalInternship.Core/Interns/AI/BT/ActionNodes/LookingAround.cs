using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class LookingAround : IBTAction
    {
        private float lookingAroundTimer;

        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            context.LookingAroundCoroutineController.StartCoroutine(LookingAroundEnumerator(ai));
            context.LookingAroundCoroutineController.KeepAlive();

            ai.StopMoving();

            return BehaviourTreeStatus.Success;
        }

        /// <summary>
        /// Coroutine for making intern turn his body to look around him
        /// </summary>
        /// <returns></returns>
        private IEnumerator LookingAroundEnumerator(InternAI ai)
        {
            lookingAroundTimer = 0f;
            yield return null;

            while (lookingAroundTimer < Const.TIMER_LOOKING_AROUND)
            {
                float freezeTimeRandom = Random.Range(Const.MIN_TIME_FREEZE_LOOKING_AROUND, Const.MAX_TIME_FREEZE_LOOKING_AROUND);
                float angleRandom = Random.Range(-180, 180);
                ai.NpcController.SetTurnBodyTowardsDirection(Quaternion.Euler(0, angleRandom, 0) * ai.NpcController.Npc.thisController.transform.forward);

                yield return new WaitForSeconds(freezeTimeRandom);
                lookingAroundTimer += freezeTimeRandom;
                PluginLoggerHook.LogDebug?.Invoke($"{ai.NpcController.Npc.playerUsername} Looking around to find player {lookingAroundTimer}/{Const.TIMER_LOOKING_AROUND}");
            }

            ai.TargetLastKnownPosition = null;
            ai.targetPlayer = null;
            yield break;
        }
    }
}
