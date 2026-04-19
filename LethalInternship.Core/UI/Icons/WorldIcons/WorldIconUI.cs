using LethalInternship.SharedAbstractions.UI;
using UnityEngine;

namespace LethalInternship.Core.UI.Icons.WorldIcons
{
    public class WorldIconUI : IIconUI
    {
        public int Key => key;
        private int key;

        public bool IsIconActive => iconGameObject.activeSelf;
        public bool IsIconInCenter => IsIconActive && iconUIController.IsIconInCenter;
        public Vector3 IconWorldPosition => iconWorldPosition;

        private GameObject iconGameObject;
        private RectTransform rectTransformCanvasOverlay;

        private Vector3 iconWorldPosition;

        private WorldIconUIController iconUIController;

        public WorldIconUI(GameObject iconGameObject, IIconUIInfos iconUIInfos, RectTransform rectTransformCanvasOverlay)
        {
            this.iconGameObject = iconGameObject;
            this.key = iconUIInfos.GetUIKey();
            this.rectTransformCanvasOverlay = rectTransformCanvasOverlay;

            iconUIController = this.iconGameObject.GetComponentInChildren<WorldIconUIController>();
            iconUIController.SetImagesOnTop(iconUIInfos.IconImagesTypes);

            SetIconActive(false);
        }

        public void SetPositionUI(Vector3 worldPosition)
        {
            iconWorldPosition = worldPosition;
            Vector3 screenPos = WorldSpaceToCanvas(rectTransformCanvasOverlay, StartOfRound.Instance.localPlayerController.gameplayCamera, worldPosition);
            iconUIController.PlaceOnCanvas(screenPos, rectTransformCanvasOverlay);
        }

        public void SetIconActive(bool toActive)
        {
            iconGameObject.SetActive(toActive);
        }

        public void TriggerPingAnimation()
        {
            iconUIController.PingAnimation();
        }

        public void ForceVisible(bool value)
        {
            iconUIController.ForceVisible(value);
        }

        public static Vector3 WorldSpaceToCanvas(RectTransform canvasRect, Camera camera, Vector3 worldPos)
        {
            // https://discussions.unity.com/t/how-to-convert-from-world-space-to-canvas-space/117981/16
            Vector3 viewportPosition = camera.WorldToViewportPoint(worldPos);
            Vector3 canvasPos = new Vector3(viewportPosition.x * canvasRect.sizeDelta.x - canvasRect.sizeDelta.x * 0.5f,
                                            viewportPosition.y * canvasRect.sizeDelta.y - canvasRect.sizeDelta.y * 0.5f,
                                            viewportPosition.z);

            // If pos behind
            //if (viewportPosition.z < 0)
            //{
            //    if (canvasPos.x > 0)
            //    {
            //        canvasPos.x = -Mathf.Infinity;
            //    }
            //    else
            //    {
            //        canvasPos.x = Mathf.Infinity;
            //    }

            //    canvasPos.y *= -1f;
            //}

            return canvasPos;
        }
    }
}
