using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameOfLife.Commons;

namespace GameOfLife.CpuAccelerated
{
    public class GameOfLifeCell : MonoBehaviour
    {
        public bool isAlive;
        public List<GameOfLifeCell> neighbours;
        public Vector2Int positionInGrid;
        
        public void SetState(bool newState) {
            //This works since isAlive.get() returns the current state, but isAlive.set(val) is setting future state
            if (newState != isAlive)
            {
                isAlive = newState;
                RunStateChangeCallbacks();
            }
        }

        public void SetInitialState(bool initialState) {
            isAlive = initialState;
            RunStateChangeCallbacks();
        }

        private void RunStateChangeCallbacks() {
            if (isAlive)
                OnBecomeAlive();
            else
                OnBecomeDead();
        }

        [Header("Visuals")]
        public MeshRenderer meshRenderer;
        public Material aliveMaterial;
        public Material deadMaterial;

        protected void OnBecomeAlive() { meshRenderer.material = aliveMaterial; }

        protected void OnBecomeDead() { meshRenderer.material = deadMaterial; }
    }
}