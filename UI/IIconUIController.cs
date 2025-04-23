using UnityEngine;

namespace LethalInternship.UI
{
    public interface IIconUIController
    {
        public void PlaceOnCanvas(Vector3 screenPos, RectTransform rectTransformCanvasParent);

        public void SetColor(Color color);
    }
}
