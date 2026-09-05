using LethalInternship.Core.UI.Others;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class SuitListButton : MonoBehaviour,
        IVisibilityUI,
        ISelectHandler,
        IDeselectHandler
    {
        public Action OnHover = null!;
        public Action OnUnhover = null!;
        public bool IsHovered;

        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI => EnumUIGroups.SuitMenu;

        void Awake()
        {
            Go = this.gameObject;
        }

        public void OnSelect(BaseEventData eventData)
        {
            IsHovered = true;
            OnHover?.Invoke();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            IsHovered = false;
            OnUnhover?.Invoke();
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            //
        }
    }
}
