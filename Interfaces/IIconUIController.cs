using UnityEngine;

namespace LethalInternship.Interfaces
{
    public interface IIconUIController
    {
        public bool IsIconInCenter { get; }

        public void PlaceOnCanvas(Vector3 screenPos, RectTransform rectTransformCanvasParent);

        public void SetColor(Color color);
    }
}
