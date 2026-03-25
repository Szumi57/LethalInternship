using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace LethalInternship.Core.UI.Icons.InputIcons
{
    public class InputIconUIController : MonoBehaviour
    {
        public RectTransform RectTransformIcon = null!;
        public GameObject[] Icons = null!;

        private Image ImageTop = null!;
        private float visibleTime = 0.5f;
        private float hiddenTime = 0.5f;
        private float hiddenTransparency = 0.5f;
        Coroutine blinkRoutine = null!;

        private float targetTransparency = 0.6f;

        public Image ImageBottom = null!;
        public RectTransform ImageBottomRectTransformIcon = null!;
        private float startScale = 3.5f;
        private float duration = 0.30f;
        Coroutine startAnim = null!;

        void OnEnable()
        {
            StartBlink();
            PlayStartAnim();
        }

        // Start after SetImageOnTop
        void Start()
        {
            Color orange = UIConst.UI_COLOR_ORANGE;
            orange.a = targetTransparency;
            ImageBottom.color = orange;

            ImageBottomRectTransformIcon = ImageBottom.GetComponent<RectTransform>();
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
            float size = 1f / screenPos.z * 1400f;

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
        }

        IEnumerator Blink()
        {
            while (ImageTop == null)
            {
                yield return null;
                continue;
            }

            Color alpha = ImageTop.color;

            while (true)
            {
                alpha.a = 1f;
                ImageTop.color = alpha;
                yield return new WaitForSeconds(visibleTime);

                alpha.a = hiddenTransparency;
                ImageTop.color = alpha;
                yield return new WaitForSeconds(hiddenTime);
            }
        }

        public void PlayStartAnim()
        {
            if (startAnim != null)
                StopCoroutine(startAnim);

            startAnim = StartCoroutine(SartAnim());
        }

        IEnumerator SartAnim()
        {
            while (ImageBottomRectTransformIcon == null)
            {
                yield return null;
                continue;
            }

            ImageBottomRectTransformIcon.localScale = Vector3.one * startScale;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = t / duration;

                // Ease-out (fast at first)
                float eased = 1f - Mathf.Pow(1f - k, 3f);

                ImageBottomRectTransformIcon.localScale = Vector3.LerpUnclamped(
                    Vector3.one * startScale,
                    Vector3.one,
                    eased
                );
                yield return null;
            }

            ImageBottomRectTransformIcon.localScale = Vector3.one;
        }
    }
}
