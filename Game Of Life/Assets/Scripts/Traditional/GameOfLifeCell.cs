using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameOfLife.Commons;

namespace GameOfLife.Traditional
{
    public class GameOfLifeCell : MonoBehaviour
    {
        public List<GameOfLifeCell> neighbors = new List<GameOfLifeCell>();
        //Implement alive state as a double buffer
        public bool isAliveEvenFrame = false;
        public bool isAliveOddFrame = false;
        public string isAliveBoolName;
        void Start() { }

        public bool isAlive
        {
            //get refers to the current state, so it's comparing to the 0
            get => Time.frameCount % 2 == 0 ? isAliveEvenFrame : isAliveOddFrame;
            //set refers to the next state, so it's comparing to the 1
            set => (Time.frameCount % 2 == 0 ? ref isAliveOddFrame : ref isAliveEvenFrame) = value;
        }
        public bool isAliveNextFrame
        {
            get => Time.frameCount % 2 == 0 ? isAliveOddFrame : isAliveEvenFrame;
        }

        public void Update() {
            int frameNumber = Time.frameCount;
            isAliveBoolName = Time.frameCount % 2 == 0 ? "isAliveEvenFrame" : "isAliveOddFrame";
            if (frameNumber < 2)
                return;
            UpdateLogic();
        }

        public void UpdateLogic() {
            int aliveNeighborsCount = GetAliveNeighborsCount();
            bool willBeAlive = isAlive ? aliveNeighborsCount is 2 or 3 : aliveNeighborsCount is 3;
            SetState(willBeAlive);
        }

        public int GetAliveNeighborsCount() => neighbors.Count(neighbor => neighbor.isAlive);

        private void SetState(bool newState) {
            if (newState != isAlive)
            {
                isAlive = newState;
                RunStateChangeCallbacks();
            }
            else 
                isAlive = newState; // this ensures that the dual buffer is updated
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

        [Header("Visuals")]
        public MeshRenderer meshRenderer;
        public Material aliveMaterial;
        public Material deadMaterial;

        protected void OnBecomeAlive() {
            meshRenderer.material = aliveMaterial;
            //Debug.Log("Cell became alive");
        }

        protected void OnBecomeDead() {
            meshRenderer.material = deadMaterial;
            //Debug.Log("Cell became dead");
        }
    }
}