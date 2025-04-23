using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.UI
{
    public class IconUIDispenser
    {
        private Canvas canvasOverlay;
        private RectTransform rectTransformCanvasOverlay;

        // Here simple icon
        private List<IconUI> hereSimpleIcons;
        private int indexHereSimpleIcons;

        // Here multiple icon
        private List<IconUI> hereMultipleIcons;
        private int indexHereMultipleIcons;

        public IconUIDispenser(Canvas canvasOverlay)
        {
            this.canvasOverlay = canvasOverlay;
            this.rectTransformCanvasOverlay = canvasOverlay.GetComponentInChildren<RectTransform>();

            hereSimpleIcons = new List<IconUI>();
            indexHereSimpleIcons = 0;

            hereMultipleIcons = new List<IconUI>();
            indexHereMultipleIcons = 0;
        }

        public void ClearDispenser()
        {
            for (int i = indexHereSimpleIcons; i < hereSimpleIcons.Count; i++)
            {
                //Plugin.LogDebug($"i {i}, hereSimpleIcons.Count {hereSimpleIcons.Count}");
                hereSimpleIcons[i].SetIconActive(false);
            }
            indexHereSimpleIcons = 0;

            for (int i = indexHereMultipleIcons; i < hereMultipleIcons.Count; i++)
            {
                //Plugin.LogDebug($"i {i}, hereMultipleIcons.Count {hereMultipleIcons.Count}");
                hereMultipleIcons[i].SetIconActive(false);
            }
            indexHereMultipleIcons = 0;
        }

        public IconUI GetHereSimpleIcon()
        {
            if (indexHereSimpleIcons >= hereSimpleIcons.Count)
            {
                hereSimpleIcons.Add(GetNewHereSimpleIconUI());
            }

            return hereSimpleIcons[indexHereSimpleIcons++];
        }

        private IconUI GetNewHereSimpleIconUI()
        {
            return new IconUI(GameObject.Instantiate(Plugin.HereSimpleIconUIPrefab, this.canvasOverlay.transform), rectTransformCanvasOverlay);
        }

        public IconUI GetHereMultipleIcon()
        {
            if (indexHereMultipleIcons >= hereMultipleIcons.Count)
            {
                hereMultipleIcons.Add(GetNewHereMultipleIconUI());
            }

            return hereMultipleIcons[indexHereMultipleIcons++];
        }

        private IconUI GetNewHereMultipleIconUI()
        {
            return new IconUI(GameObject.Instantiate(Plugin.HereMultipleIconUIPrefab, this.canvasOverlay.transform), rectTransformCanvasOverlay);
        }
    }
}
