using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LethalInternship.Core.Managers
{
    public class OutlineSystem : MonoBehaviour
    {
        public static OutlineSystem Instance;

        HashSet<Renderer> renderers = new HashSet<Renderer>();

        public Material maskMaterial;
        public Material fullscreenMaterial;

        public OutlineMaskPass MaskPass { get; private set; }

        void Awake()
        {
            Instance = this;

            var volume = gameObject.AddComponent<CustomPassVolume>();
            volume.isGlobal = true;
            volume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;

            MaskPass = new OutlineMaskPass(renderers);
            volume.customPasses.Add(MaskPass);
            volume.customPasses.Add(new OutlineFullscreenPass(() => fullscreenMaterial, () => MaskPass));
        }

        // -------- API --------

        public static void Enable(GameObject go)
        {
            if (Instance == null || go == null) return;

            //foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            //{
            //    Debug.Log($"{r.name} | type={r.GetType()} | enabled={r.enabled} | matCount={r.sharedMaterials.Length}");
            //}

            //foreach (var r in go.GetComponentsInChildren<Renderer>())
            //{
            //    PluginLoggerHook.LogDebug?.Invoke($"Adding renderer {r.name}");
            //    Instance.renderers.Add(r);
            //}

            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!r.enabled) continue;
                if (r.sharedMesh == null) continue;

                Debug.Log("Outline ADD SkinnedMesh: " + r.name);
                Instance.renderers.Add(r);
            }
        }

        public static void Disable(GameObject go)
        {
            if (Instance == null || go == null) return;

            foreach (var r in go.GetComponentsInChildren<Renderer>())
                Instance.renderers.Remove(r);
        }
    }

    public class OutlineMaskPass : CustomPass
    {
        HashSet<Renderer> renderers;
        RTHandle mask;

        public OutlineMaskPass(HashSet<Renderer> r) => renderers = r;

        protected override void Setup(ScriptableRenderContext ctx, CommandBuffer cmd)
        {
            mask = RTHandles.Alloc(
        Vector2.one,                     // scale
        name: "OutlineMask",
        colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm,
        useDynamicScale: true
    );
        }

        protected override void Execute(CustomPassContext ctx)
        {
            //PluginLoggerHook.LogDebug?.Invoke($"OutlineMaskPass executing, renderers: " + renderers.Count);

            if (renderers.Count == 0) return;

            var mat = OutlineSystem.Instance.maskMaterial;
            if (mat == null) return;

            CoreUtils.SetRenderTarget(ctx.cmd, mask, ClearFlag.Color, Color.black);

            foreach (var r in renderers)
            {
                if (r)
                {
                    Debug.Log("Drawing renderer: " + r.name);
                    ctx.cmd.DrawRenderer(r, mat);
                }
            }
        }

        protected override void Cleanup()
        {
            RTHandles.Release(mask);
        }

        public RTHandle Mask => mask;
    }

    class OutlineFullscreenPass : CustomPass
    {
        System.Func<Material> matGetter;
        System.Func<OutlineMaskPass> maskGetter;

        public OutlineFullscreenPass(System.Func<Material> m, System.Func<OutlineMaskPass> mask)
        {
            matGetter = m;
            maskGetter = mask;
        }

        //protected override void Execute(CustomPassContext ctx)
        //{
        //    var mat = matGetter();          // récupère ton fullscreenMaterial
        //    var maskPass = maskGetter();    // récupère le OutlineMaskPass
        //    if (mat == null || maskPass == null) return;

        //    //Debug.Log("Mask RT info: " + maskPass.Mask.rt.name + " size=" + maskPass.Mask.rt.width + "x" + maskPass.Mask.rt.height);
        //    // C’est ici qu’on assigne le mask
        //    mat.SetTexture("_MaskTex", maskPass.Mask);

        //    // Dessine fullscreen
        //    //PluginLoggerHook.LogDebug?.Invoke($"_OutlineColor={mat.GetColor("_OutlineColor")}, _Thickness= {mat.GetFloat("_Thickness")}");
        //    HDUtils.DrawFullScreen(ctx.cmd, mat, ctx.cameraColorBuffer);

        //    //Material debugMat = new Material(Shader.Find("Unlit/Color"));
        //    //debugMat.SetColor("_Color", Color.white);
        //    //HDUtils.DrawFullScreen(ctx.cmd, debugMat, ctx.cameraColorBuffer);
        //}

        protected override void Execute(CustomPassContext ctx)
        {
            if (ctx.hdCamera.camera.cameraType != CameraType.Game)
                return;

            var mat = matGetter();
            if (mat == null)
                return;

            // CIBLE EXPLICITE
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer);

            // DESSIN FULLSCREEN
            HDUtils.DrawFullScreen(ctx.cmd, mat, ctx.cameraColorBuffer);

            Debug.Log("Execute");
        }
    }
}
