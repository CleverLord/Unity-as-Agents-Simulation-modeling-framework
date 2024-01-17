    using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameOfLife.Commons;
using UnityEngine.Serialization;

namespace GameOfLife.DoubleBuffer
{
    [SelectionBase]
    public class GameOfLifeCell : MonoBehaviour
    {
        [FormerlySerializedAs("neighbors")]
        public List<GameOfLifeCell> neighbours = new List<GameOfLifeCell>();
        //Implement alive state as a double buffer
        public bool isAliveEvenFrame = false;
        public bool isAliveOddFrame = false;
        public bool isEvenFrameCurrent = true; //Time.frameCount % 2 == 0
        void Start() { }

        public bool isAlive
        {
            //get refers to the current state, so it's comparing to the 0
            get => isEvenFrameCurrent ? isAliveEvenFrame : isAliveOddFrame;
            //set refers to the next state, so it's comparing to the 1
            set => (isEvenFrameCurrent ? ref isAliveOddFrame : ref isAliveEvenFrame) = value;
        }
        public bool isAliveNextFrame
        {
            get => isEvenFrameCurrent ? isAliveOddFrame : isAliveEvenFrame;
        }

        public void Update() {
            if(Input.GetKeyDown(KeyCode.Space))
                isEvenFrameCurrent = !isEvenFrameCurrent;
        }

        public void LateUpdate() {
            if(Input.GetKeyDown(KeyCode.Space))
                UpdateLogic();
        }

        public void UpdateLogic() {
            int aliveNeighborsCount = GetAliveNeighborsCount();
            bool willBeAlive = isAlive ? aliveNeighborsCount is 2 or 3 : aliveNeighborsCount is 3;
            SetState(willBeAlive);
        }

        public int GetAliveNeighborsCount() => neighbours.Count(neighbor => neighbor.isAlive);

        private void SetState(bool newState) {
            // This works since isAlive.get() returns the current state, but isAlive.set(val) is setting future state
            isAlive = newState;
            if (newState != isAlive)
                RunStateChangeCallbacks();
        }

        public void SetInitialState(bool initialState) {
            isAliveEvenFrame = initialState;
            isAliveOddFrame = initialState;
            RunStateChangeCallbacks();
        }

        private void RunStateChangeCallbacks() {
            if (isAliveNextFrame)
                OnBecomeAlive();
            else
                OnBecomeDead();
        }
        
        [ContextMenu("Revive Cell")]
        public void ReviveCell() {
            SetInitialState(true); // We use SetInitialState() instead of SetState() to force the state change callbacks to run
            // Using SetState() would not run the callbacks, if the state was changed last frame
        }
        [ContextMenu("Kill Cell")]
        public void KillCell() {
            SetInitialState(false);
        }
        
        [Header("Visuals")]
        public MeshRenderer meshRenderer;
        public Material aliveMaterial;
        public Material deadMaterial;

        protected void OnBecomeAlive() {
            meshRenderer.material = aliveMaterial;
        }

        protected void OnBecomeDead() {
            meshRenderer.material = deadMaterial;
        }
    }
}