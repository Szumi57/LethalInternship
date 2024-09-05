﻿using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;

namespace LethalInternship.Inputs
{
    internal class LethalInternshipInputs : LcInputActions
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [InputAction(KeyboardControl.E, Name = "Lead Intern", GamepadPath = "<Gamepad>/dpad/up")]
        public InputAction LeadIntern { get; set; }

        [InputAction(KeyboardControl.G, Name = "Give/take item", GamepadControl = GamepadControl.ButtonEast)]
        public InputAction GiveTakeItem { get; set; }

        [InputAction(KeyboardControl.Q, Name = "Grab intern", GamepadPath = "<Gamepad>/dpad/down")]
        public InputAction GrabIntern { get; set; }

        [InputAction(KeyboardControl.R, Name = "Release grabbed interns", GamepadControl = GamepadControl.LeftShoulder)]
        public InputAction ReleaseInterns { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}