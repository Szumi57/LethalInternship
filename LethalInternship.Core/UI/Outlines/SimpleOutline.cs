using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.UI.Outlines
{
    public static class SimpleOutline
    {
        private class OutlineInstance
        {
            public GameObject go = null!;
            public SkinnedMeshRenderer renderer = null!;
            public MaterialPropertyBlock mpb = null!;
            public Color color;
        }

        private static readonly Dictionary<SkinnedMeshRenderer, OutlineInstance> active =
            new Dictionary<SkinnedMeshRenderer, OutlineInstance>();

        // -------- API --------

        public static void Add(GameObject target,
                               Color color,
                               float rimPower = 2.5f,
                               float intensity = 2.5f)
        {
            if (!target) return;
            if (!OutlineResources.SilhouetteMaterial) return;

            foreach (var smr in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!smr.enabled || !smr.sharedMesh) continue;
                if (active.ContainsKey(smr)) continue;

                GameObject go = new GameObject(smr.name + "_Outline");
                go.transform.SetParent(smr.transform, false);

                var outlineSmr = go.AddComponent<SkinnedMeshRenderer>();
                outlineSmr.sharedMesh = smr.sharedMesh;
                outlineSmr.bones = smr.bones;
                outlineSmr.rootBone = smr.rootBone;
                outlineSmr.updateWhenOffscreen = true;
                outlineSmr.sharedMaterial = OutlineResources.SilhouetteMaterial;

                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_Color", color);

                mpb.SetFloat("_RimPower", rimPower);
                mpb.SetFloat("_Intensity", intensity);

                outlineSmr.SetPropertyBlock(mpb);

                active[smr] = new OutlineInstance
                {
                    go = go,
                    renderer = outlineSmr,
                    mpb = mpb,
                    color = color,
                };
            }
        }

        public static void Remove(GameObject target)
        {
            if (!target) return;

            foreach (var smr in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!active.TryGetValue(smr, out var inst)) continue;

                Object.Destroy(inst.go);
                active.Remove(smr);
            }
        }

        public static void UpdateParams(GameObject target, float intensity, float rimPower, Color color)
        {
            foreach (var smr in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!active.TryGetValue(smr, out var inst)) continue;

                inst.mpb.SetFloat("_Intensity", intensity);
                inst.mpb.SetFloat("_RimPower", rimPower);
                inst.mpb.SetColor("_Color", color);
                inst.renderer.SetPropertyBlock(inst.mpb);
            }
        }
    }
}