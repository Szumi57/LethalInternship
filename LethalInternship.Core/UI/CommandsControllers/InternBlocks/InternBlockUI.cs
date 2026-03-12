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
        private IInternAI internAI = null!;

        void OnEnable()
        {
            TMP_FontAsset fontToUse = UIManager.Instance.FontToUse;
            NameText.font = fontToUse;
            ItemCountText.font = fontToUse;

            SetBackgroundNotHovered();
        }

        public void Setup(IInternAI i)
        {
            internAI = i;
            NameText.text = i.Npc.playerUsername;

            Refresh();
        }

        public void Refresh()
        {
            UpdateItemCount();
            UpdateObjective();
            UpdateBehaviour();
        }

        public void UpdateItemCount()
        {
            ItemCountText.text = internAI.GetNbHeldItems().ToString();
        }

        public void UpdateObjective()
        {
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
            bool autoDef = true;
            BehaviourIcon.sprite = autoDef ? SpriteAutoDefense : SpriteFlee;
        }

        private void SetBackgroundNotHovered()
        {
            SetAlpha(BackgroundImage, 67f / 255f);
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
            PluginLoggerHook.LogDebug?.Invoke($"InternBlockUI intern {internAI.Npc.playerClientId} {internAI.Npc.playerUsername} clicked");
        }

        public void MouseOver()
        {
            SetAlpha(BackgroundImage, 1f);
        }

        public void MouseLeave()
        {
            SetBackgroundNotHovered();
        }

        #endregion
    }
}
