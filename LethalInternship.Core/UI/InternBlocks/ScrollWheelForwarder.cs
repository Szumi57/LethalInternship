using UnityEngine;
using UnityEngine.EventSystems;

namespace LethalInternship.Core.UI.InternBlocks
{
    public class ScrollWheelForwarder : MonoBehaviour, IScrollHandler
    {
        public void OnScroll(PointerEventData eventData)
        {
            ExecuteEvents.ExecuteHierarchy(
                transform.parent.gameObject,
                eventData,
                ExecuteEvents.scrollHandler
            );
        }
    }
}
