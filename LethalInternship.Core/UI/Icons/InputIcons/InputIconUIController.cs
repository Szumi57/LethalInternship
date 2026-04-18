using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.CommandsSystem;
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
        public GameObject GoImageBottom = null!;

        private Image ImageTop = null!;
        private float visibleTime = 0.5f;
        private float hiddenTime = 0.5f;
        private float hiddenTransparency = 0.5f;
        Coroutine blinkRoutine = null!;

        private float targetTransparency = 0.6f;

        private RectTransform targetRT = null!;
        private Image targetImage = null!;
        private Image centerImage = null!;
        private float startScale = 3.5f;
        private float duration = 0.30f;
        private Coroutine startAnim = null!;
        private float animScale = 1f;       // coroutine animation

        private float distanceScale = 1f;   // size distance
        private float minDist = 1f;
        private float maxDist = 60f;
        private float minScale = 0.1f;
        private float maxScale = 1f;
        private float distanceAlpha = 1f;   // size alpha
        private float minAlpha = 0f; // far
        private float maxAlpha = 1f; // close

        void OnEnable()
        {
            StartBlink();
            PlayStartAnim();
        }

        // Start after SetImageOnTop
        void Start()
        {
            targetRT = GoImageBottom.GetComponentsInChildren<RectTransform>().FirstOrDefault(x => x.name == "TargetImage");

            Color orange = UIConst.UI_COLOR_ORANGE;
            orange.a = targetTransparency;

            targetImage = GoImageBottom.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "TargetImage");
            targetImage.color = orange;

            centerImage = GoImageBottom.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "CenterImage");
            centerImage.color = orange;
        }

        void Update()
        {
            PlaceOnCenterCanvas();
            SetSizeFromDistance();
        }

        void LateUpdate()
        {
            if (targetRT == null)
                return;

            float finalScale = distanceScale * animScale;
            targetRT.localScale = Vector3.one * finalScale;

            Color c = targetImage.color;
            c.a = distanceAlpha;
            targetImage.color = c;
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
                    ImageTop = Icons[index].GetComponent<Image>() ?? Icons[index].GetComponentInChildren<Image>();
                    ImageTop.color = UIConst.UI_COLOR_ORANGE;
                    return;// just the first icon found
                }
            }
        }

        private void PlaceOnCenterCanvas()
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

        private void SetSizeFromDistance()
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
                return;

            TargetData? target = TargetingManager.Instance.GetCurrentTarget();
            if (target == null)
            {
                distanceScale = 1f;
                return;
            }

            float dist = target.Value.Distance;

            float t = Mathf.InverseLerp(minDist, maxDist, dist);
            t = Mathf.Pow(t, 0.5f);

            // Scale
            distanceScale = Mathf.Lerp(maxScale, minScale, t);

            // Alpha
            distanceAlpha = Mathf.Lerp(maxAlpha, minAlpha, t);
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
            while (targetRT == null)
            {
                yield return null;
                continue;
            }

            animScale = startScale;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = t / duration;

                // Ease-out (fast at first)
                float eased = 1f - Mathf.Pow(1f - k, 3f);

                animScale = Mathf.LerpUnclamped(
                                startScale,
                                1f,
                                eased
                );
                yield return null;
            }

            animScale = 1f;
        }
    }
}
