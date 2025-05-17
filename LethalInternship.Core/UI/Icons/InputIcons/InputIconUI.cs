using LethalInternship.Core.UI.Icons.WorldIcons;
using LethalInternship.SharedAbstractions.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LethalInternship.Core.UI.Icons.InputIcons
{
    public class InputIconUI : IIconUI
    {
        public string Key => key;
        private string key;

        private GameObject iconGameObject;
        private RectTransform rectTransformCanvasOverlay;
        private List<Image> images;

        private IIconUIController iconUIController;

        public InputIconUI(GameObject iconGameObject, IIconUIInfos iconUIInfos, RectTransform rectTransformCanvasOverlay)
        {
            this.iconGameObject = iconGameObject;
            this.key = iconUIInfos.GetUIKey();
            this.rectTransformCanvasOverlay = rectTransformCanvasOverlay;

            images = new List<Image>();
            foreach (GameObject prefab in iconUIInfos.GetImagesPrefab())
            {
                images.Add(Object.Instantiate(prefab).GetComponent<Image>());
            }

            iconUIController = this.iconGameObject.GetComponentInChildren<WorldIconUIController>();
            SetIconActive(false);
        }

        public void SetPositionUICenter()
        {
            iconUIController.PlaceOnCanvas(new Vector3(0f, 0f, 10f), rectTransformCanvasOverlay);
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
