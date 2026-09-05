using UnityEngine;
using UnityEngine.EventSystems;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class SuitListPanel : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        public bool Focusing;

        private float openGraceTime = 1.5f;   // first show
        private float exitDelayTime = 0.25f;  // after exiting
        private float timer;

        void OnEnable()
        {
            SetFocus(false);
            timer = openGraceTime;
        }

        void Update()
        {
            if (Focusing)
                return;

            timer -= Time.deltaTime;

            if (timer <= 0f)
                Close();
        }

        private void Close()
        {
            this.gameObject.SetActive(false);
            SetFocus(false);
        }

        public void SetFocus(bool focus)
        {
            Focusing = focus;
        }

        #region Mouse events

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetFocus(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetFocus(false);
            timer = exitDelayTime;
        }

        #endregion
    }
}
