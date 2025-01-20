using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;

namespace LethalInternship.Inputs
{
    internal class LethalInternshipInputs : LcInputActions
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [InputAction(KeyboardControl.E, Name = "Supervise intern / Command", GamepadPath = "<Gamepad>/dpad/up")]
        public InputAction SuperviseCommandIntern { get; set; }

        [InputAction(KeyboardControl.G, Name = "Give/take item", GamepadControl = GamepadControl.ButtonEast)]
        public InputAction GiveTakeItem { get; set; }

        [InputAction(KeyboardControl.Q, Name = "Grab intern", GamepadPath = "<Gamepad>/dpad/down")]
        public InputAction GrabIntern { get; set; }

        [InputAction(KeyboardControl.R, Name = "Release grabbed interns", GamepadControl = GamepadControl.LeftShoulder)]
        public InputAction ReleaseInterns { get; set; }

        [InputAction(KeyboardControl.X, Name = "Change suit of intern", GamepadPath = "<Gamepad>/dpad/right")]
        public InputAction ChangeSuitIntern { get; set; }

        [InputAction(KeyboardControl.C, Name = "Make intern look at position", GamepadPath = "<Gamepad>/dpad/up")]
        public InputAction MakeInternLookAtPosition { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
