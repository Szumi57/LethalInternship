using LethalInternship.Core.Managers;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.TooltipBar
{
    public class TooltipBarUI : MonoBehaviour
    {
        public CanvasGroup CanvasGroup = null!;
        public TMP_Text Text = null!;
        public Image icon = null!;
        public Image holdFill = null!;
        public RectTransform root = null!;

        private Canvas canvas = null!;
        private RectTransform content = null!;

        private Vector2 offset = new Vector2(4f, 14f);
        private float showDelay = 0.7f;

        private Coroutine showRoutine = null!;
        private Coroutine holdRoutine = null!;

        void Awake()
        {
            HideImmediate();
            Text.font = UIManager.Instance.FontToUse;

            canvas = this.transform.parent.GetComponent<Canvas>();
            content = Text.transform.parent.GetComponent<RectTransform>();
        }

        void Update()
        {
            if (!UIManager.Instance.IsAnyMenuOpened)
                return;

            // Follow the mouse
            Vector2 mousePos = GetTooltipScreenPosition();

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

        private Vector2 GetTooltipScreenPosition()
        {
            if (InputManager.Instance.IsUsingController)
            {
                GameObject? selected = EventSystem.current.currentSelectedGameObject;

                if (selected != null)
                {
                    RectTransform? selectedRect = selected.transform as RectTransform;

                    if (selectedRect != null)
                    {
                        Vector3[] corners = new Vector3[4];
                        selectedRect.GetWorldCorners(corners);
                        Vector3 center = (corners[0] + corners[2]) * 0.5f;

                        return RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                                                                       center);
                    }
                }
            }

            return Mouse.current.position.ReadValue();
        }

        public void ShowImmediate(string message, Sprite iconSprite = null!)
        {
            Text.text = message;
            Show(iconSprite);
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

        private void Show(Sprite iconSprite = null!)
        {
            if (icon != null)
            {
                icon.enabled = iconSprite != null;
                icon.sprite = iconSprite;
            }

            CanvasGroup.alpha = 1f;
            CanvasGroup.blocksRaycasts = false;
        }

        IEnumerator ShowAfterDelay(string message, Sprite iconSprite)
        {
            Text.text = message;

            // If already shown, change the text
            if (CanvasGroup.alpha > 0f)
            {
                yield return null;
                Show(iconSprite);
            }
            else
            {
                yield return new WaitForSeconds(showDelay);
                Show(iconSprite);
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
            if (showRoutine != null) { StopCoroutine(showRoutine); }
            if (holdRoutine != null)
            {
                StopCoroutine(holdRoutine);
            }
        }
    }
}
