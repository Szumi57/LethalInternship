using UnityEngine;

namespace LethalInternship.Utils
{
    internal static class DrawUtil
    {
        public static void DrawWhiteLine(LineRenderer lr, Ray ray, float length)
        {
            DrawLine(lr, ray, length, UnityEngine.Color.white);
        }

        public static void DrawLine(LineRenderer lr, Ray ray, float length, Color color)
        {
            DrawLine(lr, ray.origin, ray.origin + ray.direction.normalized * length, color);
        }

        public static void DrawLine(LineRenderer lr, Vector3 start, Vector3 end, Color color)
        {
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
