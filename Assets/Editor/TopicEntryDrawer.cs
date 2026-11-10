using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

// Draws each NPC topic flat in the Inspector (no foldouts):
//     [ topic ]  Alias [ ......... ]
//     [ response ]
//     Unlocks  (list of  [ topic ] [ scope ▾ ])
[CustomPropertyDrawer(typeof(TopicEntry))]
public class TopicEntryDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement();

        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        row.Add(new PropertyField(property.FindPropertyRelative("topic"), "") { style = { flexGrow = 1, flexBasis = 0 } });
        row.Add(new Label("Alias") { style = { marginLeft = 8 } });
        row.Add(new PropertyField(property.FindPropertyRelative("alias"), "") { style = { flexGrow = 1, flexBasis = 0 } });

        root.Add(row);
        root.Add(new PropertyField(property.FindPropertyRelative("response"), ""));
        root.Add(new PropertyField(property.FindPropertyRelative("unlocks")));
        return root;
    }
}

// Draws each unlock as one row:  [ topic ] [ scope ▾ ]
[CustomPropertyDrawer(typeof(TopicUnlock))]
public class TopicUnlockDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        row.Add(new PropertyField(property.FindPropertyRelative("topic"), "") { style = { flexGrow = 1 } });
        row.Add(new PropertyField(property.FindPropertyRelative("scope"), "") { style = { width = 110 } });
        return row;
    }
}
