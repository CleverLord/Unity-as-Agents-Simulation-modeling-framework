using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameOfLife.Commons;
using UnityEngine.Serialization;

namespace GameOfLife.SimpleAgent
{
    [SelectionBase]
    public class GameOfLifeCell : MonoBehaviour
    {
        public List<GameOfLifeCell> neighbours = new List<GameOfLifeCell>();
        public bool isAlive = false;
        private bool willBeAlive = false;
        
        public void Update() {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                int aliveNeighborsCount = GetAliveNeighborsCount();
                willBeAlive = isAlive ? aliveNeighborsCount is 2 or 3 : aliveNeighborsCount is 3;
            }
        }

        public void LateUpdate() {
            SetState(willBeAlive);
        }
        
        public int GetAliveNeighborsCount() => neighbours.Count(neighbor => neighbor.isAlive);

        private void SetState(bool newState) {
            if (newState != isAlive)
            {
                isAlive = newState;
                RunStateChangeCallbacks();
            }
        }

        public void SetInitialState(bool initialState) {
            isAlive = initialState;
            willBeAlive = initialState;
            RunStateChangeCallbacks();
        }

        private void RunStateChangeCallbacks() {
            if (isAlive)
                OnBecomeAlive();
            else
                OnBecomeDead();
        }

        [ContextMenu("Revive Cell")]
        public void ReviveCell() {
            SetState(true);
        }

        [ContextMenu("Kill Cell")]
        public void KillCell() { SetState(false); }

        [Header("Visuals")]
        public MeshRenderer meshRenderer;
        public Material aliveMaterial;
        public Material deadMaterial;

        protected void OnBecomeAlive() { meshRenderer.material = aliveMaterial; }

        protected void OnBecomeDead() { meshRenderer.material = deadMaterial; }
    }
}