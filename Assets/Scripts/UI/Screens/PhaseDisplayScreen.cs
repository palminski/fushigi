using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PhaseDisplayScreen : OverlayScreen
{
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private Image phaseBackground;

    [Header("Display Info")]
    [SerializeField] private Color PlayerBGColor;
    [SerializeField] private Color EnemyBGColor;
    [SerializeField] private Color AllyBGColor;

    [Header("Timing")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeSeconds = 0.2f;
    [SerializeField] private float holdTime = 1.2f;

    private bool skipping;


    public void Show(GamePhase phase, string displayText = null, Color? displayColor = null)
    {

        switch (phase)
        {
            case GamePhase.Player:
                phaseText.text = "Player Phase";
                phaseBackground.color = PlayerBGColor;
                break;

            case GamePhase.Enemy:
                phaseText.text = "Enemy Phase";
                phaseBackground.color = EnemyBGColor;
                break;
            case GamePhase.Ally:
                phaseText.text = "Ally Phase";
                phaseBackground.color = AllyBGColor;
                break;
        }
        if (displayText != null)
        {
            phaseText.text = displayText;
        }
        if (displayColor.HasValue)
        {
            phaseBackground.color = displayColor.Value;
        }
        StartCoroutine(ShowBanner());
    }

    private IEnumerator ShowBanner()
    {
        
        yield return Fade(0f,1f);
        for (float t = 0; t < holdTime && !skipping; t += Time.deltaTime)
        {
            yield return null;
        }
        skipping = false;
        yield return Fade(1f, 0f);
        Close();
    }

    public override void OnClick()
    {
        skipping = true;
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha)
    {
        for (float t = 0; t < fadeSeconds && !skipping; t += Time.deltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t / fadeSeconds);
            yield return null;
        }
        canvasGroup.alpha = toAlpha;
    }
}
