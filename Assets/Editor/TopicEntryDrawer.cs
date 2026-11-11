using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

// Flat Inspector layouts for NPC dialogue data, so the nested lists stay readable.
// Nested blocks (responses, outcomes, choices) are indented with a line down their left edge.

// [ topic ]  Alias [ ......... ]
// Responses (list)
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

        root.Add(new PropertyField(property.FindPropertyRelative("responses")));
        return root;
    }
}

// A foldout headed by the start of the response's text. Inside: the text, Requires / Forbids, the outcome and any choices,
// all open, so stray flags can't hide in a collapsed list
[CustomPropertyDrawer(typeof(TopicResponse))]
public class TopicResponseDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        SerializedProperty text = property.FindPropertyRelative("text");

        // Open/closed is remembered on the response itself (Unity keeps it while the editor is open).
        // Everything inside is always open, so opening a response shows all of it.
        var foldout = new Foldout { text = Heading(text), value = property.isExpanded };
        foldout.RegisterValueChangedCallback(evt =>
        {
            if (evt.target == foldout) property.isExpanded = evt.newValue; // ignore lists inside folding and unfolding
        });
        foldout.TrackPropertyValue(text, changed => foldout.text = Heading(changed));

        var block = DialogueDrawerStyle.Block();
        block.Add(new PropertyField(text, ""));
        block.Add(DialogueDrawerStyle.OpenList(property.FindPropertyRelative("requires")));
        block.Add(DialogueDrawerStyle.OpenList(property.FindPropertyRelative("forbids")));
        block.Add(new PropertyField(property.FindPropertyRelative("outcome")));
        block.Add(DialogueDrawerStyle.OpenList(property.FindPropertyRelative("choices")));
        foldout.Add(block);
        return foldout;
    }

    // The first line of the text, cut short, so collapsed responses can still be told apart
    private static string Heading(SerializedProperty text)
    {
        string firstLine = (text.stringValue ?? "").Split('\n')[0].Trim();
        if (firstLine.Length == 0) return "Response (no text)";
        return firstLine.Length > 50 ? firstLine.Substring(0, 50) + "…" : firstLine;
    }
}

// The four kinds of outcome as lists (empty lists stay one line tall)
[CustomPropertyDrawer(typeof(DialogueOutcome))]
public class DialogueOutcomeDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = DialogueDrawerStyle.Block();
        root.Add(new Label("Outcome") { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold } });
        root.Add(DialogueDrawerStyle.OpenList(property.FindPropertyRelative("unlocks")));
        root.Add(DialogueDrawerStyle.OpenList(property.FindPropertyRelative("setFlags")));
        root.Add(DialogueDrawerStyle.OpenList(property.FindPropertyRelative("giveItems")));
        root.Add(DialogueDrawerStyle.OpenList(property.FindPropertyRelative("raiseEvents")));
        return root;
    }
}

// [ label ]  Continue to [ topic ]
// [ reply ]
// Outcome
[CustomPropertyDrawer(typeof(DialogueChoice))]
public class DialogueChoiceDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = DialogueDrawerStyle.Block();

        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        row.Add(new PropertyField(property.FindPropertyRelative("label"), "") { style = { flexGrow = 1, flexBasis = 0 } });
        row.Add(new Label("Continue to") { style = { marginLeft = 8 } });
        row.Add(new PropertyField(property.FindPropertyRelative("continueTo"), "") { style = { flexGrow = 1, flexBasis = 0 } });
        root.Add(row);

        root.Add(new PropertyField(property.FindPropertyRelative("reply"), ""));
        root.Add(new PropertyField(property.FindPropertyRelative("outcome")));
        return root;
    }
}

// [ topic ] [ scope ▾ ]
[CustomPropertyDrawer(typeof(TopicUnlock))]
public class TopicUnlockDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        return DialogueDrawerStyle.ScopedRow(property.FindPropertyRelative("topic"), property.FindPropertyRelative("scope"));
    }
}

// [ flag ] [ scope ▾ ]
[CustomPropertyDrawer(typeof(FlagChange))]
public class FlagChangeDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        return DialogueDrawerStyle.ScopedRow(property.FindPropertyRelative("flag"), property.FindPropertyRelative("scope"));
    }
}

static class DialogueDrawerStyle
{
    // An indented block with a faint line down its left edge, so you can see what's nested inside what
    public static VisualElement Block()
    {
        var block = new VisualElement();
        block.style.marginLeft = 4;
        block.style.marginTop = 2;
        block.style.marginBottom = 4;
        block.style.paddingLeft = 6;
        block.style.borderLeftWidth = 2;
        block.style.borderLeftColor = new UnityEngine.Color(0.5f, 0.5f, 0.5f, 0.4f);
        return block;
    }

    // A list that's always open when its parent is - used inside responses, which do the folding
    public static PropertyField OpenList(SerializedProperty list)
    {
        list.isExpanded = true;
        return new PropertyField(list);
    }

    // An asset field and a scope dropdown on one row
    public static VisualElement ScopedRow(SerializedProperty asset, SerializedProperty scope)
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        row.Add(new PropertyField(asset, "") { style = { flexGrow = 1 } });
        row.Add(new PropertyField(scope, "") { style = { width = 110 } });
        return row;
    }
}
