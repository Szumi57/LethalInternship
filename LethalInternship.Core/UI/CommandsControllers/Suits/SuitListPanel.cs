using UnityEngine;

namespace LethalInternship.Core.UI.CommandsControllers.Suits
{
    public class SuitListPanel : MonoBehaviour
    {
        public bool Hovering;

        private float openGraceTime = 2.0f;   // first show
        private float exitDelayTime = 0.25f;  // after exiting
        private float timer;

        void OnEnable()
        {
            Hovering = false;
            timer = openGraceTime;
        }

        void Update()
        {
            if (Hovering)
                return;

            timer -= Time.deltaTime;

            if (timer <= 0f)
                Close();
        }

        private void Close()
        {
            this.gameObject.SetActive(false);
            Hovering = false;
        }

        public void MouseOver()
        {
            Hovering = true;
        }

        public void MouseLeave()
        {
            Hovering = false;
            timer = exitDelayTime;
        }
    }
}
