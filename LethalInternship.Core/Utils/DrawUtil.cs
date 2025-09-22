using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Utils
{
    /// <summary>
    /// Utilitary class for help drawing using <c>LineRenderer</c>
    /// </summary>
    public static class DrawUtil
    {
        public static void DrawWhiteLine(LineRenderer? lr, Ray ray, float length)
        {
            DrawLine(lr, ray, length, UnityEngine.Color.white);
        }

        public static void DrawLine(LineRenderer? lr, Ray ray, float length, Color color)
        {
            DrawLine(lr, ray.origin, ray.origin + ray.direction.normalized * length, color);
        }

        public static void DrawLine(LineRenderer? lr, Vector3 start, Vector3 end, Color color)
        {
            if (lr == null)
            {
                return;
            }

            lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }

        public static void DrawPath(LineRendererUtil lineRendererUtil, NavMeshPath path)
        {
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    DrawUtil.DrawLine(lineRendererUtil.GetLineRenderer(), path.corners[i], path.corners[i + 1], Color.red);
                    DrawUtil.DrawLine(lineRendererUtil.GetLineRenderer(), path.corners[i], path.corners[i] + new Vector3(0, 1, 0), Color.red);
                }
            }
            else if (path.status == NavMeshPathStatus.PathComplete)
            {
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    DrawUtil.DrawLine(lineRendererUtil.GetLineRenderer(), path.corners[i], path.corners[i + 1], Color.white);
                    DrawUtil.DrawLine(lineRendererUtil.GetLineRenderer(), path.corners[i], path.corners[i] + new Vector3(0, 1, 0), Color.white);
                }
            }
            else
            {
                PluginLoggerHook.LogDebug?.Invoke($"DrawPath PathInvalid");
            }
        }
    }
}
