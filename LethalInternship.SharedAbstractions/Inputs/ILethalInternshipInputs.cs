using UnityEngine.InputSystem;

namespace LethalInternship.SharedAbstractions.Inputs
{
    public interface ILethalInternshipInputs
    {
        public InputAction ManageIntern { get; set; }


        public InputAction GiveItemToIntern { get; set; }


        public InputAction GrabIntern { get; set; }

        public InputAction ReleaseInterns { get; set; }


        public InputAction OpenCommandsOneIntern { get; set; }

        public InputAction OpenAllCommandsIntern { get; set; }


        public InputAction MakeInternLookAtPosition { get; set; }
    }
}
