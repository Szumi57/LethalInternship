using UnityEngine;

namespace LethalInternship.Core.UI.Others
{
    public interface IVisibilityUI : IGroupUI
    {
        GameObject Go { get; }

        void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!);
    }
}
