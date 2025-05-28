using LethalInternship.Core.UI.Icons.InputIcons;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using LethalInternship.SharedAbstractions.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship.Core.UI.Icons.Pools
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
            return new InputIconUI(Object.Instantiate(PluginRuntimeProvider.Context.InputIconPrefab, canvasOverlay.transform), iconInfos, rectTransformCanvasOverlay);
        }
    }
}
