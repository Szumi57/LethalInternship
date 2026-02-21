using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections.Generic;
using UnityEngine;

public static class SimpleRimOutlineBold
{
    private class OutlineInstance
    {
        public GameObject go;
        public SkinnedMeshRenderer renderer;
        public MaterialPropertyBlock mpb;
        public float pulse;
        public float baseIntensity;
    }

    private static readonly Dictionary<SkinnedMeshRenderer, OutlineInstance> active =
        new Dictionary<SkinnedMeshRenderer, OutlineInstance>();

    // -------- API --------

    public static void Enable(
        GameObject target,
        Color color,
        float rimPower = 2.5f,
        float intensity = 2.5f,
        float fill = 0f,
        float pulse = 0f)
    {
        if (!target) return;

        if (!OutlineResources.RimOutlineMaterial)
        {
            PluginLoggerHook.LogError?.Invoke("RimOutlineMaterial is null");
            return;
        }

        foreach (var smr in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!smr.enabled || !smr.sharedMesh) continue;
            if (active.ContainsKey(smr)) continue;

            // Duplication (facultative mais recommandé)
            GameObject go = new GameObject(smr.name + "_RimOutline");
            go.transform.SetParent(smr.transform, false);

            var outlineSmr = go.AddComponent<SkinnedMeshRenderer>();
            outlineSmr.sharedMesh = smr.sharedMesh;
            outlineSmr.bones = smr.bones;
            outlineSmr.rootBone = smr.rootBone;
            outlineSmr.updateWhenOffscreen = true;
            outlineSmr.sharedMaterial = OutlineResources.RimOutlineMaterial;

            // MPB
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_OutlineColor", color);
            mpb.SetFloat("_RimPower", rimPower);
            mpb.SetFloat("_Intensity", intensity);
            mpb.SetFloat("_Fill", fill);
            outlineSmr.SetPropertyBlock(mpb);

            active[smr] = new OutlineInstance
            {
                go = go,
                renderer = outlineSmr,
                mpb = mpb,
                pulse = pulse,
                baseIntensity = intensity
            };

            PluginLoggerHook.LogDebug?.Invoke($"RimOutline enabled on {smr.name}");
        }
    }

    public static void Disable(GameObject target)
    {
        if (!target) return;

        foreach (var smr in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!active.TryGetValue(smr, out var inst)) continue;

            Object.Destroy(inst.go);
            active.Remove(smr);
        }
    }

    /// <summary>
    /// À appeler depuis un Update global (1 fois par frame)
    /// </summary>
    public static void Update()
    {
        if (active.Count == 0) return;

        float t = Time.time;

        foreach (var inst in active.Values)
        {
            if (!inst.renderer) continue;

            if (inst.pulse > 0f)
            {
                float pulseValue = inst.baseIntensity *
                    (1f + Mathf.Sin(t * 5f) * inst.pulse);

                inst.mpb.SetFloat("_Intensity", pulseValue);
                inst.renderer.SetPropertyBlock(inst.mpb);
            }
        }
    }
}
