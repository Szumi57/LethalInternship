using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using LethalInternship.SharedAbstractions.Inputs;
using UnityEngine.InputSystem;

namespace LethalInternship.Inputs
{
    public class LethalInternshipInputs : LcInputActions, ILethalInternshipInputs
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [InputAction(KeyboardControl.E, Name = "Manage intern", GamepadPath = "<Gamepad>/dpad/up")]
        public InputAction ManageIntern { get; set; }


        [InputAction(KeyboardControl.G, Name = "Give/take item", GamepadControl = GamepadControl.ButtonEast)]
        public InputAction GiveItemToIntern { get; set; }


        [InputAction(KeyboardControl.Q, Name = "Grab intern", GamepadPath = "<Gamepad>/dpad/down")]
        public InputAction GrabIntern { get; set; }

        [InputAction(KeyboardControl.R, Name = "Release grabbed interns", GamepadControl = GamepadControl.LeftShoulder)]
        public InputAction ReleaseInterns { get; set; }


        [InputAction(KeyboardControl.X, Name = "Command for this intern", GamepadPath = "<Gamepad>/dpad/left")]
        public InputAction OpenCommandsOneIntern { get; set; }

        [InputAction(KeyboardControl.Z, Name = "Commands for all interns", GamepadPath = "<Gamepad>/dpad/right")]
        public InputAction OpenAllCommandsIntern { get; set; }


        [InputAction(KeyboardControl.C, Name = "Make intern look at position", GamepadPath = "<Gamepad>/dpad/up")]
        public InputAction MakeInternLookAtPosition { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
