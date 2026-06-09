using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueFramework.Editor
{
    public class EditorWindow : UnityEditor.EditorWindow
    {
        // ── Views ─────────────────────────────────────────────────────────────
        private DialoguesView graphView;
        private ActorsView actorsView;
        private QuestsView questsView;

        // ── Toolbar containers ────────────────────────────────────────────────
        private VisualElement graphToolbar;
        private VisualElement actorsToolbar;
        private VisualElement questsToolbar;

        // ── State ─────────────────────────────────────────────────────────────
        private ObjectField graphAssetField;
        private GraphData currentGraph;
        private VisualElement contentArea;

        // ── Conversation bar ──────────────────────────────────────────────────
        private PopupField<string> conversationDropdown;
        private VisualElement conversationBar;

        // ── Menu entry ────────────────────────────────────────────────────────

        [MenuItem("Window/Dialogue Framework")]
        public static void Open() => GetWindow<EditorWindow>("Dialogue Framework");

        // ── Unity lifecycle ───────────────────────────────────────────────────

        public void CreateGUI()
        {
            BuildTopBar();

            contentArea = new VisualElement();
            contentArea.style.flexGrow = 1;
            rootVisualElement.Add(contentArea);
        }

        // ── Top bar ───────────────────────────────────────────────────────────

        private void BuildTopBar()
        {
            var topBar = new VisualElement();
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.alignItems = Align.Center;
            topBar.style.paddingRight = 8;

            graphAssetField = new ObjectField("Graph Asset")
            {
                objectType = typeof(GraphData),
                allowSceneObjects = false
            };
            graphAssetField.RegisterValueChangedCallback(evt =>
            {
                currentGraph = evt.newValue as GraphData;
                RebuildViews();
            });

            var newButton = new Button(CreateNewGraphAsset) { text = "New" };
            var saveButton = new Button(SaveGraph) { text = "Save" };

            topBar.Add(graphAssetField);
            topBar.Add(newButton);
            topBar.Add(saveButton);

            topBar.Add(BuildTab("Nodes", () => ShowView(graphView, graphToolbar)));
            topBar.Add(BuildTab("Actors", () => ShowView(actorsView, actorsToolbar)));
            topBar.Add(BuildTab("Quests", () => ShowView(questsView, questsToolbar)));

            rootVisualElement.Add(topBar);
        }

        // ── Conversation bar ─────────────────────────────────────────────────

        private void BuildConversationBar()
        {
            conversationBar = new VisualElement();
            conversationBar.style.flexDirection = FlexDirection.Row;
            conversationBar.style.alignItems = Align.Center;
            conversationBar.style.paddingLeft = 4;
            conversationBar.style.paddingRight = 4;

            RefreshConversationDropdown();

            conversationBar.Add(new Button(CreateNewConversation) { text = "+ New Conversation" });
            conversationBar.Add(new Button(RenameCurrentConversation) { text = "Rename" });
            conversationBar.Add(new Button(DeleteCurrentConversation) { text = "Delete" });
        }

        private void RefreshConversationDropdown()
        {
            if (conversationBar == null) return;

            // Remove old dropdown if exists
            if (conversationDropdown != null && conversationBar.Contains(conversationDropdown))
                conversationBar.Remove(conversationDropdown);

            var names = currentGraph.s_Conversations.Select(c => c.s_CName).ToList();
            if (names.Count == 0) names.Add("(no conversations)");

            string current = names[0];
            if (graphView != null && !string.IsNullOrEmpty(graphView.CurrentConversationGuid))
            {
                var cur = currentGraph.s_Conversations.FirstOrDefault(c => c.s_CGuid == graphView.CurrentConversationGuid);
                if (cur != null) current = cur.s_CName;
            }

            conversationDropdown = new PopupField<string>("Conversation", names, current);
            conversationDropdown.RegisterValueChangedCallback(evt =>
            {
                var c = currentGraph.s_Conversations.FirstOrDefault(c => c.s_CName == evt.newValue);
                if (c != null && graphView != null)
                    graphView.SetCurrentConversation(c.s_CGuid);
            });

            conversationBar.Insert(0, conversationDropdown);
        }

        private void CreateNewConversation()
        {
            if (currentGraph == null) return;

            string name = "Conversation " + (currentGraph.s_Conversations.Count + 1);
            var conv = new ConversationData
            {
                s_CGuid = System.Guid.NewGuid().ToString(),
                s_CName = name
            };
            currentGraph.s_Conversations.Add(conv);
            EditorUtility.SetDirty(currentGraph);

            graphView.SetCurrentConversation(conv.s_CGuid);
            RefreshConversationDropdown();
        }

        private void RenameCurrentConversation()
        {
            if (currentGraph == null || graphView == null) return;

            var conv = currentGraph.s_Conversations.FirstOrDefault(c => c.s_CGuid == graphView.CurrentConversationGuid);
            if (conv == null) return;

            EditorInputDialog.Show(
                "Rename conversation",
                "New name:",
                conv.s_CName,
                newName =>
                {
                    if (string.IsNullOrEmpty(newName)) return;
                    conv.s_CName = newName;
                    EditorUtility.SetDirty(currentGraph);
                    RefreshConversationDropdown();
                });
        }

        private void DeleteCurrentConversation()
        {
            if (currentGraph == null || graphView == null) return;

            var conv = currentGraph.s_Conversations.FirstOrDefault(c => c.s_CGuid == graphView.CurrentConversationGuid);
            if (conv == null) return;

            bool confirm = EditorUtility.DisplayDialog(
                "Delete conversation",
                $"Delete '{conv.s_CName}' and all its nodes? This cannot be undone.",
                "Delete", "Cancel");
            if (!confirm) return;

            // Collect guids of nodes that belong to this conversation
            var nodeGuids = currentGraph.s_Nodes
                .Where(n => n.s_ConversationGuid == conv.s_CGuid)
                .Select(n => n.s_NGuid)
                .ToHashSet();

            currentGraph.s_Nodes.RemoveAll(n => n.s_ConversationGuid == conv.s_CGuid);
            currentGraph.s_Links.RemoveAll(l =>
                nodeGuids.Contains(l.s_OutputNodeGuid) || nodeGuids.Contains(l.s_InputNodeGuid));
            currentGraph.s_Conversations.Remove(conv);

            EditorUtility.SetDirty(currentGraph);

            string next = currentGraph.s_Conversations.Count > 0
                ? currentGraph.s_Conversations[0].s_CGuid
                : "";
            graphView.SetCurrentConversation(next);
            RefreshConversationDropdown();
        }

        private Tab BuildTab(string label, System.Action onSelected)
        {
            var tab = new Tab(label);
            tab.style.paddingLeft = 4;
            tab.style.paddingRight = 4;
            tab.selected += _ =>
            {
                if (currentGraph == null)
                {
                    EditorUtility.DisplayDialog(
                        "No graph assigned",
                        "Assign or create a Dialogue Graph Asset first.",
                        "OK");
                    return;
                }
                onSelected();
            };
            return tab;
        }

        // ── View switching ────────────────────────────────────────────────────

        private void ShowView(GraphView view, VisualElement toolbar)
        {
            if (view == null) return;

            contentArea.Clear();

            // Show conversation bar only when displaying the nodes view
            if (view == graphView && conversationBar != null)
                contentArea.Add(conversationBar);

            if (toolbar != null)
                contentArea.Add(toolbar);

            view.style.flexGrow = 1;
            contentArea.Add(view);
        }

        // ── View / toolbar construction ───────────────────────────────────────

        private void RebuildViews()
        {
            contentArea.Clear();

            if (currentGraph == null)
                return;

            // Dialogue nodes view
            graphView = new DialoguesView(this, currentGraph);
            graphToolbar = BuildGraphToolbar();
            BuildConversationBar();

            // Load first conversation if any exists
            if (currentGraph.s_Conversations.Count > 0)
                graphView.SetCurrentConversation(currentGraph.s_Conversations[0].s_CGuid);

            // List views
            actorsView = new ActorsView(this, currentGraph);
            actorsToolbar = BuildActorsToolbar();

            questsView = new QuestsView(this, currentGraph);
            questsToolbar = BuildQuestsToolbar();

            ShowView(graphView, graphToolbar);
        }

        // ── Toolbar builders ──────────────────────────────────────────────────

        private VisualElement BuildGraphToolbar()
        {
            var toolbar = new Toolbar();
            var addButton = new Button(() => graphView.CreateNode(new Vector2(200, 200)))
            { text = "Add Node" };
            toolbar.Add(addButton);
            return toolbar;
        }

        private VisualElement BuildActorsToolbar()
        {
            var toolbar = new Toolbar();

            toolbar.Add(new Button(() => actorsView.CreateActor())
            { text = "Add Actor" });

            toolbar.Add(new Button(() =>
            {
                if (actorsView.selection.Count == 0)
                {
                    EditorUtility.DisplayDialog("Nothing selected", "Select an actor first.", "OK");
                    return;
                }
                if (actorsView.selection[0] is ActorEditorNode node)
                    actorsView.RemoveActor(node);
            })
            { text = "Remove Actor" });

            return toolbar;
        }

        private VisualElement BuildQuestsToolbar()
        {
            var toolbar = new Toolbar();

            toolbar.Add(new Button(() => questsView.CreateQuest())
            { text = "Add Quest" });

            toolbar.Add(new Button(() =>
            {
                if (questsView.selection.Count == 0)
                {
                    EditorUtility.DisplayDialog("Nothing selected", "Select a quest first.", "OK");
                    return;
                }
                if (questsView.selection[0] is QuestEditorNode node)
                    questsView.RemoveQuest(node);
            })
            { text = "Remove Quest" });

            return toolbar;
        }

        // ── Asset actions ─────────────────────────────────────────────────────

        private void CreateNewGraphAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Dialogue Graph",
                "NewNodeGraph",
                "asset",
                "Choose where to save the asset");

            if (string.IsNullOrEmpty(path))
                return;

            var asset = ScriptableObject.CreateInstance<GraphData>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            currentGraph = asset;
            graphAssetField.SetValueWithoutNotify(asset);

            RebuildViews();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void SaveGraph()
        {
            if (currentGraph == null)
            {
                EditorUtility.DisplayDialog(
                    "No graph assigned",
                    "Assign or create a Dialogue Graph Asset first.",
                    "OK");
                return;
            }

            if (graphView == null)
                graphView = new DialoguesView(this, currentGraph);

            NodeGraphSaveUtility.Save(graphView, currentGraph);
        }
    }
}