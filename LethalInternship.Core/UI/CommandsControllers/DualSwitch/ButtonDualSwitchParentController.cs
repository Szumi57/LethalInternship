using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.DualSwitch
{
    public class ButtonDualSwitchParentController : MonoBehaviour
    {
        private enum StateAutoDefenseUI
        {
            AutoDefense,
            Flee
        }

        public static System.Action<EnumInputAction> OnDualSwitchSelected = null!;

        public ButtonDualSwitchChildController left = null!;
        public ButtonDualSwitchChildController right = null!;

        public bool IsStatusImageEnabled = false;
        public Image StatusImage = null!;
        public Sprite[] StatusSprites = null!;

        void Awake()
        {
            left.OnSideSelected += HandleSide;
            right.OnSideSelected += HandleSide;

            StatusImage.enabled = IsStatusImageEnabled;
        }

        void HandleSide(EnumClickSide side)
        {
            switch (side)
            {
                case EnumClickSide.Left:
                    OnDualSwitchSelected?.Invoke(EnumInputAction.SetToAutoFlee);
                    if (IsStatusImageEnabled)
                        StatusImage.sprite = StatusSprites[(int)StateAutoDefenseUI.Flee];
                    break;
                case EnumClickSide.Right:
                    OnDualSwitchSelected?.Invoke(EnumInputAction.SetToAutoDefense);
                    if (IsStatusImageEnabled)
                        StatusImage.sprite = StatusSprites[(int)StateAutoDefenseUI.AutoDefense];
                    break;
            }
        }
    }
}
