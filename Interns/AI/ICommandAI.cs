using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.Interns.AI
{
    public interface ICommandAI
    {
        public EnumCommandEnd Execute();

        public void PlayerHeard(Vector3 noisePosition);

        public string GetBillboardStateIndicator();
    }
}
