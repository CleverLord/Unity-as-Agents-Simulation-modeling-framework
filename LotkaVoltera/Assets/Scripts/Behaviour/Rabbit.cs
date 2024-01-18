using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rabbit : Animal {
    public static readonly string[] GeneNames = { "A", "B" };

    public void Start()
    {
        moveArcHeight = 0.1f;
    }
}