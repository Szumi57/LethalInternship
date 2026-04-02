using UnityEngine.InputSystem;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IInputManager
    {
        string GetKeyAction(InputAction inputAction);
    }
}
