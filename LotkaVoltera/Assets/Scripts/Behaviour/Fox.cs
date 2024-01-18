using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fox : Animal
{
    public static readonly string[] GeneNames = { "C", "D" };

    public void Start()
    {
        moveArcHeight = 0.1f;
    }
}