using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.CoroutineControllers
{
    public class CoroutineController
    {
        private InternAI ai;

        private Coroutine? coroutine;
        private bool shouldStopCoroutine;

        public CoroutineController(InternAI ai)
        {
            this.ai = ai;
            shouldStopCoroutine = true;
            coroutine = null;
        }

        public void KeepAlive()
        {
            shouldStopCoroutine = false;
        }

        public void Reset()
        {
            shouldStopCoroutine = true;
        }

        public void CheckCoroutine()
        {
            if (shouldStopCoroutine
                && coroutine != null)
            {
                PluginLoggerHook.LogDebug?.Invoke("CoroutineController stops coroutine");
                ai.StopCoroutine(coroutine);
                coroutine = null;
            }
        }

        public void StartCoroutine(IEnumerator coroutineMethod)
        {
            if (coroutine == null)
            {
                coroutine = ai.StartCoroutine(coroutineMethod);
            }
        }

        public void RestartCoroutine(IEnumerator coroutineMethod)
        {
            StopCoroutine();
            StartCoroutine(coroutineMethod);
        }

        public void StopCoroutine()
        {
            if (coroutine != null)
            {
                ai.StopCoroutine(coroutine);
                coroutine = null;
            }
        }
    }
}
