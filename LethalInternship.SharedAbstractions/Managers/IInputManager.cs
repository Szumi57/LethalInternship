using UnityEngine.InputSystem;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IInputManager
    {
        void RemoveEventHandlers();
        string GetKeyAction(InputAction inputAction);
    }
}
