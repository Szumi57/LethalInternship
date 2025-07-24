using System;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandButton
{
    public class CommandButtonController : MonoBehaviour
    {
        public event EventHandler OnSelected = null!;

        public int ID;
        public Image CommandFrameImage;
        public Image CommandIcon;

        public bool IsNotAvailable;

        // Start is called before the first frame update
        void Start()
        {
            if (CommandFrameImage == null)
            {
                CommandFrameImage = GetComponent<Image>();
            }
            if (CommandIcon == null)
            {
                CommandIcon = GetComponentInChildren<Image>();
            }
        }

        // Update is called once per frame
        void Update()
        {
            float transparency = 1f;
            if (IsNotAvailable)
            {
                transparency = 0.2f;
            }

            SetTransparency(CommandFrameImage, transparency);
            SetTransparency(CommandIcon, transparency);
        }

        private void SetTransparency(Image image, float transparency)
        {
            if (image != null)
            {
                Color __alpha = image.color;
                __alpha.a = transparency;
                image.color = __alpha;
            }
        }

        public void Selected()
        {
            OnSelected?.Invoke(this, null);
        }
    }
}
