using UnityEngine;

[CreateAssetMenu()]
public abstract class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;
}
