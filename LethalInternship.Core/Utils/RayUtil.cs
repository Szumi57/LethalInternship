using UnityEngine;

namespace LethalInternship.Core.Utils
{
    /// <summary>
    /// Utilitary class to help with using rays
    /// </summary>
    public class RayUtil
    {
        public static void RayCastAndDrawFromPointWithColor(LineRenderer? lr, Vector3 origin, Vector3 endPoint, Color color)
        {
            Ray ray = new Ray(origin, (endPoint - origin));
            float length = Vector3.Distance(endPoint, origin);
            DrawUtil.DrawLine(lr, ray, length, color);
        }

        public static bool RayCastAndDraw(LineRenderer? lr, Vector3 origin, Vector3 directionOrigin, float angle, float length)
        {
            Vector3 axis = Vector3.Cross(directionOrigin, Vector3.up);
            if (axis == Vector3.zero) axis = Vector3.right;
            Ray ray = new Ray(origin, Quaternion.AngleAxis(angle, axis) * directionOrigin);
            if (Physics.Raycast(ray, length, StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore))
            {
                DrawUtil.DrawLine(lr, ray, length, Color.red);
                return true;
            }
            else
            {
                DrawUtil.DrawWhiteLine(lr, ray, length);
                return false;
            }
        }

        public static bool RayCastForwardAndDraw(LineRenderer? lr, Vector3 origin, Vector3 directionForward, float length)
        {
            Ray ray = new Ray(origin, directionForward);
            if (Physics.Raycast(ray, length, StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore))
            {
                DrawUtil.DrawLine(lr, ray, length, Color.red);
                return true;
            }
            else
            {
                DrawUtil.DrawWhiteLine(lr, ray, length);
                return false;
            }
        }

        public static bool RayCastDownAndDraw(LineRenderer? lr, Vector3 origin, float length)
        {
            Ray ray = new Ray(origin, Vector3.down);
            if (Physics.Raycast(ray, length, StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore))
            {
                DrawUtil.DrawLine(lr, ray, length, Color.red);
                return true;
            }
            else
            {
                DrawUtil.DrawWhiteLine(lr, ray, length);
                return false;
            }
        }
    }
}
