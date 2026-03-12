using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;

namespace LethalInternship.Core.UI.CommandsControllers.DualSwitch
{
    public class ButtonDualSwitchParentController : MonoBehaviour
    {
        public System.Action<(EnumInputAction, EnumClickSide)> OnDualSwitchSelected = null!;

        public ButtonDualSwitchChildController left = null!;
        public ButtonDualSwitchChildController right = null!;

        void Awake()
        {
            left.OnSideSelected += HandleSide;
            right.OnSideSelected += HandleSide;
        }

        void HandleSide(EnumClickSide side)
        {
            switch (side)
            {
                case EnumClickSide.Left:
                    Debug.Log("Click LEFT");
                    break;
                case EnumClickSide.Right:
                    Debug.Log("Click RIGHT");
                    break;
            }
        }
    }
}
