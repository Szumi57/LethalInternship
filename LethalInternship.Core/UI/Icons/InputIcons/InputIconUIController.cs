using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.Icons.InputIcons
{
    public class InputIconUIController : MonoBehaviour
    {
        public RectTransform RectTransformIcon = null!;
        public Image ImageBottom = null!;
        public GameObject[] Icons = null!;

        private Image ImageTop = null!;
        private float visibleTime = 1f;
        private float hiddenTime = 0.2f;

        Coroutine blinkRoutine = null!;

        void OnEnable()
        {
            StartBlink();
        }

        // Start after SetImageOnTop
        void Start()
        {
            ImageBottom.color = UIConst.UI_COLOR_ORANGE;
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
                    ImageTop = Icons[index].GetComponent<Image>();
                    ImageTop.color = UIConst.UI_COLOR_ORANGE;
                    return;// just the first icon found
                }
            }
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

        public void StartBlink()
        {
            StopBlink();
            blinkRoutine = StartCoroutine(Blink());
        }

        public void StopBlink()
        {
            if (blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
                blinkRoutine = null!;
            }

            if (ImageTop != null)
            {
                ImageTop.enabled = false;
            }
        }

        IEnumerator Blink()
        {
            while (true)
            {
                if (ImageTop == null)
                {
                    yield return new WaitForSeconds(hiddenTime);
                    continue;
                }

                ImageTop.enabled = true;
                yield return new WaitForSeconds(visibleTime);

                ImageTop.enabled = false;
                yield return new WaitForSeconds(hiddenTime);
            }
        }
    }
}
