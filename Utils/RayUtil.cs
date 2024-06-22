using UnityEngine;

namespace LethalInternship.Utils
{
    internal class RayUtil
    {
        public static bool RayCastAndDraw(LineRenderer lr, Vector3 origin, Vector3 directionOrigin, float angle, float length)
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

        public static bool RayCastForwardAndDraw(LineRenderer lr, Vector3 origin, Vector3 directionForward, float length)
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

        public static bool RayCastDownAndDraw(LineRenderer lr, Vector3 origin, Vector3 directionDown, float length)
        {
            Ray ray = new Ray(origin, directionDown);
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
