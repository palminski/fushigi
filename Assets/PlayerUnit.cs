using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUnit : MapObject
{
    public Mover mover;
    // Start is called before the first frame update
    void Awake()
    {
        mover = GetComponent<Mover>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}
