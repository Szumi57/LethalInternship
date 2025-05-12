using LethalInternship.Interfaces;
using LethalInternship.UI.Icons.WorldIcons;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship.UI.Icons.Pools
{
    public class WorldIconUIPool : IconUIPoolBase<WorldIconUI>
    {
        private Canvas canvasOverlay;
        private RectTransform rectTransformCanvasOverlay;

        public WorldIconUIPool(Canvas canvasOverlay)
            : base()
        {
            this.canvasOverlay = canvasOverlay;
            rectTransformCanvasOverlay = canvasOverlay.GetComponentInChildren<RectTransform>();
        }

        protected override WorldIconUI NewIcon(IIconUIInfos iconInfos)
        {
            return new WorldIconUI(Object.Instantiate(Plugin.DefaultIconImagePrefab, canvasOverlay.transform), iconInfos, rectTransformCanvasOverlay);
        }
    }
}
