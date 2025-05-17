using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LethalInternship.Core.Utils
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static class BTUtil
    {
        
        public static void PrintTree(IPrintableNode tree)
        {
            PrintNode(tree);
        }

        private static void PrintNode(IPrintableNode node, int depth = 0)
        {
            string log = string.Empty;
            for (int i = 1; i <= depth - 1; i++)
            {
                log += "│   ";
            }

            if (depth > 0)
                log += "├─ ";

            PluginLoggerHook.LogDebug?.Invoke($"{log}{node.NodeTypeSign} {node.NodeType} {node.Name}");
            foreach (var childNode in node.Children)
            {
                PrintNode(childNode, depth + 1);
            }
        }

        /// <summary>
        /// Export json for website : https://opensource.adobe.com/behavior_tree_editor
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static string Export1TreeJson(IPrintableNode tree)
        {
            return JsonConvert.SerializeObject(Export1Tree(tree));
        }

        /// <summary>
        /// Export object for website : https://opensource.adobe.com/behavior_tree_editor
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static Export1Tree Export1Tree(IPrintableNode tree)
        {
            Guid rootGuid = Guid.NewGuid();

            Export1Tree export1Tree = new Export1Tree()
            {
                version = "0.3.0",
                scope = "tree",
                id = Guid.NewGuid(),
                title = "My tree",
                description = "Description",
                root = rootGuid,
                properties = new Dictionary<string, string>(),
                nodes = new Dictionary<Guid, Export1Node>(),
                display = new Dictionary<string, float>
                {
                    ["camera_x"] = 810,
                    ["camera_y"] = 390.5f,
                    ["camera_z"] = 0.75f,
                    ["x"] = -588,
                    ["y"] = -144,
                },
                custom_nodes = new List<Export1CustomNode>()
            };

            Export1Node(tree, rootGuid, export1Tree.nodes, export1Tree.custom_nodes);

            return export1Tree;
        }

        private static void Export1Node(IPrintableNode node,
                                        Guid guid,
                                        Dictionary<Guid, Export1Node> export1Nodes,
                                        List<Export1CustomNode> export1CustomNodes)
        {
            string nodeType = node.NodeType;
            string name = node.Name;

            Export1Node export1Node = new Export1Node()
            {
                id = guid,
                name = nodeType == "action" ? name : nodeType,
                title = name,
                description = string.Empty,
                properties = new Dictionary<string, string>(),
                display = new Dictionary<string, int> { ["x"] = 0, ["y"] = 0 },
            };

            // Custom node
            if (nodeType == "action")
            {
                export1CustomNodes.Add(new Export1CustomNode()
                {
                    version = "0.3.0",
                    scope = "node",
                    name = name,
                    category = "action",
                    title = null!,
                    description = null!,
                    properties = new Dictionary<string, string>()
                });
            }

            // Add node to tree
            export1Nodes.Add(guid, export1Node);

            // Traverse tree
            foreach (var childNode in node.Children)
            {
                if (export1Node.children == null)
                {
                    export1Node.children = new List<Guid>();
                }

                Guid guidChildNode = Guid.NewGuid();
                export1Node.children.Add(guidChildNode);
                Export1Node(childNode, guidChildNode, export1Nodes, export1CustomNodes);
            }
        }
    }

    public class Export1Tree
    {
        public string version { get; set; }
        public string scope { get; set; }
        public Guid id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public Guid root { get; set; }
        public Dictionary<string, string> properties { get; set; }
        public Dictionary<Guid, Export1Node> nodes { get; set; }
        public Dictionary<string, float> display { get; set; }
        public List<Export1CustomNode> custom_nodes { get; set; }
    }

    public class Export1Node
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public Dictionary<string, string> properties { get; set; }
        public Dictionary<string, int> display { get; set; }
        public List<Guid> children { get; set; }
    }

    public class Export1CustomNode
    {
        public string version { get; set; }
        public string scope { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public Dictionary<string, string> properties { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
