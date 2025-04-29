using System.Collections;
using UnityEngine;

namespace LethalInternship.Interns.AI.CoroutineControllers
{
    public class CoroutineController
    {
        private InternAI ai;

        public bool ShouldStopCoroutine;
        public Coroutine? Coroutine;

        public CoroutineController(InternAI ai)
        {
            this.ai = ai;
            ShouldStopCoroutine = true;
            Coroutine = null;
        }

        public void KeepAlive()
        {
            ShouldStopCoroutine = false;
        }

        public void Reset()
        {
            ShouldStopCoroutine = true;
        }

        public void CheckCoroutine()
        {
            if (ShouldStopCoroutine
                && Coroutine != null)
            {
                Plugin.LogDebug("CoroutineController stops coroutine");
                ai.StopCoroutine(Coroutine);
                Coroutine = null;
            }
        }

        public void StartCoroutine(IEnumerator coroutineMethod)
        {
            if (Coroutine == null)
            {
                Coroutine = ai.StartCoroutine(coroutineMethod);
            }
        }

        public void RestartCoroutine(IEnumerator coroutineMethod)
        {
            StopCoroutine();
            StartCoroutine(coroutineMethod);
        }

        public void StopCoroutine()
        {
            if (Coroutine != null)
            {
                ai.StopCoroutine(Coroutine);
                Coroutine = null;
            }
        }
    }
}
