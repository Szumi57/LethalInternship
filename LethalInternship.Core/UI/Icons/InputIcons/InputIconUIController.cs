using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.Icons.InputIcons
{
    public class InputIconUIController : MonoBehaviour
    {
        public RectTransform RectTransformIcon = null!;
        public GameObject TopIconContainer = null!;
        public GameObject IconTop = null!;
        public Image ImageBottom = null!;
        public GameObject[] Icons = null!;

        private GameObject ImageTopPrefab = null!;
        private Image ImageTop = null!;

        // Start after SetImageOnTop
        void Start()
        {
            ImageBottom.color = UIConst.UI_COLOR_ORANGE;
            UpdateImagesOnTop();
        }

        public void SetImageOnTop(GameObject image)
        {
            ImageTopPrefab = image;
        }

        public void SetImageOnTop(EnumIconImagesTypes iconImageTypes)
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
                    Icons[index].GetComponent<Image>().color = UIConst.UI_COLOR_ORANGE;
                    return;// just the first icon found
                }
            }
        }

        private void UpdateImagesOnTop()
        {


            //// Add image
            ////Object.Destroy(IconTop);
            //GameObject IconTop2 = Object.Instantiate(ImageTopPrefab == null ? PluginRuntimeProvider.Context.DefaultIconImagePrefab : ImageTopPrefab);
            //IconTop2.transform.SetParent(TopIconContainer.transform);
            ////IconTop.transform.localPosition = Vector3.zero;
            ////IconTop2.transform.SetAsFirstSibling();

            //ImageTop = IconTop2.GetComponent<Image>();
            //ImageTop.enabled = true;
            //ImageTop.color = UIConst.UI_COLOR_ORANGE;
            ////IconTop2.GetComponent<RectTransform>().rect.res;
            //PluginLoggerHook.LogDebug?.Invoke($"TopIconContainer height {TopIconContainer.GetComponent<RectTransform>().rect.height}");
            //PluginLoggerHook.LogDebug?.Invoke($"ImageTopPrefab {IconTop2.GetComponent<RectTransform>().rect.top}");
            //PluginLoggerHook.LogDebug?.Invoke($"ImageTopPrefab height {IconTop2.GetComponent<RectTransform>().rect.height}");
            //PluginLoggerHook.LogDebug?.Invoke($"ImageTopPrefab {ImageTop.name}");
            //PluginLoggerHook.LogDebug?.Invoke($"ImageTopPrefab {ImageTop.color}");
            //PluginLoggerHook.LogDebug?.Invoke($"ImageTopPrefab {ImageTop.sprite.name}");
            //PluginLoggerHook.LogDebug?.Invoke($"ImageTopPrefab {ImageTop.enabled}");

        }

        public void PlaceOnCenterCanvas()
        {
            if (RectTransformIcon == null)
            {
                return;
            }

            Vector3 screenPos = new Vector3(0f, 0f, 10f);
            float size = 1f / screenPos.z * 600f;

            // Size
            RectTransformIcon.sizeDelta = new Vector2(size, size);

            // Position
            RectTransformIcon.localPosition = new Vector3(screenPos.x, screenPos.y, 0f);
        }
    }
}
