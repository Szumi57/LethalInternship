using UnityEngine;

namespace LethalInternship.Core.UI.Outlines
{
    public static class OutlineResources
    {
        public static Material SilhouetteMaterial = null!;
        public static Material RimOutlineMaterial = null!;

        public static void Init(Material silhouetteMaterial)
        {
            SilhouetteMaterial = silhouetteMaterial;
            RimOutlineMaterial = null!;
        }

        public static void Init(Material silhouetteMaterial, Material rimOutlineMaterial)
        {
            SilhouetteMaterial = silhouetteMaterial;
            RimOutlineMaterial = rimOutlineMaterial;
        }
    }
}
