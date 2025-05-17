using LethalInternship.SharedAbstractions.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.Icons.WorldIcons
{
    [ExecuteInEditMode]
    public class WorldIconUIController : MonoBehaviour, IIconUIController
    {
        [SerializeField]
        private float minWidth;
        [SerializeField]
        private float minHeight;

        public float MinWidth
        {
            get { return minWidth; }
            set
            {
                minWidth = value;
                if (minWidth < 0f)
                {
                    minWidth = 0f;
                }
            }
        }
        public float MinHeight
        {
            get { return minHeight; }
            set
            {
                minHeight = value;
                if (minHeight <= 0f)
                {
                    minHeight = 1f;
                }
            }
        }

        private RectTransform rectTransformIcon = null!;

        private bool isIconInCenter;
        public bool IsIconInCenter { get => isIconInCenter; }

        private Image ImageTop = null!;
        private Image ImageBottom = null!;

        // Start is called before the first frame update
        void Start()
        {
            rectTransformIcon = GetComponent<RectTransform>();
            ImageTop = GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "Top");
            ImageBottom = GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "Bottom");
        }

        // https://discussions.unity.com/t/public-variable-with-setter-isnt-showing-in-inspector/583932/4
        // is called by Unity when ever a value in the inspector is changed
        private void OnValidate()
        {
            MinWidth = minWidth;
            MinHeight = minHeight;
        }

        // Update is called once per frame
        private void Update()
        {
            if (rectTransformIcon == null)
            {
                return;
            }

            if (rectTransformIcon.sizeDelta.x < MinWidth)
            {
                rectTransformIcon.sizeDelta = new Vector2(MinWidth, rectTransformIcon.sizeDelta.y);
            }
            if (rectTransformIcon.sizeDelta.y < MinHeight)
            {
                rectTransformIcon.sizeDelta = new Vector2(rectTransformIcon.sizeDelta.x, MinHeight);
            }

            rectTransformIcon.sizeDelta = new Vector2(MinWidth / MinHeight * rectTransformIcon.sizeDelta.y, rectTransformIcon.sizeDelta.y);
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
                rectTransformIcon.sizeDelta = new Vector2(rectTransformIcon.sizeDelta.x, size);
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
            if (isIconInCenter)
            {
                rectTransformIcon.sizeDelta *= 1.5f;
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
    }
}
