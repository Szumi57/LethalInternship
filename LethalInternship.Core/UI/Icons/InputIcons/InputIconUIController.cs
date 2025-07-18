using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LethalInternship.Core.UI.Icons.InputIcons
{
    public class InputIconUIController : MonoBehaviour
    {
        private RectTransform rectTransformIcon = null!;

        private bool isIconInCenter;
        public bool IsIconInCenter { get => isIconInCenter; }

        private GameObject ImageTopPrefab = null!;
        private Image ImageTop = null!;

        private Image ImageBottom = null!;

        void Start()
        {
            rectTransformIcon = GetComponent<RectTransform>();

            ImageBottom = GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "PointerIconImage");
            UpdateImagesOnTop();
        }

        public void SetImageOnTop(GameObject image)
        {
            ImageTopPrefab = image;
        }

        private void UpdateImagesOnTop()
        {
            if (ImageTopPrefab == null)
            {
                Object.Destroy(GetComponentsInChildren<Image>().FirstOrDefault(x => x.name != "PointerIconImage").gameObject);

                ImageTop = Object.Instantiate(PluginRuntimeProvider.Context.DefaultIconImagePrefab).GetComponent<Image>();
                ImageTop.transform.SetParent(this.transform);
            }
            else
            {
                Object.Destroy(GetComponentsInChildren<Image>().FirstOrDefault(x => x.name != "PointerIconImage").gameObject);

                // Add image
                GameObject imageInstantiated = Object.Instantiate(ImageTopPrefab);
                ImageTop = imageInstantiated.GetComponent<Image>();
                imageInstantiated.transform.SetParent(this.transform);
                imageInstantiated.transform.SetAsFirstSibling();
            }
        }

        public void PlaceOnCenterCanvas()
        {
            if (rectTransformIcon == null)
            {
                return;
            }

            Vector3 screenPos = new Vector3(0f, 0f, 10f);
            float size = 1f / screenPos.z * 400f;

            // Size
            rectTransformIcon.sizeDelta = new Vector2(size, size);

            // Position
            rectTransformIcon.localPosition = new Vector3(screenPos.x, screenPos.y, 0f);
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
    }
}
