using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class ConditionEditorNode : Node
{
    public ConditionData Data { get; }

    public ConditionEditorNode(Vector2 size, ConditionData data)
    {
        Data = data;
        title = data.name;

        style.width = size.x;
        style.height = size.y;

        var textField = new TextField("Name");
        textField.SetValueWithoutNotify(data.name);
        textField.RegisterValueChangedCallback(evt =>
        {
            Data.name = evt.newValue;
            title = evt.newValue;
        });

        extensionContainer.Add(textField);

        // Agrega un toggle para marcar si la condición se cumple o no
        var toggle = new Toggle("Is Condition Met");
        toggle.SetValueWithoutNotify(data.value);
        toggle.RegisterValueChangedCallback(evt => {
            data.value = evt.newValue;
        });

        extensionContainer.Add(toggle);


        RefreshExpandedState();
        RefreshPorts();

        SetPosition(new Rect(Vector2.zero, size));
    }

    public void SetNodeSize(float width, float height)
    {
        style.width = width;
        style.height = height;

        Rect rect = GetPosition();
        rect.width = width;
        rect.height = height;
        SetPosition(rect);
    }
}
