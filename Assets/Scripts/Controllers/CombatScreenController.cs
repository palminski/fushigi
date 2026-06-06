using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatScreenController : MonoBehaviour
{
    public static CombatScreenController Instance { get; private set; }
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text combatLogText;
    private GameInput inputActions;

    
    void Awake()
    {
        if (Instance != null && Instance != this) {Destroy(gameObject); return;}
        Instance = this;
        panel.SetActive(false);
        inputActions = new GameInput();

    }

    void OnEnable()
    {
        inputActions.Gameplay.Click.performed += OnClick;
    }

    void OnDisable()
    {
        inputActions.Gameplay.Click.performed -= OnClick;
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        Hide();
    }

    public void Show(CombatResult result)
    {
        combatLogText.text = "<color=#FFD700>o--|=====- Combat Start -=====|--o</color>\n";
        panel.SetActive(true);
        ScreenManager.Instance.Open();
        StartCoroutine(PrintLog(BuildLog(result)));
    }

    public void Hide()
    {
        panel.SetActive(false);
        StopAllCoroutines();
        ScreenManager.Instance.Close();
    }

    private List<string> BuildLog(CombatResult result)
    {
        List<string> combatLines = new();
        foreach(CombatEvent evt in result.events)
        {
            string line = $"{(evt.hitWasPlayer ? "<color=#32a852>" : "<color=#a83232>")}{evt.hitterName} attacks {evt.hitName} for {evt.damage} ({evt.hitHpBefore} -> {evt.hitHpAfter} HP)</color>";
            if(evt.wasFatal) line += $" - {evt.hitName} defeated!!!";

            combatLines.Add(line);
        }
        return combatLines;
    }

    private IEnumerator PrintLog(List<string> log)
    {
        foreach (string line in log)
        {
            combatLogText.text += ("\n" + line);
            yield return new WaitForSeconds(0.5f);
        }
        combatLogText.text += ("\n\n<color=#FFD700>o--|=====- Combat End -=====|--o</color>");
        yield return new WaitForSeconds(2f);
        Hide();
    }
}
