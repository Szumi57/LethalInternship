using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.UI.Outlines
{
    public static class SimpleOutline
    {
        private class OutlineInstance
        {
            public GameObject go = null!;
            public Renderer renderer = null!;
            public MaterialPropertyBlock mpb = null!;
        }

        private static readonly Dictionary<Renderer, OutlineInstance> active = new Dictionary<Renderer, OutlineInstance>();

        // ---------- API ----------

        public static void Add(GameObject target,
                               Color color,
                               bool onlySkinned,
                               float rimPower = 2.5f,
                               float intensity = 2.5f)
        {
            if (!target) return;
            if (!OutlineResources.SilhouetteMaterial) return;

            foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                if (active.ContainsKey(r)) continue;

                if (r is SkinnedMeshRenderer smr)
                {
                    AddSkinned(smr, color, rimPower, intensity);
                }
                else if (!onlySkinned && r is MeshRenderer mr)
                {
                    AddStatic(mr, color, rimPower, intensity);
                }
            }
        }

        public static void Remove(GameObject target)
        {
            if (!target) return;

            foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            {
                if (!active.TryGetValue(r, out var inst)) continue;

                Object.Destroy(inst.go);
                active.Remove(r);
            }
        }

        public static void UpdateParams(GameObject target,
                                        float intensity,
                                        float rimPower,
                                        Color color)
        {
            foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            {
                if (!active.TryGetValue(r, out var inst)) continue;

                inst.mpb.SetFloat("_Intensity", intensity);
                inst.mpb.SetFloat("_RimPower", rimPower);
                inst.mpb.SetColor("_Color", color);
                inst.renderer.SetPropertyBlock(inst.mpb);
            }
        }

        // ---------- Internals ----------

        private static void AddSkinned(SkinnedMeshRenderer smr,
                                      Color color,
                                      float rimPower,
                                      float intensity)
        {
            if (!smr.sharedMesh) return;

            GameObject go = new GameObject(smr.name + "_Outline");
            go.transform.SetParent(smr.transform, false);

            var outline = go.AddComponent<SkinnedMeshRenderer>();
            outline.sharedMesh = smr.sharedMesh;
            outline.bones = smr.bones;
            outline.rootBone = smr.rootBone;
            outline.updateWhenOffscreen = true;
            outline.sharedMaterial = OutlineResources.SilhouetteMaterial;

            var mpb = CreateMPB(color, rimPower, intensity);
            outline.SetPropertyBlock(mpb);

            active[smr] = new OutlineInstance
            {
                go = go,
                renderer = outline,
                mpb = mpb
            };
        }

        private static void AddStatic(MeshRenderer mr,
                                      Color color,
                                      float rimPower,
                                      float intensity)
        {
            if (mr.name.Contains("ScanNode"))
                return;

            var mf = mr.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) return;

            GameObject go = new GameObject(mr.name + "_Outline");
            go.transform.SetParent(mr.transform, false);

            var outlineMf = go.AddComponent<MeshFilter>();
            outlineMf.sharedMesh = mf.sharedMesh;

            var outlineMr = go.AddComponent<MeshRenderer>();
            outlineMr.sharedMaterial = OutlineResources.SilhouetteMaterial;

            var mpb = CreateMPB(color, rimPower, intensity);
            outlineMr.SetPropertyBlock(mpb);

            active[mr] = new OutlineInstance
            {
                go = go,
                renderer = outlineMr,
                mpb = mpb
            };
        }

        private static MaterialPropertyBlock CreateMPB(Color color,
                                                       float rimPower,
                                                       float intensity)
        {
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", color);
            mpb.SetFloat("_RimPower", rimPower);
            mpb.SetFloat("_Intensity", intensity);
            return mpb;
        }
    }
}