using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.CoroutineControllers
{
    public class SearchCoroutineController
    {
        private InternAI ai;

        public bool ShouldStopCoroutine;
        public AISearchRoutine AISearchRoutine;

        public SearchCoroutineController(InternAI ai)
        {
            this.ai = ai;
            ShouldStopCoroutine = true;
            AISearchRoutine = null!;
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
                && AISearchRoutine != null)
            {
                ai.StopSearch(AISearchRoutine);
            }
        }

        public void StartSearch(Vector3 startOfSearch)
        {
            if (AISearchRoutine == null
                || !AISearchRoutine.inProgress)
            {
                // Start the coroutine from base game to search for players
                ai.StartSearch(startOfSearch, AISearchRoutine);
            }
        }

        public void StopSearch()
        {
            if (AISearchRoutine != null
                && AISearchRoutine.inProgress)
            {
                PluginLoggerHook.LogDebug?.Invoke("SearchCoroutineController stops coroutine");
                ai.StopSearch(AISearchRoutine, true);
            }
        }
    }
}
