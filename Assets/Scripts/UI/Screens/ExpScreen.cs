using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExpScreen : OverlayScreen
{
    [SerializeField] private Image fillBar;
    [SerializeField] private TMP_Text expText;
    private bool skipping;

    [Header("Timing")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float expFillSpeed=100f;
    [SerializeField] private float fadeSeconds = 0.2f;
    [SerializeField] private float holdTime = 1.2f;
    
    public void Show(PlayerUnit playerUnit, int beforeExp, int gainedExp)
    {
        StartCoroutine(ShowBanner(beforeExp, gainedExp));
        
    }

    private IEnumerator ShowBanner(int before, int gained)
    {
        SetBar(before);
        yield return Fade(0f,1f);
        yield return FillBar(before,gained);
        for (float t = 0; t < holdTime && !skipping; t += Time.deltaTime)
        {
            yield return null;
        }
        skipping = false;
        yield return Fade(1f, 0f);
        Close();
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

    private IEnumerator FillBar(int before, int gained)
    {
        float shown = before;
        float target = before + gained;
        while (shown < target && !skipping)
        {
            shown = Mathf.MoveTowards(shown, target, expFillSpeed * Time.deltaTime);
            SetBar(shown % 100f);
            yield return null;
        }
        SetBar(target % 100f);
    }


    private void SetBar(float barPercent)
    {
        fillBar.fillAmount = barPercent / 100f;
        expText.text = ((int)barPercent).ToString();
    }

    public override void OnClick()
    {
        skipping = true;
    }

    

    
}
