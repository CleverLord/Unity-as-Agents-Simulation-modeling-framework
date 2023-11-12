using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameOfLife.Commons;

namespace GameOfLife.PureCpu
{
    [Serializable]
    public class GameOfLifeCell
    {
        public bool isAlive;
        [NonSerialized]
        public List<GameOfLifeCell> neighbours;
        public Vector2Int positionInGrid;
    }
}