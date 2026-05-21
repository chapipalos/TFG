using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class EditorWindow : UnityEditor.EditorWindow
{
    private DialoguesView graphView;
    private ActorsView actorsView;
    private ConditionsView conditionsView;
    private QuestsView questsView;

    private ObjectField graphAssetField;
    private GraphData currentGraph;

    private VisualElement windowVisualElement;

    [MenuItem("Window/Dialogue Framework")]
    public static void Open()
    {
        GetWindow<EditorWindow>("Dialogue Framework");
    }

    public void CreateGUI()
    {
        windowVisualElement = new VisualElement();
        windowVisualElement.style.flexDirection = FlexDirection.Column;
        windowVisualElement.style.flexGrow = 1;

        AddTabs();

        rootVisualElement.Add(windowVisualElement);
    }

    private void AddTabs()
    {
        var visualAux = new VisualElement();
        visualAux.style.flexDirection = FlexDirection.Row;

        var tab1 = new Tab("Nodes view");
        tab1.style.paddingRight = 4;
        tab1.style.paddingLeft = 4;
        tab1.selected += _ =>
        {
            if(graphAssetField.value == null)
            {
                EditorUtility.DisplayDialog("Error", "Assign or create a Dialogue Graph Asset first.", "OK");
                return;
            }
            AddGraphView();
        };

        var tab2 = new Tab("Actors view");
        tab2.style.paddingRight = 4;
        tab2.style.paddingLeft = 4;
        tab2.selected += _ =>
        {
            if (graphAssetField.value == null)
            {
                EditorUtility.DisplayDialog("Error", "Assign or create a Dialogue Graph Asset first.", "OK");
                return;
            }
            AddActorsView();
        };

        var tab3 = new Tab("Conditions view");
        tab3.style.paddingRight = 4;
        tab3.style.paddingLeft = 4;
        tab3.selected += _ =>
        {
            if (graphAssetField.value == null)
            {
                EditorUtility.DisplayDialog("Error", "Assign or create a Dialogue Graph Asset first.", "OK");
                return;
            }
            AddConditionsView();
        };

        var tab4 = new Tab("Events view");
        tab4.style.paddingRight = 4;
        tab4.style.paddingLeft = 4;
        tab4.selected += _ =>
        {
            if (graphAssetField.value == null)
            {
                EditorUtility.DisplayDialog("Error", "Assign or create a Dialogue Graph Asset first.", "OK");
                return;
            }
            AddQuestsView();
        };

        GetGraph(visualAux);

        visualAux.Add(tab1);
        visualAux.Add(tab2);
        visualAux.Add(tab3);
        visualAux.Add(tab4);

        visualAux.style.alignItems = Align.Center;
        visualAux.style.alignSelf = Align.Center;

        rootVisualElement.Add(visualAux);
    }

    private void GetGraph(VisualElement v)
    {
        var aux = new VisualElement();

        aux.style.flexDirection = FlexDirection.Row;

        graphAssetField = new ObjectField("Dialogue Graph Asset")
        {
            objectType = typeof(GraphData),
            allowSceneObjects = false
        };
        graphAssetField.RegisterValueChangedCallback(evt =>
        {
            currentGraph = evt.newValue as GraphData;
            LoadGraph();
        });

        var newButton = new Button(CreateNewGraphAsset)
        {
            text = "New Dialogue Graph"
        };

        var saveButton = new Button(SaveGraph)
        {
            text = "Save Dialogue Graph"
        };

        aux.Add(graphAssetField);
        aux.Add(newButton);
        aux.Add(saveButton);

        aux.style.paddingRight = 20;

        v.Add(aux);
    }

    private void AddGraphView()
    {
        windowVisualElement.Clear();

        var visualAux = new VisualElement();
        visualAux.style.flexDirection = FlexDirection.Row;

        var toolbar = new Toolbar();

        var addNodeButton = new Button(() =>
        {
            graphView.CreateNode(new Vector2(200, 200));
        })
        {
            text = "Add Node"
        };

        toolbar.Add(addNodeButton);

        visualAux.Add(toolbar);

        visualAux.style.alignContent = Align.Auto;
        visualAux.style.alignItems = Align.Center;
        visualAux.style.alignSelf = Align.Center;

        windowVisualElement.Add(visualAux);

        graphView = new DialoguesView(this, currentGraph);
        graphView.style.flexGrow = 1;
        windowVisualElement.Add(graphView);
    }

    private void AddActorsView()
    {
        windowVisualElement.Clear();

        var visualAux = new VisualElement();
        visualAux.style.flexDirection = FlexDirection.Row;

        var toolbar = new Toolbar();

        var addActorButton = new Button(() =>
        {
            actorsView.CreateActor();
        })
        {
            text = "Add Actor"
        };

        var removeActorButton = new Button(() =>
        {
            if (actorsView.selection.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Select an actor to remove from the list.", "OK");
                return;
            }

            var selectedActor = actorsView.selection[0] as ActorEditorNode;
            if (selectedActor != null)
            {
                actorsView.RemoveActor(selectedActor);
            }
        })
        {
            text = "Remove Actor"
        };

        toolbar.Add(addActorButton);
        toolbar.Add(removeActorButton);

        visualAux.Add(toolbar);

        visualAux.style.alignContent = Align.Auto;
        visualAux.style.alignItems = Align.Center;
        visualAux.style.alignSelf = Align.Center;

        windowVisualElement.Add(visualAux);


        actorsView = new ActorsView(this, currentGraph);
        actorsView.style.flexGrow = 1;
        windowVisualElement.Add(actorsView);
    }

    private void AddConditionsView()
    {
        windowVisualElement.Clear();

        var visualAux = new VisualElement();
        visualAux.style.flexDirection = FlexDirection.Row;

        var toolbar = new Toolbar();

        var addConditionButton = new Button(() =>
        {
            conditionsView.CreateCondition();
        })
        {
            text = "Add Condition"
        };

        var removeConditionButton = new Button(() =>
        {
            if (conditionsView.selection.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Select an actor to remove from the list.", "OK");
                return;
            }

            var selectedActor = conditionsView.selection[0] as ConditionEditorNode;
            if (selectedActor != null)
            {
                conditionsView.RemoveCondition(selectedActor);
            }
        })
        {
            text = "Remove Condition"
        };

        toolbar.Add(addConditionButton);
        toolbar.Add(removeConditionButton);

        visualAux.Add(toolbar);

        visualAux.style.alignContent = Align.Auto;
        visualAux.style.alignItems = Align.Center;
        visualAux.style.alignSelf = Align.Center;

        windowVisualElement.Add(visualAux);


        conditionsView = new ConditionsView(this, currentGraph);
        conditionsView.style.flexGrow = 1;
        windowVisualElement.Add(conditionsView);
    }

    private void AddQuestsView()
    {
        windowVisualElement.Clear();

        var visualAux = new VisualElement();
        visualAux.style.flexDirection = FlexDirection.Row;

        var toolbar = new Toolbar();

        var addQuestButton = new Button(() =>
        {
            questsView.CreateQuest();
        })
        {
            text = "Add Quest"
        };

        var removeQuestButton = new Button(() =>
        {
            if (questsView.selection.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Select a quest to remove from the list.", "OK");
                return;
            }

            var selectedQuest = questsView.selection[0] as QuestEditorNode;
            if (selectedQuest != null)
            {
                questsView.RemoveQuest(selectedQuest);
            }
        })
        {
            text = "Remove Quest"
        };

        toolbar.Add(addQuestButton);
        toolbar.Add(removeQuestButton);

        visualAux.Add(toolbar);

        visualAux.style.alignContent = Align.Auto;
        visualAux.style.alignItems = Align.Center;
        visualAux.style.alignSelf = Align.Center;

        windowVisualElement.Add(visualAux);

        questsView = new QuestsView(this, currentGraph);
        questsView.style.flexGrow = 1;
        windowVisualElement.Add(questsView);
    }

    private void CreateNewGraphAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Dialogue Graph",
            "NewNodeGraph",
            "asset",
            "Chose where do you want to save the asset"
        );

        if (string.IsNullOrEmpty(path))
            return;

        var asset = ScriptableObject.CreateInstance<GraphData>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        currentGraph = asset;
        graphAssetField.SetValueWithoutNotify(asset);

        graphView.ClearGraph();

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    private void SaveGraph()
    {
        if (currentGraph == null)
        {
            EditorUtility.DisplayDialog("Error", "Assign or create a Dialogue Graph Asset first.", "OK");    
            return;
        }

        NodeGraphSaveUtility.Save(graphView, currentGraph);
    }

    private void LoadGraph()
    {
        if (currentGraph == null)
        {
            windowVisualElement.Clear();
            graphView.ClearGraph();
            return;
        }

        NodeGraphSaveUtility.Load(graphView, currentGraph);
    }
}