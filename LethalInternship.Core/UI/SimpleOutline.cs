using System.Collections.Generic;
using UnityEngine;

public static class SimpleOutline
{
    private class OutlineInstance
    {
        public GameObject go;
        public SkinnedMeshRenderer renderer;
        public MaterialPropertyBlock mpb;
        public Color color;
        public float pulse;
    }

    private static readonly Dictionary<SkinnedMeshRenderer, OutlineInstance> active =
        new Dictionary<SkinnedMeshRenderer, OutlineInstance>();

    // -------- API --------

    public static void Add(
        GameObject target,
        Camera povCamera,
        Color color,
        float rimPower = 2.5f,
        float intensity = 2.5f,
        float pulse = 0f)
    {
        if (!target) return;
        if (!OutlineResources.SilhouetteMaterial) return;

        if (!povCamera) return;

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
                pulse = pulse
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

    public static void Update()
    {
        if (active.Count == 0) return;

        float t = Time.time;

        foreach (var kv in active)
        {
            var inst = kv.Value;
            if (!inst.renderer) continue;

            if (inst.pulse > 0f)
            {
                float p = 1f + Mathf.Sin(t * 5f) * inst.pulse;
                inst.go.transform.localScale = Vector3.one * p;
            }

            inst.mpb.SetColor("_Color", inst.color);
            inst.renderer.SetPropertyBlock(inst.mpb);
        }
    }
}

public static class OutlineResources
{
    public static Material SilhouetteMaterial;
    public static Material RimOutlineMaterial;

    public static void Init(Material silhouetteMaterial, Material rimOutlineMaterial)
    {
        SilhouetteMaterial = silhouetteMaterial;
        RimOutlineMaterial = rimOutlineMaterial;
    }
}
