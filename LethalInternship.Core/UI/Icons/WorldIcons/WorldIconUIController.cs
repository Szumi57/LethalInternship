using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace LethalInternship.Core.UI.Icons.WorldIcons
{
    [ExecuteInEditMode]
    public class WorldIconUIController : MonoBehaviour
    {
        private RectTransform iconRT = null!;

        public bool IsIconInCenter { get; private set; }
        public GameObject[] Icons = null!;
        public Image ImageBottom = null!;

        private HashSet<Image> ImagesTop = new HashSet<Image>();

        private float sizeScale = 1f;
        private float animScale = 1f;
        private float distanceAlpha;

        // Fade out
        private Coroutine fadeRoutine = null!;
        private bool startFadeRoutineRequested;
        private bool stopFadeRoutineRequested;
        private bool forceVisible;
        private float fadeDuration = 10f;
        private float fadeAlpha = 1f;

        // Animation
        private float baseScale = 1f;

        private bool startPingRoutineRequested;
        private float pingOvershoot = 1.5f;
        private float pingUndershoot = 0.8f;
        private float pingDuration = 0.35f;

        private bool startFocusRoutineRequested;
        private float focusScale = 1.5f;
        private float focusInDuration = 0.15f;
        private float focusOutDuration = 0.15f;

        private Coroutine currentPingAnimationRoutine = null!;
        private Coroutine currentFocusAnimationRoutine = null!;

        // Start is called before the first frame update
        void Start()
        {
            iconRT = GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            startFadeRoutineRequested = true;
        }

        void Update()
        {
            if (startFadeRoutineRequested)
            {
                FadeOut(fadeDuration);
                startFadeRoutineRequested = false;
            }
            if (stopFadeRoutineRequested)
            {
                if (fadeRoutine != null)
                {
                    StopCoroutine(fadeRoutine);
                }

                fadeRoutine = null!;
                stopFadeRoutineRequested = false;
            }

            if (startPingRoutineRequested)
            {
                StartNewPingRoutine();
                startPingRoutineRequested = false;
            }

            if (startFocusRoutineRequested)
            {
                if (IsIconInCenter)
                    StartNewFocusRoutine(FocusInRoutine());
                else
                    StartNewFocusRoutine(FocusOutRoutine());

                startFocusRoutineRequested = false;
            }
        }

        void LateUpdate()
        {
            iconRT.localScale = Vector3.one * sizeScale * animScale;
            SetTransparency(distanceAlpha * (forceVisible ? 1f : fadeAlpha));
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
                    ImagesTop.Add(Icons[index].GetComponent<Image>() ?? Icons[index].GetComponentInChildren<Image>());
                }
            }

            SetAllImagesColor(UIConst.UI_COLOR_ORANGE);
        }

        public void PlaceOnCanvas(Vector3 screenPos, RectTransform rectTransformCanvasParent)
        {
            if (iconRT == null)
                return;

            // Size
            if (screenPos.z != 0f)
            {
                // alpha with distance
                float t = Mathf.Clamp01(screenPos.z / 5f);
                distanceAlpha = Mathf.Pow(t, 0.5f);

                // Size with distance
                float size = Mathf.Clamp((1 / screenPos.z) + 0.3f, 0.3f, 1f);
                sizeScale = size;
            }

            // Limit the image to screen borders
            //if (screenPos.x - rectTransformIcon.sizeDelta.x * 0.5f < rectTransformCanvasParent.sizeDelta.x * -0.5f)
            //{
            //    screenPos.x = rectTransformCanvasParent.sizeDelta.x * -0.5f + rectTransformIcon.sizeDelta.x * 0.5f;
            //}
            //if (screenPos.x + rectTransformIcon.sizeDelta.x * 0.5f > rectTransformCanvasParent.sizeDelta.x * 0.5f)
            //{
            //    screenPos.x = rectTransformCanvasParent.sizeDelta.x * 0.5f - rectTransformIcon.sizeDelta.x * 0.5f;
            //}

            //if (screenPos.y < rectTransformCanvasParent.sizeDelta.y * -0.5f)
            //{
            //    screenPos.y = rectTransformCanvasParent.sizeDelta.y * -0.5f;
            //}
            //if (screenPos.y + rectTransformIcon.sizeDelta.y > rectTransformCanvasParent.sizeDelta.y * 0.5f)
            //{
            //    screenPos.y = rectTransformCanvasParent.sizeDelta.y * 0.5f - rectTransformIcon.sizeDelta.y;
            //}
            // Position
            iconRT.localPosition = new Vector3(screenPos.x, screenPos.y + iconRT.sizeDelta.y * 0.5f, 0f);

            // Is icon in center
            float xLeft = iconRT.localPosition.x - iconRT.sizeDelta.x / 2;
            float xRight = iconRT.localPosition.x + iconRT.sizeDelta.x / 2;
            float yTop = iconRT.localPosition.y + iconRT.sizeDelta.y / 2;
            float yBottom = iconRT.localPosition.y - iconRT.sizeDelta.y / 2;

            bool wasInCenter = IsIconInCenter;
            IsIconInCenter = xLeft < 0f && xRight > 0f && yTop > 0f && yBottom < 0f;
            Focus(wasInCenter != IsIconInCenter);
        }

        #region FadeOut

        public void FadeOut(float duration)
        {
            StartFadeRoutine(FadeRoutine(fadeAlpha, 0f, duration));
        }

        public void ForceVisible(bool value)
        {
            if (forceVisible == value)
                return;

            forceVisible = value;

            if (forceVisible)
            {
                StopFade();
                SetAlpha(1f);
            }
            else
            {
                startFadeRoutineRequested = true;
                stopFadeRoutineRequested = false;
            }
        }

        private void StartFadeRoutine(IEnumerator routine)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(routine);
        }

        private void StopFade()
        {
            stopFadeRoutineRequested = true;
            startFadeRoutineRequested = false;
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            float t = 0f;
            fadeAlpha = from;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                fadeAlpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            fadeAlpha = to;
        }

        private void SetAlpha(float a)
        {
            fadeAlpha = a;
        }

        #endregion

        #region Animation

        public void PingAnimation()
        {
            startPingRoutineRequested = true;
        }

        public void Focus(bool focus)
        {
            startFocusRoutineRequested = focus;
        }

        private void StartNewPingRoutine()
        {
            if (currentPingAnimationRoutine != null)
                StopCoroutine(currentPingAnimationRoutine);

            currentPingAnimationRoutine = StartCoroutine(PingRoutine());
        }

        private IEnumerator PingRoutine()
        {
            // overshoot
            yield return ScaleTo(baseScale * pingOvershoot, pingDuration * 0.4f);
            // undershoot
            yield return ScaleTo(baseScale * pingUndershoot, pingDuration * 0.3f);
            // return to base
            yield return ScaleTo(baseScale, pingDuration * 0.3f);

            currentPingAnimationRoutine = null!;
        }

        private void StartNewFocusRoutine(IEnumerator routine)
        {
            if (currentPingAnimationRoutine != null)
                return;

            if (currentFocusAnimationRoutine != null)
                StopCoroutine(currentFocusAnimationRoutine);

            currentFocusAnimationRoutine = StartCoroutine(routine);
        }

        private IEnumerator FocusInRoutine()
        {
            yield return ScaleTo(baseScale * focusScale, focusInDuration);
        }

        private IEnumerator FocusOutRoutine()
        {
            yield return ScaleTo(baseScale, focusOutDuration);
        }

        private IEnumerator ScaleTo(float target, float duration)
        {
            float start = animScale;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                animScale = Mathf.Lerp(start, target, EaseOut(t));
                yield return null;
            }

            animScale = target;
        }

        private float EaseOut(float t)
        {
            // cubic ease-out (rebond doux)
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        #endregion

        private void SetTransparency(float a)
        {
            foreach (Image image in ImagesTop)
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, a);
            }
            if (ImageBottom != null)
                ImageBottom.color = new Color(ImageBottom.color.r, ImageBottom.color.g, ImageBottom.color.b, a);
        }

        private void SetAllImagesColor(Color color)
        {
            foreach (Image image in ImagesTop)
            {
                image.color = new Color(color.r, color.g, color.b, image.color.a);
            }
            if (ImageBottom != null)
                ImageBottom.color = new Color(color.r, color.g, color.b, ImageBottom.color.a);
        }
    }
}
