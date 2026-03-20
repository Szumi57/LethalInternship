using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace LethalInternship.Core.UI.Icons.WorldIcons
{
    [ExecuteInEditMode]
    public class WorldIconUIController : MonoBehaviour
    {
        private RectTransform rectTransformIcon = null!;
        private Animator animator = null!;

        private bool isIconInCenter;
        public bool IsIconInCenter { get => isIconInCenter; }
        public GameObject[] Icons = null!;
        public Image ImageBottom = null!;

        private Image ImageTop = null!;

        private bool pingAnimationNextUpdate = false;

        // Start is called before the first frame update
        void Start()
        {
            rectTransformIcon = GetComponent<RectTransform>();
            animator = GetComponent<Animator>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (pingAnimationNextUpdate)
            {
                TriggerPingAnimation();
                pingAnimationNextUpdate = false;
            }
        }

        public void SetImagesOnTop(EnumIconImagesTypes iconImageTypes)
        {
            for (int i = 0; i < Icons.Length; i++)
                Icons[i].gameObject.SetActive(false);

            foreach (var iconType in Enum.GetValues(typeof(EnumIconImagesTypes)).Cast<EnumIconImagesTypes>())
            {
                if (iconType == EnumIconImagesTypes.None)
                    continue;

                if ((iconImageTypes & iconType) != 0)
                {
                    int index = Mathf.RoundToInt(Mathf.Log((int)iconType, 2));
                    Icons[index].gameObject.SetActive(true);
                    ImageTop = Icons[index].GetComponent<Image>();
                    ImageTop.color = UIConst.UI_COLOR_ORANGE;
                }
            }
        }

        public void PlaceOnCanvas(Vector3 screenPos, RectTransform rectTransformCanvasParent)
        {
            if (rectTransformIcon == null)
            {
                return;
            }

            // Size
            if (screenPos.z != 0f)
            {
                float size = 1f / screenPos.z * 400f;
                //PluginLoggerHook.LogDebug?.Invoke($"size {size}, dist {screenPos.z}");
                if (size < 10f) { size = 10f; }
                if (size > 200f) { size = 200f; }
                if (screenPos.z < 5f)
                {
                    SetTransparency(screenPos.z / 5f * 0.5f);
                }
                else
                {
                    SetTransparency(1f);
                }

                // Size with distance
                rectTransformIcon.sizeDelta = new Vector2(size, size);
            }

            // Limit the image to screen borders
            if (screenPos.x - rectTransformIcon.sizeDelta.x * 0.5f < rectTransformCanvasParent.sizeDelta.x * -0.5f)
            {
                screenPos.x = rectTransformCanvasParent.sizeDelta.x * -0.5f + rectTransformIcon.sizeDelta.x * 0.5f;
            }
            if (screenPos.x + rectTransformIcon.sizeDelta.x * 0.5f > rectTransformCanvasParent.sizeDelta.x * 0.5f)
            {
                screenPos.x = rectTransformCanvasParent.sizeDelta.x * 0.5f - rectTransformIcon.sizeDelta.x * 0.5f;
            }

            if (screenPos.y < rectTransformCanvasParent.sizeDelta.y * -0.5f)
            {
                screenPos.y = rectTransformCanvasParent.sizeDelta.y * -0.5f;
            }
            if (screenPos.y + rectTransformIcon.sizeDelta.y > rectTransformCanvasParent.sizeDelta.y * 0.5f)
            {
                screenPos.y = rectTransformCanvasParent.sizeDelta.y * 0.5f - rectTransformIcon.sizeDelta.y;
            }
            // Position
            rectTransformIcon.localPosition = new Vector3(screenPos.x, screenPos.y + rectTransformIcon.sizeDelta.y * 0.5f, 0f);

            // Is icon in center
            float xLeft = rectTransformIcon.localPosition.x - rectTransformIcon.sizeDelta.x / 2;
            float xRight = rectTransformIcon.localPosition.x + rectTransformIcon.sizeDelta.x / 2;
            float yTop = rectTransformIcon.localPosition.y + rectTransformIcon.sizeDelta.y / 2;
            float yBottom = rectTransformIcon.localPosition.y - rectTransformIcon.sizeDelta.y / 2;

            isIconInCenter = xLeft < 0f && xRight > 0f && yTop > 0f && yBottom < 0f;
            animator.SetBool("IsHovered", isIconInCenter);
        }

        private void SetTransparency(float alpha)
        {
            if (ImageTop != null)
            {
                ImageTop.color = new Color(ImageTop.color.r, ImageTop.color.g, ImageTop.color.b, alpha);
            }
            if (ImageBottom != null)
            {
                ImageBottom.color = new Color(ImageBottom.color.r, ImageBottom.color.g, ImageBottom.color.b, alpha);
            }
        }

        public void SetColor(Color color)
        {
            if (ImageTop != null)
            {
                ImageTop.color = new Color(color.r, color.g, color.b, ImageTop.color.a);
            }
            if (ImageBottom != null)
            {
                ImageBottom.color = new Color(color.r, color.g, color.b, ImageBottom.color.a);
            }
        }

        public void TriggerPingAnimation()
        {
            if (animator == null)
            {
                pingAnimationNextUpdate = true;
                return;
            }

            animator.ResetTrigger("Ping");
            animator.SetTrigger("Ping");
        }
    }
}
