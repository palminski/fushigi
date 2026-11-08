using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(DialogueLine))]
public class DialogueLineDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement();

        var row = new VisualElement{style = {flexDirection = FlexDirection.Row}};
        var speaker = new PropertyField(property.FindPropertyRelative("speaker")){style = {flexGrow = 1}};
        var side = new PropertyField(property.FindPropertyRelative("side"), ""){style = {width = 70}};

        row.Add(speaker);
        row.Add(side);

        root.Add(row);
        root.Add(new PropertyField(property.FindPropertyRelative("text"), ""));

        return root;
    }
}
