using UnityEngine;

namespace LethalInternship.Utils
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
    }
}
