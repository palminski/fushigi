using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Prints the combat log line by line over the map. Click to skip; it also closes itself shortly after the last line.
// A placeholder until combat animations replace it.
public class CombatReadoutScreen : OverlayScreen
{
    [SerializeField] private TMP_Text combatLogText;
    [SerializeField] private float secondsPerLine = 0.5f;
    [SerializeField] private float secondsAfterEnd = 2f;

    public void Show(CombatResult result)
    {
        combatLogText.text = "<color=#FFD700>o--|=====- Combat Start -=====|--o</color>\n";
        StartCoroutine(PrintLog(BuildLog(result)));
    }

    public override void OnClick()
    {
        Close();
    }

    private List<string> BuildLog(CombatResult result)
    {
        List<string> combatLines = new();
        foreach (CombatEvent evt in result.events)
        {
            string color = evt.hitWasPlayer ? "<color=#32a852>" : "<color=#a83232>";
            string line = evt.didHit
                ? $"{color}{evt.hitterName} attacks {evt.hitName} for {evt.damage} ({evt.hitHpBefore} -> {evt.hitHpAfter} HP)</color>"
                : $"{color}{evt.hitterName} attacks {evt.hitName}, but they missed!</color>";
            if (evt.wasFatal) line += $" - {evt.hitName} defeated!!!";
            combatLines.Add(line);
        }
        return combatLines;
    }

    private IEnumerator PrintLog(List<string> log)
    {
        foreach (string line in log)
        {
            combatLogText.text += "\n" + line;
            yield return new WaitForSeconds(secondsPerLine);
        }
        combatLogText.text += "\n\n<color=#FFD700>o--|=====- Combat End -=====|--o</color>";
        yield return new WaitForSeconds(secondsAfterEnd);
        Close();
    }
}
