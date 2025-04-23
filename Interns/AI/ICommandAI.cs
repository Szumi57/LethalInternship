using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.Interns.AI
{
    public interface ICommandAI
    {
        public EnumCommandTypes GetCommandType();

        public void Execute();

        public void PlayerHeard(Vector3 noisePosition);

        public string GetBillboardStateIndicator();
    }
}
