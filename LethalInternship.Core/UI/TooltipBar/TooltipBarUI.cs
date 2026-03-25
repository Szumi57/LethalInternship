using LethalInternship.Core.Managers;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.TooltipBar
{
    public class TooltipBarUI : MonoBehaviour
    {
        public static TooltipBarUI Instance { get; private set; } = null!;

        public CanvasGroup CanvasGroup = null!;
        public TMP_Text Text = null!;
        public Image icon = null!;
        public Image holdFill = null!;
        public RectTransform root = null!;

        private Canvas canvas = null!;
        private RectTransform content = null!;

        private Vector2 offset = new Vector2(0, 10);
        private float showDelay = 0.8f;

        private Coroutine showRoutine = null!;
        private Coroutine holdRoutine = null!;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            HideImmediate();

            canvas = this.transform.parent.parent.GetComponent<Canvas>();
            content = Text.transform.parent.GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            Text.font = UIManager.Instance.FontToUse;
        }

        void Update()
        {
            if (!UIManager.Instance.IsCommandsAllOpened)
                return;

            // Follow the mouse
            Vector2 mousePos = Mouse.current.position.ReadValue();

            RectTransform? canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                canvasRect,
                                mousePos,
                                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                                out Vector2 localPos);

            Vector2 pos = localPos + offset;

            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 contentSize = content.rect.size;

            // right clamp
            float maxX = canvasSize.x / 2f - contentSize.x;
            pos.x = Mathf.Min(pos.x, maxX);

            // top clamp
            float maxY = canvasSize.y / 2f - contentSize.y;
            pos.y = Mathf.Min(pos.y, maxY);

            // left / bottom clamp
            float minX = -canvasSize.x / 2f;
            float minY = -canvasSize.y / 2f;

            pos.x = Mathf.Max(pos.x, minX);
            pos.y = Mathf.Max(pos.y, minY);

            root.anchoredPosition = pos;
        }

        public void ShowImmediate(Sprite iconSprite = null!)
        {
            if (icon != null)
            {
                icon.enabled = iconSprite != null;
                icon.sprite = iconSprite;
            }

            CanvasGroup.alpha = 1f;
            CanvasGroup.blocksRaycasts = false;
        }

        public void RequestShow(string message, Sprite iconSprite = null!)
        {
            CancelAll();
            showRoutine = StartCoroutine(ShowAfterDelay(message, iconSprite));
        }

        public void Hide()
        {
            CancelAll();
            HideImmediate();
        }

        IEnumerator ShowAfterDelay(string message, Sprite iconSprite)
        {
            Text.text = message;

            // If already shown, change the text
            if (CanvasGroup.alpha > 0f)
            {
                yield return null;
                ShowImmediate(iconSprite);
            }
            else
            {
                yield return new WaitForSeconds(showDelay);
                ShowImmediate(iconSprite);
            }
        }

        public void StartHold(float duration, System.Action onComplete)
        {
            holdRoutine = StartCoroutine(HoldRoutine(duration, onComplete));
        }

        public void StopHold()
        {
            if (holdRoutine != null)
                StopCoroutine(holdRoutine);

            holdFill.fillAmount = 0f;
            holdFill.transform.parent.gameObject.SetActive(false);
        }

        IEnumerator HoldRoutine(float duration, System.Action onComplete)
        {
            CanvasGroup.alpha = 1f;
            CanvasGroup.blocksRaycasts = false;
            holdFill.transform.parent.gameObject.SetActive(true);
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                holdFill.fillAmount = t / duration;
                yield return null;
            }

            holdFill.fillAmount = 1f;
            onComplete?.Invoke();
        }

        void HideImmediate()
        {
            CanvasGroup.alpha = 0f;
            holdFill.fillAmount = 0f;
            holdFill.transform.parent.gameObject.SetActive(false);
        }

        void CancelAll()
        {
            if (showRoutine != null) StopCoroutine(showRoutine);
            if (holdRoutine != null) StopCoroutine(holdRoutine);
        }
    }
}
