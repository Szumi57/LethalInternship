using LethalInternship.Core.Interns.AI.CoroutineControllers;
using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.Core.Interns.AI.BT
{
    public class BTContext
    {
        public InternAI InternAI { get; set; }

        public CoroutineController PanikCoroutine { get; set; }
        public CoroutineController LookingAroundCoroutineController { get; set; }
        public CoroutineController searchingWanderCoroutineController { get; set; }
        public SearchCoroutineController searchForPlayers { get; set; }
    }
}
