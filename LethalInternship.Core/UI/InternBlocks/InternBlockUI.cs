using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
using LethalInternship.Core.UI.Others;
using LethalInternship.Core.UI.TooltipBar;
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
            if (identity.Alive)
            {
                UpdateItemCount();
                UpdateObjective();
                UpdateBehaviour();
                return;
            }

            // Dead
            SetBackgroundNotHovered();
            ItemCountText.transform.parent.gameObject.SetActive(false);
            ObjectiveIcon.transform.parent.gameObject.SetActive(false);
            BehaviourIcon.sprite = SpriteDead;
        }

        public void UpdateItemCount()
        {
            if (!identity.Alive) return;

            ItemCountText.transform.parent.gameObject.SetActive(true);
            IInternAI? intern = identity.InternAI;
            if (intern != null)
            {
                ItemCountText.text = intern.GetNbHeldItems().ToString();
            }
        }

        public void UpdateObjective()
        {
            if (!identity.Alive) return;

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
            if (!identity.Alive) return;

            bool autoDef = true;
            BehaviourIcon.sprite = autoDef ? SpriteAutoDefense : SpriteFlee;
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
            if (!identity.Alive) return;

            IdentitySelectionService.Instance.SelectSingle(identity);
            OnSelected?.Invoke();
        }

        public void MouseOver()
        {
            if (!identity.Alive) return;

            TooltipBarUI.Instance.RequestShow($"block UI {NameText.text}, click to ...");

            if (identity.InternAI != null)
            {
                CameraFocusUI.Instance.FocusOnIntern(identity.InternAI.Npc.transform);
            }

            SetAlpha(BackgroundImage, 1f);
        }

        public void MouseLeave()
        {
            if (!identity.Alive) return;

            TooltipBarUI.Instance.Hide();

            SetBackgroundNotHovered();
        }

        #endregion
    }
}
