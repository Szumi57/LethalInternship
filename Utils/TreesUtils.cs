﻿using BepInEx;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LethalInternship.Utils
{
    internal static class TreesUtils
    {
        public static void PrintTransformTree(Transform[] tree, string? parent = null)
        {
            if (tree == null)
            {
                Plugin.Logger.LogDebug($"Tree of transform is null");
                return;
            }
            if (tree.Length == 0)
            {
                Plugin.Logger.LogDebug($"Tree of transform is empty");
                return;
            }

            if (parent.IsNullOrWhiteSpace())
            {
                parent = tree.FirstOrDefault()?.parent?.root?.name;
            }
            TransformTreeTraversal(tree, parent, 0);
        }

        private static void TransformTreeTraversal(Transform[] tree, string? parent, int depth)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Transform actual in tree)
            {
                if ((string.IsNullOrWhiteSpace(parent) && (actual.parent == null || string.IsNullOrWhiteSpace(actual.parent.name)))
                    || (actual.parent != null && actual.parent.name == parent))
                {
                    for (int i = 1; i <= depth - 1; i++)
                    {
                        sb.Append("│   ");
                    }

                    if (depth > 0)
                        sb.Append("├──");

                    Plugin.Logger.LogDebug($"{sb.ToString()}{actual.name}");
                    sb.Clear();
                    TransformTreeTraversal(tree, actual.name, depth + 1);
                }
            }
        }


        public static void PrintComponentsTreeOfGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Plugin.Logger.LogDebug($"GameObject is null");
                return;
            }

            Component[] components = gameObject.GetComponentsInChildren(typeof(Component)).Where(x => x != null).ToArray();
            if (components.Length == 0)
            {
                Plugin.Logger.LogDebug($"GameObject has no components");
                return;
            }

            ComponentTreeTraversal(components);
        }

        private static int ComponentTreeTraversal(Component[] tree, string? parent = null, int index = 0, int depth = 0, string lastTransformName = "")
        {
            StringBuilder sb = new StringBuilder();

            for (int i = index; i < tree.Length; i++)
            {
                Component actual = tree[i];
                Transform transform = actual as Transform;
                bool isTransform = transform != null;
                if (transform == null)
                {
                    transform = actual.transform;
                }

                if (string.IsNullOrWhiteSpace(parent) && string.IsNullOrWhiteSpace(transform.parent?.name))
                {
                    Plugin.Logger.LogDebug($"{(isTransform ? transform.name : actual.ToString())}");
                }

                if (string.IsNullOrWhiteSpace(parent) && !string.IsNullOrWhiteSpace(transform.parent?.name))
                {
                    parent = transform.parent.name;
                }

                if (parent == transform.name)
                {
                    for (int j = 1; j <= depth - 1; j++)
                    {
                        sb.Append("│   ");
                    }

                    if (depth > 0)
                        sb.Append("├──");

                    Plugin.Logger.LogDebug($"{sb}{(isTransform ? transform.name : actual.ToString())}");
                    sb.Clear();
                    continue;
                }

                if (transform.parent?.name == parent && isTransform)
                {
                    for (int j = 1; j <= depth - 1; j++)
                    {
                        sb.Append("│   ");
                    }

                    if (depth > 0)
                        sb.Append("├──");

                    if (transform.name == lastTransformName)
                    {
                        continue;
                    }

                    lastTransformName = transform.name;
                    Plugin.Logger.LogDebug($"{sb}{transform.name}");
                    sb.Clear();
                    i = ComponentTreeTraversal(tree, transform.name, i + 1, depth + 1, lastTransformName);
                    index = i;
                }
            }

            return index;
        }
    }
}
