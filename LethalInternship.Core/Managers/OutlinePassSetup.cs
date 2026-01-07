using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LethalInternship.Core.Managers
{
    public class OutlinePassSetup : MonoBehaviour
    {
        void Awake()
        {
            var go = new GameObject("OutlinePassVolume");
            Object.DontDestroyOnLoad(go);

            var volume = go.AddComponent<CustomPassVolume>();
            volume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
            volume.isGlobal = true;
            volume.customPasses.Add(new OutlineCustomPass());
        }
    }

    public class OutlineCustomPass : CustomPass
    {
        static HashSet<Renderer> outlined = new HashSet<Renderer>();

        Material maskMat;
        Material dilateMat;

        protected override void Setup(ScriptableRenderContext ctx, CommandBuffer cmd)
        {
            maskMat = new Material(Shader.Find("Hidden/OutlineMask"));
            dilateMat = new Material(Shader.Find("Hidden/OutlineDilate"));
        }

        public static void Add(Renderer r)
        {
            if (r) outlined.Add(r);
        }

        public static void Remove(Renderer r)
        {
            if (r) outlined.Remove(r);
        }

        protected override void Execute(CustomPassContext ctx)
        {
            var cmd = ctx.cmd;
            var maskBuffer = ctx.customColorBuffer.Value;

            CoreUtils.SetRenderTarget(cmd, maskBuffer.nameID, ClearFlag.Color, Color.black);

            foreach (var r in outlined)
            {
                if (!r) continue;
                cmd.DrawRenderer(r, maskMat);
            }

            cmd.SetGlobalTexture("_MaskTex", maskBuffer.nameID);
            HDUtils.DrawFullScreen(cmd, dilateMat, ctx.cameraColorBuffer);
        }

        protected override void Cleanup()
        {
            CoreUtils.Destroy(maskMat);
            CoreUtils.Destroy(dilateMat);
        }
    }

    public static class OutlineController
    {
        public static void Enable(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                OutlineCustomPass.Add(r);
        }

        public static void Disable(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                OutlineCustomPass.Remove(r);
        }
    }
}
