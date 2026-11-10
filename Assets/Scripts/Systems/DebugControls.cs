using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Development shortcuts: Esc quits (or leaves Play mode in the editor), R restarts the current level.
// Reads the keyboard directly rather than through GameInput, since these aren't part of the game's controls.
public class DebugControls : MonoBehaviour
{

    [SerializeField] private NpcDialogue testTopicDialogue;
    [SerializeField] private PlayerUnit testTopicDialogueInitiator;
    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return; // no keyboard connected

        if (keyboard.escapeKey.wasPressedThisFrame) Quit();
        if (keyboard.rKey.wasPressedThisFrame) RestartScene();
        if (keyboard.f9Key.wasPressedThisFrame)
        {
            GameSaveManager.NukeSaveData();
            RestartScene();
        }
        if (keyboard.tKey.wasPressedThisFrame && testTopicDialogue != null && !ScreenManager.Instance.IsBlocking)
        {
            StartCoroutine(DialogueManager.Instance.StartTopicDialogue(testTopicDialogue, testTopicDialogueInitiator));
        }
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
