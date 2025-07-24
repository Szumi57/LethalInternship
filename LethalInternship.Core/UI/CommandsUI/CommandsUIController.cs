using LethalInternship.Core.UI.CommandsWheel;
using TMPro;
using UnityEngine;

namespace LethalInternship.Core.UI.CommandsUI
{
    public class CommandsUIController : MonoBehaviour
    {
        public CommandWheelController CommandWheelController;
        public TextMeshProUGUI TitleListInterns;
        public TextMeshProUGUI ListInterns;

        void Start()
        {
        }

        public void SetTitleListInterns(string title)
        {
            TitleListInterns.text = title;
        }

        public void SetTextListInterns(string textList)
        {
            ListInterns.text = textList;
        }
    }
}
