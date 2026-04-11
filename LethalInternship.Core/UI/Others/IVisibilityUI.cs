using UnityEngine;

namespace LethalInternship.Core.UI.Others
{
    public interface IVisibilityUI
    {
        GameObject Go { get; }
        EnumUIGroups GroupUI { get; }

        void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!);
    }
}
