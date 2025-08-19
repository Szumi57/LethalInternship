using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using TMPro;
using UnityEngine;

namespace LethalInternship.Core.UI.CommandsControllers
{
    public class CommandsMainUIController : MonoBehaviour
    {
        public CommandsPanelController CommandsPanelController;
        public TextMeshProUGUI TitleListInterns;
        public TextMeshProUGUI ListInterns;
        public TextMeshProUGUI ModNamePanelDescription;

        private TMP_FontAsset font;

        void Start()
        {
            ModNamePanelDescription.text = $"{PluginRuntimeProvider.Context.Plugin_Name} v{PluginRuntimeProvider.Context.Plugin_Version}";
        }

        public void SetTitleListInterns(string title)
        {
            TitleListInterns.text = title;
        }

        public void SetTextListInterns(string textList)
        {
            ListInterns.text = textList;
        }

        public void SetFont(TMP_FontAsset font)
        {
            this.font = font;
            TitleListInterns.font = font;
            ListInterns.font = font;
            ModNamePanelDescription.font = font;

            CommandsPanelController.SetFont(this.font);
        }
    }
}
