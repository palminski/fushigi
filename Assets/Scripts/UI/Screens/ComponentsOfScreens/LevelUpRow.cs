using TMPro;
using UnityEngine;

public class LevelUpRow : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text value;
    [SerializeField] private TMP_Text gain;

    public void Setup(string statName, int oldValue)
    {
        label.text = statName;
        value.text = oldValue.ToString();
        gain.gameObject.SetActive(false);
    }

    public void Reveal(int newValue, int ammount)
    {
        value.text = newValue.ToString();
        gain.text = "+" + ammount;
        gain.gameObject.SetActive(true);
    }
}
