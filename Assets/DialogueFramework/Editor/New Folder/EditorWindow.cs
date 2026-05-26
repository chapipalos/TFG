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
        // Created once per graph load and re-attached when switching tabs.
        // Never recreated just because the user clicked a different tab.
        private DialoguesView graphView;
        private ActorsView actorsView;
        private ConditionsView conditionsView;
        private QuestsView questsView;

        // ── Toolbar containers ────────────────────────────────────────────────
        // Kept alive so we can re-Add them without rebuilding buttons.
        private VisualElement graphToolbar;
        private VisualElement actorsToolbar;
        private VisualElement conditionsToolbar;
        private VisualElement questsToolbar;

        // ── State ─────────────────────────────────────────────────────────────
        private ObjectField graphAssetField;
        private GraphData currentGraph;
        private VisualElement contentArea;   // scrollable area below the tab bar

        // ── Menu entry ────────────────────────────────────────────────────────

        [MenuItem("Window/Dialogue Framework")]
        public static void Open() => GetWindow<EditorWindow>("Dialogue Framework");

        // ── Unity lifecycle ───────────────────────────────────────────────────

        public void CreateGUI()
        {
            // Top bar: asset picker + tabs
            BuildTopBar();

            // Content area grows to fill the remaining space
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

            // Asset picker + action buttons
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

            // Tabs
            topBar.Add(BuildTab("Nodes", () => ShowView(graphView, graphToolbar)));
            topBar.Add(BuildTab("Actors", () => ShowView(actorsView, actorsToolbar)));
            topBar.Add(BuildTab("Conditions", () => ShowView(conditionsView, conditionsToolbar)));
            topBar.Add(BuildTab("Quests", () => ShowView(questsView, questsToolbar)));

            rootVisualElement.Add(topBar);
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

        /// <summary>
        /// Swap the content area to show <paramref name="view"/> with its
        /// <paramref name="toolbar"/>. The previous view is detached, not destroyed,
        /// so scroll position and selection survive tab switches.
        /// </summary>
        private void ShowView(GraphView view, VisualElement toolbar)
        {
            // Guard: views are null until RebuildViews() has been called
            if (view == null) return;

            contentArea.Clear();

            if (toolbar != null)
                contentArea.Add(toolbar);

            view.style.flexGrow = 1;
            contentArea.Add(view);
        }

        // ── View / toolbar construction ───────────────────────────────────────

        /// <summary>
        /// Called whenever a new GraphData is assigned.
        /// Tears down old views and builds fresh ones from the new data.
        /// </summary>
        private void RebuildViews()
        {
            contentArea.Clear();

            if (currentGraph == null)
                return;

            // Dialogue nodes view — constructor is empty, Load fills nodes + links
            graphView = new DialoguesView(this, currentGraph);
            graphToolbar = BuildGraphToolbar();
            NodeGraphSaveUtility.Load(graphView, currentGraph);

            // List views
            actorsView = new ActorsView(this, currentGraph);
            actorsToolbar = BuildActorsToolbar();

            conditionsView = new ConditionsView(this, currentGraph);
            conditionsToolbar = BuildConditionsToolbar();

            questsView = new QuestsView(this, currentGraph);
            questsToolbar = BuildQuestsToolbar();

            // Show nodes view by default after loading
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

        private VisualElement BuildConditionsToolbar()
        {
            var toolbar = new Toolbar();

            toolbar.Add(new Button(() => conditionsView.CreateCondition())
            { text = "Add Condition" });

            toolbar.Add(new Button(() =>
            {
                if (conditionsView.selection.Count == 0)
                {
                    EditorUtility.DisplayDialog("Nothing selected", "Select a condition first.", "OK");
                    return;
                }
                if (conditionsView.selection[0] is ConditionEditorNode node)
                    conditionsView.RemoveCondition(node);
            })
            { text = "Remove Condition" });

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

            // BUG FIX: graphView may be null here if the user has never
            // opened the Nodes tab. RebuildViews() handles initialization safely.
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

            // BUG FIX: graphView could be null if the Nodes tab was never opened.
            // Initialize it now if needed so Save always works.
            if (graphView == null)
                graphView = new DialoguesView(this, currentGraph);

            NodeGraphSaveUtility.Save(graphView, currentGraph);
        }
    }
}