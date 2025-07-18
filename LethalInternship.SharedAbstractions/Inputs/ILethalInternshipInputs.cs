using UnityEngine.InputSystem;

namespace LethalInternship.SharedAbstractions.Inputs
{
    public interface ILethalInternshipInputs
    {
        public InputAction ManageIntern { get; set; }

        public InputAction GiveTakeItem { get; set; }

        public InputAction GrabIntern { get; set; }

        public InputAction ReleaseInterns { get; set; }

        public InputAction ChangeSuitIntern { get; set; }

        public InputAction MakeInternLookAtPosition { get; set; }

        public InputAction OpenCommandsIntern { get; set; }
    }
}
