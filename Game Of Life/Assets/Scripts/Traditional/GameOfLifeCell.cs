using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Traditional
{
    public class GameOfLifeCell : MonoBehaviour
    {
        public Dictionary<Direction, GameOfLifeCell> neighbors = new Dictionary<Direction, GameOfLifeCell>();
        public bool isAlive = false;
        private bool willBeAlive = false;
        void Start() { }

        //Function to handle logic
        private void FixedUpdate() {
            int aliveNeighborsCount = GetAliveNeighborsCount();
            willBeAlive = isAlive ? aliveNeighborsCount is 2 or 3 : aliveNeighborsCount is 3;
        }

        //Function to update visuals
        public void Update() {
            if (willBeAlive != isAlive)
                ChangeState(willBeAlive);
        }

        int GetAliveNeighborsCount() => neighbors.Values.Count(neighbor => neighbor.isAlive);

        public void ChangeState(bool newState) {
            isAlive = newState;
            if (isAlive)
                OnBecomeAlive();
            else
                OnBecomeDead();
        }

        [Header("Visuals")]
        public MeshRenderer meshRenderer;
        public Material aliveMaterial;
        public Material deadMaterial;
        public void OnBecomeAlive() { meshRenderer.material = aliveMaterial; }
        public void OnBecomeDead() { meshRenderer.material = deadMaterial; }
    }
}