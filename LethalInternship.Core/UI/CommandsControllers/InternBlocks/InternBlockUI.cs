using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalInternship.Core.UI.CommandsControllers.InternBlocks
{
    public class InternBlockUI : MonoBehaviour
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

        void OnEnable()
        {
            TMP_FontAsset fontToUse = UIManager.Instance.FontToUse;
            NameText.font = fontToUse;
            ItemCountText.font = fontToUse;

            SetBackgroundNotHovered();
        }

        public void Setup(IInternIdentity i)
        {
            identity = i;
            NameText.text = i.Name;

            Refresh();
        }

        public void Refresh()
        {
            UpdateItemCount();
            UpdateObjective();
            UpdateBehaviour();
        }

        public void UpdateDeadInternState()
        {
            if (identity.Alive)
            {
                Refresh();
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

        #region Events

        public void Selected()
        {
            if (!identity.Alive) return;

            PluginLoggerHook.LogDebug?.Invoke($"InternBlockUI intern {identity.InternAI?.Npc.playerClientId} {identity.InternAI?.Npc.playerUsername} clicked");
        }

        public void MouseOver()
        {
            if (!identity.Alive) return;

            SetAlpha(BackgroundImage, 1f);
        }

        public void MouseLeave()
        {
            if (!identity.Alive) return;

            SetBackgroundNotHovered();
        }

        #endregion
    }
}
