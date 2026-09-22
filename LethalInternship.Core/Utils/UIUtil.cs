using UnityEngine;

namespace LethalInternship.Core.Utils
{
    public static class UIUtil
    {
        public static string GetGameObjectPath(GameObject? obj)
        {
            if (obj == null)
                return "[null]";

            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }
    }
}
