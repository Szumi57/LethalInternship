using LethalInternship.Interfaces;
using LethalInternship.UI.Icons.InputIcons;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship.UI.Icons.Pools
{
    public class InputIconUIPool : IconUIPoolBase<InputIconUI>
    {
        private Canvas canvasOverlay;
        private RectTransform rectTransformCanvasOverlay;

        public InputIconUIPool(Canvas canvasOverlay)
            : base()
        {
            this.canvasOverlay = canvasOverlay;
            rectTransformCanvasOverlay = canvasOverlay.GetComponentInChildren<RectTransform>();
        }

        protected override InputIconUI NewIcon(IIconUIInfos iconInfos)
        {
            return new InputIconUI(Object.Instantiate(Plugin.DefaultIconImagePrefab, canvasOverlay.transform), iconInfos, rectTransformCanvasOverlay);
        }
    }
}
