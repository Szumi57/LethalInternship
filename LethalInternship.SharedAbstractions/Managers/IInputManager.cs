using LethalInternship.SharedAbstractions.CommandsSystem;
using UnityEngine.InputSystem;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IInputManager
    {
        TargetedAbility? CurrentTargetedAbility { get; }
        TargetedAbility? PreviousTargetedAbility { get; }

        string GetKeyAction(InputAction inputAction);
    }
}
