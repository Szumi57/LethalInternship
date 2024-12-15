using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    internal class SpawningAnimationState : AIState
    {
        private EnumSpawnAnimation enumSpawnAnimation;

        /// <summary>
        /// <inheritdoc cref="AIState(InternAI)"/>
        /// </summary>
        public SpawningAnimationState(InternAI ai, EnumSpawnAnimation enumSpawnAnimation) : base(ai)
        {
            this.enumSpawnAnimation = enumSpawnAnimation;
            CurrentState = EnumAIStates.SpawningAnimation;
        }

        public override void DoAI()
        {
            // Start coroutine for spawning animation
            //StartspawnAnimationCoroutine();

            ai.StopMoving();
        }

        public override void TryPlayCurrentStateVoiceAudio()
        {
            ai.TryPlayVoiceAudioCutAndRepeatTalk(EnumVoicesState.Hit, shouldSyncAudio: false);
        }

        //private void StartspawnAnimationCoroutine()
        //{
        //    if (this.spawnAnimationCoroutine == null)
        //    {
        //        this.spawnAnimationCoroutine = ai.BeginInternSpawnAnimation(this.enumSpawnAnimation);
        //    }
        //}

        //private void StopspawnAnimationCoroutine()
        //{
        //    if (this.spawnAnimationCoroutine != null)
        //    {
        //        ai.StopCoroutine(this.spawnAnimationCoroutine);
        //    }
        //}
    }
}
