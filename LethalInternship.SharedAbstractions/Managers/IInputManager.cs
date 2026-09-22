using LethalInternship.SharedAbstractions.CommandsSystem;
using UnityEngine.InputSystem;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IInputManager
    {
        TargetedAbility? CurrentTargetedAbility { get; }
        TargetedAbility? PreviousTargetedAbility { get; }

        bool IsUsingController { get; }

        bool IsManualEmoteToIgnore { get; }

        string GetKeyAction(InputAction inputAction);
    }
}
