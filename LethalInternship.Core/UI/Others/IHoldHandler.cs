using UnityEngine.EventSystems;

namespace LethalInternship.Core.UI.Others
{
    public interface IHoldHandler : IEventSystemHandler
    {
        void OnHoldStart();
        void OnHoldEnd();
    }
}
