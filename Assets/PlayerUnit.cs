using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(UnitAttributes))]
public class PlayerUnit : MapObject
{
    public Mover mover;

    public bool canAct = true;

    private SpriteRenderer spriteRenderer;

    private Color baseColor;

    public UnitAttributes unitAttributes;
    // Start is called before the first frame update
    void Awake()
    {
        unitAttributes = GetComponent<UnitAttributes>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
        mover = GetComponent<Mover>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetInactive()
    {
        canAct = false;
        float lumunence = 0.299f * baseColor.r + 0.587f * baseColor.g + 0.114f * baseColor.b;
        Color inactiveColor = new Color(lumunence, lumunence, lumunence, 0.5f);
        spriteRenderer.color = inactiveColor;
        GameController.Instance.CheckAndChangePhase(); //Temporary for now
    }

    public void SetActive()
    {
        canAct = true;
        spriteRenderer.color = baseColor;

    }
    
}
