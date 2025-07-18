using LethalInternship.Core.UI.Icons.WorldIcons;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using LethalInternship.SharedAbstractions.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship.Core.UI.Icons.Pools
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
            return new WorldIconUI(Object.Instantiate(PluginRuntimeProvider.Context.WorldIconPrefab, canvasOverlay.transform), iconInfos, rectTransformCanvasOverlay);
        }
    }
}
