using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameOfLife.Commons
{
    public class Defaults : MonoBehaviour
    {
        public Vector2Int gridSize = new Vector2Int(32, 18);
        public int iterationCount = 100;
        public GridMap gridMap;
        public static Defaults Instance { get; private set; }
        public void Awake() {
            Instance = this;
        }
    } 
}