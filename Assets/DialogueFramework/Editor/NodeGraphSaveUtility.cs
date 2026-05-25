using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DialogueFramework.Editor
{
    public static class NodeGraphSaveUtility
    {
        public static void Save(DialoguesView graphView, GraphData graphData)
        {
            if (graphView == null || graphData == null)
            {
                Debug.LogError("GraphView or GraphData is not assigned.");
                return;
            }

            Undo.RecordObject(graphData, "Save Node Graph");

            graphData.nodes.Clear();
            graphData.links.Clear();

            var graphNodes = graphView.nodes
                .ToList()
                .OfType<DialogueEditorNode>()
                .ToList();

            foreach (var node in graphNodes)
            {
                node.Data.position = node.GetPosition().position;

                if (string.IsNullOrEmpty(node.Data.guid))
                    node.Data.guid = GUID.Generate().ToString();

                graphData.nodes.Add(node.Data);
            }

            var graphEdges = graphView.edges.ToList();

            foreach (var edge in graphEdges)
            {
                if (edge.output?.node is not DialogueEditorNode outputNode)
                    continue;

                if (edge.input?.node is not DialogueEditorNode inputNode)
                    continue;

                graphData.links.Add(new NodeLinkData
                {
                    outputNodeGuid = outputNode.Data.guid,
                    inputNodeGuid = inputNode.Data.guid
                });
            }

            EditorUtility.SetDirty(graphData);
            AssetDatabase.SaveAssets();
        }

        public static void Load(DialoguesView graphView, GraphData graphData)
        {
            if (graphView == null || graphData == null)
            {
                Debug.LogError("GraphView o GraphData es null.");
                return;
            }

            graphView.ClearGraph();

            var createdNodes = new Dictionary<string, DialogueEditorNode>();

            foreach (var nodeData in graphData.nodes)
            {
                var node = graphView.CreateNodeFromData(nodeData);

                if (node == null || node.Data == null || string.IsNullOrEmpty(node.Data.guid))
                {
                    Debug.LogWarning("Nodo cargado sin GUID.");
                    continue;
                }

                createdNodes[node.Data.guid] = node;

                node.RefreshPorts();
                node.RefreshExpandedState();
            }

            foreach (var linkData in graphData.links)
            {
                if (!createdNodes.TryGetValue(linkData.outputNodeGuid, out var outputNode))
                {
                    Debug.LogWarning($"Output node not found: {linkData.outputNodeGuid}");
                    continue;
                }

                if (!createdNodes.TryGetValue(linkData.inputNodeGuid, out var inputNode))
                {
                    Debug.LogWarning($"Input node not found: {linkData.inputNodeGuid}");
                    continue;
                }

                if (outputNode.OutputPort == null || inputNode.InputPort == null)
                {
                    Debug.LogWarning("OutputPort o InputPort es null.");
                    continue;
                }

                var edge = outputNode.OutputPort.ConnectTo(inputNode.InputPort);

                outputNode.OutputPort.Connect(edge);
                inputNode.InputPort.Connect(edge);

                graphView.AddElement(edge);
            }

            foreach (var node in createdNodes.Values)
            {
                node.RefreshPorts();
                node.RefreshExpandedState();
            }
        }
    }
}