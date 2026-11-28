using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpScreen : OverlayScreen
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text characterName;
    [SerializeField] private TMP_Text characterLevel;

    [Header("Rows")]
    [SerializeField] private LevelUpRow healthRow;
    [SerializeField] private LevelUpRow strengthRow;
    [SerializeField] private LevelUpRow skillRow;
    [SerializeField] private LevelUpRow speedRow;
    [SerializeField] private LevelUpRow defenceRow;
    [SerializeField] private LevelUpRow buildRow;
    [SerializeField] private LevelUpRow magicRow;
    [SerializeField] private LevelUpRow movementRow;

    [Header("Timing")]
    [SerializeField] private float fadeSeconds = 0.2f;
    [SerializeField] private float holdTime = 1.2f;
    [SerializeField] private float secondsPerStat = 0.25f;

    private bool skipping;
    private bool closeRequested;


    public void Show(PlayerUnit unit, StatBlock gains)
    {
        portrait.sprite = unit.character.portrait;
        characterName.text = unit.character.characterName;
        characterLevel.text = "Level "+unit.unitAttributes.level.ToString();

        UnitAttributes attributes = unit.unitAttributes;

        healthRow.Setup("Health:", attributes.health - gains.health);
        strengthRow.Setup("Strength:", attributes.strength - gains.strength);
        skillRow.Setup("Skill:", attributes.skill - gains.skill);
        speedRow.Setup("Speed:", attributes.speed - gains.speed);
        defenceRow.Setup("Defence:", attributes.defence - gains.defence);
        magicRow.Setup("Magic:", attributes.magic - gains.magic);
        buildRow.Setup("Build:", attributes.build - gains.build);
        movementRow.Setup("Move:", attributes.movement - gains.movement);

        StartCoroutine(Play(unit, gains));
    }

    private IEnumerator Play(PlayerUnit playerUnit, StatBlock gains)
    {
        yield return Fade(0f,1f);
        
        yield return RevelIf(healthRow, playerUnit.unitAttributes.health, gains.health);
        yield return RevelIf(strengthRow, playerUnit.unitAttributes.strength, gains.strength);
        yield return RevelIf(skillRow, playerUnit.unitAttributes.skill, gains.skill);
        yield return RevelIf(speedRow, playerUnit.unitAttributes.speed, gains.speed);
        yield return RevelIf(defenceRow, playerUnit.unitAttributes.defence, gains.defence);
        yield return RevelIf(magicRow, playerUnit.unitAttributes.magic, gains.magic);
        yield return RevelIf(buildRow, playerUnit.unitAttributes.build, gains.build);
        yield return RevelIf(movementRow, playerUnit.unitAttributes.movement, gains.movement);

        skipping = true;
        for (float t = 0; t < holdTime && !closeRequested; t += Time.deltaTime)
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

    private IEnumerator RevelIf(LevelUpRow levelUpRow, int newValue, int gain)
    {
        if(gain <= 0) yield break;
        levelUpRow.Reveal(newValue, gain);
        for (float t = 0; t < secondsPerStat && !skipping; t += Time.deltaTime)
        {
            yield return null;
        }
    }

    public override void OnClick()
    {
        if (skipping  == true) closeRequested = true;
        skipping = true;
    }
}
