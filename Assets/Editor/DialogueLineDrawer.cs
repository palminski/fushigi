using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

// Draws each conversation line flat in the Inspector (no foldouts): one row per side, then the text
//     Left   [ speaker ]  ☐ Darken  ☐ Clear
//     Right  [ speaker ]  ☐ Darken  ☐ Clear
//     [ text ]
[CustomPropertyDrawer(typeof(DialogueLine))]
public class DialogueLineDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement();
        root.Add(SlotRow("Left", property.FindPropertyRelative("left")));
        root.Add(SlotRow("Right", property.FindPropertyRelative("right")));
        root.Add(new PropertyField(property.FindPropertyRelative("text"), ""));
        return root;
    }

    private static VisualElement SlotRow(string sideName, SerializedProperty slot)
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

        row.Add(new Label(sideName) { style = { width = 40 } });
        row.Add(new PropertyField(slot.FindPropertyRelative("speaker"), "") { style = { flexGrow = 1 } });
        row.Add(LabeledToggle(slot.FindPropertyRelative("darken"), "Darken"));
        row.Add(LabeledToggle(slot.FindPropertyRelative("clear"), "Clear"));
        return row;
    }

    // A checkbox with its label to the right, sized to fit
    private static Toggle LabeledToggle(SerializedProperty property, string label)
    {
        var toggle = new Toggle { text = label, style = { marginLeft = 8 } };
        toggle.BindProperty(property);
        return toggle;
    }
}
