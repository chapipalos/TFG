using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DialogueFramework.Editor
{
    public static class NodeGraphSaveUtility
    {
        // ── Save ──────────────────────────────────────────────────────────────

        public static void Save(DialoguesView graphView, GraphData graphData)
        {
            if (graphView == null || graphData == null)
            {
                Debug.LogError("[SaveUtility] GraphView or GraphData is null.");
                return;
            }

            Undo.RecordObject(graphData, "Save Node Graph");

            graphData.nodes.Clear();
            graphData.links.Clear();

            // ── Nodes ─────────────────────────────────────────────────────────

            var graphNodes = graphView.nodes
                .ToList()
                .OfType<DialogueEditorNode>()
                .ToList();

            var validGuids = new HashSet<string>();

            foreach (var node in graphNodes)
            {
                node.Data.position = node.GetPosition().position;

                if (string.IsNullOrEmpty(node.Data.guid))
                    node.Data.guid = GUID.Generate().ToString();

                graphData.nodes.Add(node.Data);
                validGuids.Add(node.Data.guid);
            }

            // ── Edges ─────────────────────────────────────────────────────────

            foreach (var edge in graphView.edges.ToList())
            {
                if (edge.output?.node is not DialogueEditorNode outputNode) continue;
                if (edge.input?.node is not DialogueEditorNode inputNode) continue;

                if (!validGuids.Contains(outputNode.Data.guid) ||
                    !validGuids.Contains(inputNode.Data.guid))
                {
                    Debug.LogWarning("[SaveUtility] Skipping edge with missing GUID.");
                    continue;
                }

                // Determine which reply port this edge comes from (if any)
                string outputPortGuid = string.Empty;
                foreach (var (replyGuid, port) in outputNode.GetAllReplyPorts())
                {
                    if (port == edge.output)
                    {
                        outputPortGuid = replyGuid;
                        break;
                    }
                }

                graphData.links.Add(new NodeLinkData
                {
                    outputNodeGuid = outputNode.Data.guid,
                    inputNodeGuid = inputNode.Data.guid,
                    outputPortGuid = outputPortGuid   // empty → generic OutputPort
                });
            }

            EditorUtility.SetDirty(graphData);
            AssetDatabase.SaveAssets();
        }

        // ── Load ──────────────────────────────────────────────────────────────

        public static void Load(DialoguesView graphView, GraphData graphData)
        {
            if (graphView == null || graphData == null)
            {
                Debug.LogError("[SaveUtility] GraphView or GraphData is null.");
                return;
            }

            graphView.ClearGraph();

            // ── Nodes ─────────────────────────────────────────────────────────

            var createdNodes = new Dictionary<string, DialogueEditorNode>();

            foreach (var nodeData in graphData.nodes)
            {
                var node = graphView.CreateNodeFromData(nodeData);

                if (node == null || node.Data == null || string.IsNullOrEmpty(node.Data.guid))
                {
                    Debug.LogWarning("[SaveUtility] Skipping node with missing GUID.");
                    continue;
                }

                createdNodes[node.Data.guid] = node;
            }

            // ── Edges ─────────────────────────────────────────────────────────

            foreach (var linkData in graphData.links)
            {
                if (!createdNodes.TryGetValue(linkData.outputNodeGuid, out var outputNode))
                {
                    Debug.LogWarning($"[SaveUtility] Output node not found: {linkData.outputNodeGuid}");
                    continue;
                }

                if (!createdNodes.TryGetValue(linkData.inputNodeGuid, out var inputNode))
                {
                    Debug.LogWarning($"[SaveUtility] Input node not found: {linkData.inputNodeGuid}");
                    continue;
                }

                if (inputNode.InputPort == null)
                {
                    Debug.LogWarning("[SaveUtility] InputPort is null.");
                    continue;
                }

                // Resolve the correct output port:
                // - if outputPortGuid is set → find that reply's port
                // - otherwise → use the generic OutputPort
                Port outputPort;

                if (!string.IsNullOrEmpty(linkData.outputPortGuid))
                {
                    outputPort = outputNode.GetReplyPort(linkData.outputPortGuid);
                    if (outputPort == null)
                    {
                        Debug.LogWarning($"[SaveUtility] Reply port not found: {linkData.outputPortGuid}");
                        continue;
                    }
                }
                else
                {
                    outputPort = outputNode.OutputPort;
                    if (outputPort == null)
                    {
                        Debug.LogWarning("[SaveUtility] Generic OutputPort is null.");
                        continue;
                    }
                }

                var edge = outputPort.ConnectTo(inputNode.InputPort);
                graphView.AddElement(edge);
            }

            // ── Refresh ───────────────────────────────────────────────────────

            foreach (var node in createdNodes.Values)
            {
                node.RefreshPorts();
                node.RefreshExpandedState();
            }
        }
    }
}