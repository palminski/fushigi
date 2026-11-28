using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Scriptable Objects/Units/Character")]
public class Character : ScriptableObject
{
    public string characterName;
    public Sprite portrait;
    public JobClass characterClass;


    public StatBlock growths;
    public StatBlock bases;
    public int baseLevel = 1;
    public List<Skill> skills;
    
    public Color color; //Will be used late MAYBE

}
