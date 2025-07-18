using LethalInternship.SharedAbstractions.UI;
using UnityEngine;

namespace LethalInternship.Core.UI.Icons.WorldIcons
{
    public class WorldIconUI : IIconUI
    {
        public string Key => key;
        private string key;

        public bool IsIconActive => iconGameObject.activeSelf;
        public bool IsIconInCenter => IsIconActive && iconUIController.IsIconInCenter;
        public Vector3 IconWorldPosition => iconWorldPosition;

        private GameObject iconGameObject;
        private RectTransform rectTransformCanvasOverlay;
        //private List<GameObject> images;

        private Vector3 iconWorldPosition;

        private WorldIconUIController iconUIController;

        public WorldIconUI(GameObject iconGameObject, IIconUIInfos iconUIInfos, RectTransform rectTransformCanvasOverlay)
        {
            this.iconGameObject = iconGameObject;
            this.key = iconUIInfos.GetUIKey();
            this.rectTransformCanvasOverlay = rectTransformCanvasOverlay;

            //images = new List<GameObject>();
            //foreach (GameObject prefab in iconUIInfos.GetImagesPrefab())
            //{
            //    images.Add(prefab);
            //}

            iconUIController = this.iconGameObject.GetComponentInChildren<WorldIconUIController>();
            iconUIController.SetImagesOnTop(iconUIInfos.GetImagesPrefab());

            SetIconActive(false);
        }

        public void SetPositionUI(Vector3 worldPosition)
        {
            iconWorldPosition = worldPosition;
            Vector3 screenPos = WorldSpaceToCanvas(rectTransformCanvasOverlay, StartOfRound.Instance.localPlayerController.gameplayCamera, worldPosition);
            iconUIController.PlaceOnCanvas(screenPos, rectTransformCanvasOverlay);
        }

        public void SetColorIcon(Color color)
        {
            iconUIController.SetColor(color);
        }

        public void SetDefaultColor()
        {
            // r255 g111 b1 #ff6f01
            iconUIController.SetColor(new Color(255 / 255f, 111 / 255f, 1 / 255f));
        }

        public void SetIconActive(bool toActive)
        {
            iconGameObject.SetActive(toActive);
        }

        public void TriggerPingAnimation()
        {
            iconUIController.TriggerPingAnimation();
        }

        public static Vector3 WorldSpaceToCanvas(RectTransform canvasRect, Camera camera, Vector3 worldPos)
        {
            // https://discussions.unity.com/t/how-to-convert-from-world-space-to-canvas-space/117981/16
            Vector3 viewportPosition = camera.WorldToViewportPoint(worldPos);
            Vector3 canvasPos = new Vector3(viewportPosition.x * canvasRect.sizeDelta.x - canvasRect.sizeDelta.x * 0.5f,
                                            viewportPosition.y * canvasRect.sizeDelta.y - canvasRect.sizeDelta.y * 0.5f,
                                            Mathf.Abs(viewportPosition.z));

            // If pos behind
            if (viewportPosition.z < 0)
            {
                if (canvasPos.x > 0)
                {
                    canvasPos.x = -Mathf.Infinity;
                }
                else
                {
                    canvasPos.x = Mathf.Infinity;
                }

                canvasPos.y *= -1f;
            }

            return canvasPos;
        }
    }
}
