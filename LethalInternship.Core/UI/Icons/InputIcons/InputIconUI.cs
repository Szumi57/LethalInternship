using LethalInternship.SharedAbstractions.UI;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.UI.Icons.InputIcons
{
    public class InputIconUI : IIconUI
    {
        public string Key => key;
        private string key;

        private GameObject iconGameObject;
        private RectTransform rectTransformCanvasOverlay;

        private InputIconUIController iconUIController;

        public InputIconUI(GameObject iconGameObject, IIconUIInfos iconUIInfos, RectTransform rectTransformCanvasOverlay)
        {
            this.iconGameObject = iconGameObject;
            this.key = iconUIInfos.GetUIKey();
            this.rectTransformCanvasOverlay = rectTransformCanvasOverlay;

            iconUIController = this.iconGameObject.GetComponentInChildren<InputIconUIController>();
            iconUIController.SetImageOnTop(iconUIInfos.GetImagesPrefab().First());

            SetIconActive(false);
        }

        public void SetPositionUICenter()
        {
            iconUIController.PlaceOnCenterCanvas();
            SetIconActive(true);
        }

        public void SetColorIconValidOrNot(bool isValidNavMeshPoint)
        {
            if (isValidNavMeshPoint)
            {
                iconUIController.SetColor(Color.green);
            }
            else
            {
                iconUIController.SetColor(Color.red);
            }
        }

        public void SetIconActive(bool active)
        {
            iconGameObject.SetActive(active);
        }
    }
}
