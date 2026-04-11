using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.InternBlocks
{
    public class InternBlockUI : MonoBehaviour, IRefreshableUI
    {
        private enum EnumBehaviourIcon
        {
            None = 0,
            Follow,
            Scavenge,
            GoToShip,
            GoToCruiser,
            GoToGatheringPoint,
            Fighting,
        }

        public static System.Action OnSelected = null!;

        public TextMeshProUGUI NameText = null!;
        public TextMeshProUGUI ItemCountText = null!;

        public Image BackgroundImage = null!;

        public Image ObjectiveIcon = null!;
        public Image BehaviourIcon = null!;

        public Sprite[] ObjectiveSprites = null!;

        public Sprite SpriteAutoDefense = null!;
        public Sprite SpriteFlee = null!;
        public Sprite SpriteDead = null!;

        private IInternIdentity identity = null!;

        // Typing animation
        private string fullText = string.Empty;
        private string cursorChar = "$";
        private float cursorBlink = 0.2f;
        private Coroutine typingCoroutine = null!;
        private Coroutine cursorCoroutine = null!;
        private bool showCursor = true;
        private string currentText = string.Empty;

        private bool isNotInteractable;
        private string tooltipMessageNotInteractable = string.Empty;
        private string tooltipMessage => isNotInteractable ? tooltipMessageNotInteractable : "InternBlockUI";
        private bool isStateValid => identity.Alive && !isNotInteractable;

        void OnEnable()
        {
            TMP_FontAsset fontToUse = UIManager.Instance.FontToUse;
            NameText.font = fontToUse;
            ItemCountText.font = fontToUse;

            SetBackgroundNotHovered();

            if (!string.IsNullOrWhiteSpace(fullText))
            {
                // Typing animation
                NameText.text = string.Empty;
                currentText = string.Empty;
                typingCoroutine = StartCoroutine(TypeText());
                cursorCoroutine = StartCoroutine(CursorBlink());
            }
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            this.tooltipMessageNotInteractable = tooltipMessageNotInteractable;
            if (isNotInteractable == interactable)
            {
                isNotInteractable = !interactable;
                Refresh();
            }
        }

        public void Setup(IInternIdentity i)
        {
            identity = i;
            fullText = i.Name;

            // Typing animation
            NameText.text = string.Empty;
            currentText = string.Empty;
            typingCoroutine = StartCoroutine(TypeText());
            cursorCoroutine = StartCoroutine(CursorBlink());

            Refresh();
        }

        public void Refresh()
        {
            ItemCountText.transform.parent.gameObject.SetActive(isStateValid);
            ObjectiveIcon.transform.parent.gameObject.SetActive(isStateValid);

            if (isStateValid)
            {
                UpdateItemCount();
                UpdateObjective();
                UpdateBehaviour();
            }
            else
            {
                SetBackgroundNotHovered();
                BehaviourIcon.sprite = SpriteDead;
            }
        }

        public void UpdateItemCount()
        {
            if (!isStateValid) return;

            ItemCountText.transform.parent.gameObject.SetActive(true);
            IInternAI? intern = identity.InternAI;
            if (intern != null)
            {
                ItemCountText.text = intern.GetNbHeldItems().ToString();
            }
        }

        public void UpdateObjective()
        {
            if (!isStateValid) return;

            ObjectiveIcon.transform.parent.gameObject.SetActive(true);

            //EnumInputAction objective =
            EnumInputAction objective = EnumInputAction.FollowMe;
            if (ObjectiveSprites == null
                || ObjectiveSprites.Length == 0)
            {
                PluginLoggerHook.LogWarning?.Invoke($"InternBlockUI no objective sprites available !");
                return;
            }

            switch (objective)
            {
                case EnumInputAction.FollowMe:
                    ObjectiveIcon.sprite = ObjectiveSprites[(int)EnumBehaviourIcon.Follow];
                    break;
                default:
                    ObjectiveIcon.sprite = ObjectiveSprites[(int)EnumBehaviourIcon.None];
                    break;
            }
        }

        public void UpdateBehaviour()
        {
            if (!isStateValid) return;

            BehaviourIcon.sprite = identity.AutoDefense ? SpriteAutoDefense : SpriteFlee;
            Debug.Log($"BehaviourIcon.sprite {BehaviourIcon.sprite.name}");
        }

        private void SetBackgroundNotHovered()
        {
            SetAlpha(BackgroundImage, 100f / 255f);
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
            NameText.text = currentText + (showCursor ? cursorChar : " ");
        }

        #region Events

        public void Selected()
        {
            if (!isStateValid) return;

            IdentitySelectionService.Instance.SelectSingle(identity);
            OnSelected?.Invoke();
        }

        public void MouseOver()
        {
            UIManager.Instance.ToolTipBarUI.RequestShow(tooltipMessage);

            if (!isStateValid) return;

            if (identity.InternAI != null)
                CameraFocusUI.Instance.FocusOnIntern(identity.InternAI.Npc.transform);

            SetAlpha(BackgroundImage, 1f);
        }

        public void MouseLeave()
        {
            UIManager.Instance.ToolTipBarUI.Hide();
            SetBackgroundNotHovered();
        }

        #endregion
    }
}
