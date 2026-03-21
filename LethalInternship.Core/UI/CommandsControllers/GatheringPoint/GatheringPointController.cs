using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.GatheringPoint
{
    public class GatheringPointController : MonoBehaviour
    {
        public System.Action<EnumInputAction> OnSelected = null!;

        public Image BgImage = null!;
        public Image BgRemoveButtonImage = null!;

        public Image SetImage = null!;
        public Image GoToImage = null!;

        public TextMeshProUGUI TMPDescription = null!;

        public RemoveGatheringPointController removeGatheringPointController = null!;

        float transparency = 1f;

        public bool IsNotAvailable;

        // ?
        bool gatheringPointSet = false;

        // Typing animation
        private string fullText = string.Empty;
        private string cursorChar = "$";
        private float cursorBlink = 0.2f;
        private Coroutine typingCoroutine = null!;
        private Coroutine cursorCoroutine = null!;
        private bool showCursor = true;
        private string currentText = string.Empty;

        void Awake()
        {
            removeGatheringPointController.OnSelected += RemoveButtonSelected;
        }

        void OnEnable()
        {
            SetButtonNotHovered();
            SetTMPDescriptionFont(UIManager.Instance.FontToUse);

            UpdateStateRemoveButton();
        }

        void Start()
        {
            SetAlpha(SetImage, 1f);
            SetAlpha(GoToImage, 0f);
            SetButtonNotHovered();
        }

        // Update is called once per frame
        void Update()
        {
            // Transparency
            if (IsNotAvailable)
            {
                SetAlpha(GetCurrentImage(), 0.2f);
            }
            else
            {
                SetAlpha(GetCurrentImage(), transparency);
                if (removeGatheringPointController != null)
                {
                    removeGatheringPointController.IsNotAvailable = false;
                }
            }
        }

        private Image GetCurrentImage()
        {
            return gatheringPointSet ? GoToImage : SetImage;
        }

        private void SetAlpha(Image image, float transparency)
        {
            if (image != null
                && image.color.a != transparency)
            {
                Color alpha = image.color;
                alpha.a = transparency;
                image.color = alpha;
            }
        }

        private EnumInputAction GetCurrentInputAction()
        {
            return gatheringPointSet ? EnumInputAction.GoToGatheringPoint : EnumInputAction.SetGatheringPoint;
        }

        private void SetTMPDescriptionFont(TMP_FontAsset font)
        {
            if (TMPDescription != null)
            {
                TMPDescription.font = font;
            }
        }

        private void SetButtonHovered()
        {
            SetAlpha(BgImage, 1f);

            if (gatheringPointSet)
            {
                SetAlpha(BgRemoveButtonImage, 1f);
            }

            fullText = UIConst.COMMANDS_BUTTON_STRING[(int)GetCurrentInputAction()];
            if (TMPDescription != null)
            {
                TMPDescription.text = "";
                typingCoroutine = StartCoroutine(TypeText());
                cursorCoroutine = StartCoroutine(CursorBlink());
            }
        }

        private void SetButtonNotHovered()
        {
            SetAlpha(BgImage, 0f);

            // Typing animation
            StopAllCoroutines();
            if (TMPDescription != null)
            {
                TMPDescription.text = string.Empty;
                currentText = string.Empty;
            }
        }

        private void UpdateIconAndDesc()
        {
            if (gatheringPointSet)
            {
                SetAlpha(GoToImage, 1f);
                SetAlpha(SetImage, 0f);
            }
            else
            {
                SetAlpha(GoToImage, 0f);
                SetAlpha(SetImage, 1f);
            }
        }

        private void UpdateStateRemoveButton()
        {
            if (removeGatheringPointController != null)
            {
                removeGatheringPointController.gameObject.SetActive(gatheringPointSet);
            }
        }

        IEnumerator TypeText()
        {
            foreach (char c in fullText)
            {
                currentText += c;
                UpdateText();
                yield return new WaitForSeconds(Random.Range(0.02f, 0.07f));
            }
        }

        IEnumerator CursorBlink()
        {
            while (currentText != fullText)
            {
                showCursor = !showCursor;
                UpdateText();
                yield return new WaitForSeconds(cursorBlink);
            }
            // No cursor after the end
            showCursor = false;
            UpdateText();
        }

        void UpdateText()
        {
            TMPDescription.text = currentText + (showCursor ? cursorChar : " ");
        }

        public void Selected()
        {
            gatheringPointSet = !gatheringPointSet;

            OnSelected?.Invoke(GetCurrentInputAction());

            UpdateIconAndDesc();
            UpdateStateRemoveButton();
        }

        private void RemoveButtonSelected(EnumInputAction enumInputAction)
        {
            PluginLoggerHook.LogDebug?.Invoke($"RemoveButtonSelected {enumInputAction} click !");
        }

        public void MouseOver()
        {
            if (IsNotAvailable)
            {
                return;
            }

            SetButtonHovered();
        }

        public void MouseLeave()
        {
            if (IsNotAvailable)
            {
                return;
            }

            SetButtonNotHovered();
        }
    }
}
