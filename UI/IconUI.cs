using UnityEngine;

namespace LethalInternship.UI
{
    public class IconUI
    {
        private IIconUIController iconUIController;
        private GameObject iconGameObject;
        private RectTransform rectTransformCanvasOverlay;

        public IconUI(GameObject iconGameObject, RectTransform rectTransformCanvasOverlay)
        {
            this.iconGameObject = iconGameObject;
            this.rectTransformCanvasOverlay = rectTransformCanvasOverlay;

            iconUIController = this.iconGameObject.GetComponentInChildren<IconHereController>();
            iconGameObject.SetActive(false);
        }

        public void SetPositionUI(Vector3 worldPosition)
        {
            Vector3 screenPos = WorldSpaceToCanvas(rectTransformCanvasOverlay, StartOfRound.Instance.localPlayerController.gameplayCamera, worldPosition);
            iconUIController.PlaceOnCanvas(screenPos, rectTransformCanvasOverlay);
            iconGameObject.SetActive(true);
        }

        public void SetPositionUICenter()
        {
            iconUIController.PlaceOnCanvas(new Vector3(0f, 0f, 10f), rectTransformCanvasOverlay);
            iconGameObject.SetActive(true);
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

        public void SetDefaultColor()
        {
            // r255 g111 b1 #ff6f01
            iconUIController.SetColor(new Color(255 / 255f, 111 / 255f, 1 / 255f));
        }

        public void SetIconActive(bool active)
        {
            iconGameObject.SetActive(active);
        }

        public static Vector3 WorldSpaceToCanvas(RectTransform canvasRect, Camera camera, Vector3 worldPos)
        {
            // https://discussions.unity.com/t/how-to-convert-from-world-space-to-canvas-space/117981/16
            Vector3 viewportPosition = camera.WorldToViewportPoint(worldPos);
            Vector3 canvasPos = new Vector3((viewportPosition.x * canvasRect.sizeDelta.x) - (canvasRect.sizeDelta.x * 0.5f),
                                            (viewportPosition.y * canvasRect.sizeDelta.y) - (canvasRect.sizeDelta.y * 0.5f),
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
