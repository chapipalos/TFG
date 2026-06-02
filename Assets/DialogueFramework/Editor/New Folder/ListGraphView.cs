using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueFramework.Editor
{
    /// <summary>
    /// GraphView base for flat, scrollable, full-width node lists.
    /// Subclasses only need to implement four things:
    ///   - NodeHeight property
    ///   - CreateNodeFromData(TData) → TNode
    ///   - GetDataList(GraphData)    → the list inside GraphData to read/write
    ///   - BuildNewData()            → factory for a brand-new TData item
    /// Everything else (scroll, reorder, geometry, callbacks) lives here.
    /// </summary>
    public abstract class ListGraphView<TNode, TData> : GraphView
        where TNode : Node, IListNode<TData>
    {
        // ── Public ────────────────────────────────────────────────────────────

        public EditorWindow Window { get; }

        // ── Protected – subclass configures these ─────────────────────────────

        /// <summary>Height of each node in pixels. Override in subclass.</summary>
        protected abstract float m_NodeHeight { get; }

        // ── Private state ─────────────────────────────────────────────────────

        private readonly List<TNode> m_Nodes = new();
        private GraphData currentGraph;

        private float scrollOffsetY;
        private const float Spacing    = 10f;
        private const float ScrollSpeed = 30f;
        private const float FallbackWidth = 300f;

        // ── Constructor ───────────────────────────────────────────────────────

        protected ListGraphView(EditorWindow window, GraphData graph)
        {
            Window       = window;
            currentGraph = graph;

            Insert(0, new GridBackground());
            this.AddManipulator(new RectangleSelector());

            serializeGraphElements = _ => string.Empty;
            unserializeAndPaste    = (_, _) => { };
            graphViewChanged       = change => change;

            if (currentGraph != null)
            {
                foreach (var data in GetDataList(currentGraph))
                    AddNodeInternal(CreateNodeFromData(data));
            }

            RegisterCallback<GeometryChangedEvent>(_ => Reorder());
            RegisterCallback<WheelEvent>(OnWheelEvent);
        }

        // ── Abstract API ──────────────────────────────────────────────────────

        /// <summary>Return the list inside <paramref name="graph"/> that this view manages.</summary>
        protected abstract List<TData> GetDataList(GraphData graph);

        /// <summary>Create and return a new TData with a fresh GUID and default values.</summary>
        protected abstract TData BuildNewData();

        /// <summary>Instantiate a TNode from existing data (no side-effects on GraphData).</summary>
        protected abstract TNode CreateNodeFromData(TData data);

        // ── Public CRUD ───────────────────────────────────────────────────────

        /// <summary>Create a brand-new item, persist it, and add its node to the view.</summary>
        public TNode CreateItem()
        {
            if (currentGraph == null)
            {
                Debug.LogError($"[{GetType().Name}] No GraphData assigned.");
                return null;
            }

            var data = BuildNewData();
            GetDataList(currentGraph).Add(data);
            EditorUtility.SetDirty(currentGraph);

            var node = CreateNodeFromData(data);
            AddNodeInternal(node);
            return node;
        }

        /// <summary>Remove a node from the view and its data from GraphData.</summary>
        public void RemoveItem(TNode node)
        {
            if (node == null) return;

            RemoveElement(node);
            m_Nodes.Remove(node);

            if (currentGraph != null)
            {
                GetDataList(currentGraph).Remove(node.m_Data);
                EditorUtility.SetDirty(currentGraph);
            }

            Reorder();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void AddNodeInternal(TNode node)
        {
            AddElement(node);
            m_Nodes.Add(node);
            Reorder();
        }

        private void OnWheelEvent(WheelEvent evt)
        {
            if (m_Nodes.Count == 0) return;

            float total   = m_Nodes.Count * (m_NodeHeight + Spacing);
            float visible = layout.height;
            float maxScroll = Mathf.Max(0, total - visible);

            scrollOffsetY += evt.delta.y * ScrollSpeed;
            scrollOffsetY  = Mathf.Clamp(scrollOffsetY, 0, maxScroll);

            Reorder();
            evt.StopPropagation();
        }

        private float GetCurrentWidth()
        {
            float w = contentRect.width > 0 ? contentRect.width : layout.width;
            return w > 0 ? w : FallbackWidth;
        }

        private void Reorder()
        {
            float width = GetCurrentWidth();

            for (int i = 0; i < m_Nodes.Count; i++)
            {
                var node = m_Nodes[i];
                node.SetNodeSize(width, m_NodeHeight);
                float y = i * (m_NodeHeight + Spacing) - scrollOffsetY;
                node.SetPosition(new Rect(0, y, width, m_NodeHeight));
            }
        }
    }

    /// <summary>
    /// Required contract for nodes used inside ListGraphView.
    /// Ensures the view can read the data back and call SetNodeSize.
    /// </summary>
    public interface IListNode<out TData>
    {
        TData m_Data { get; }
        void SetNodeSize(float width, float height);
    }
}
