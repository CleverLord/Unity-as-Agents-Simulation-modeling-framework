using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameOfLife.Performant.GPU
{
    public class GameOfLifeGPU : MonoBehaviour
    {
        public Texture2D startImage;
        public Material gameOfLifeMaterial;
        public Material gameOfLifeDisplayMaterial;
        public Material gameOfLifeDisplayOldMaterial;
        public RenderTexture currentGameState;
        public RenderTexture newGameState;
        public int iterationsPerFrame = 1;
        public int iterationCount = 0;
        public int unityFrames = 0;
        public int targetTime = 10;

        private void Start() {
            if (startImage == null)
            {
                Debug.LogError("Please assign a start image to the GameOfLifeGPU script");
                this.enabled = false;
            }

            // Create a temporary RenderTexture to hold the loaded image
            currentGameState = new RenderTexture(startImage.width, startImage.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            currentGameState.filterMode = FilterMode.Point;
            currentGameState.enableRandomWrite = true;
            currentGameState.Create();

            //Create a temporary RenderTexture to hold the new state
            newGameState = new RenderTexture(currentGameState);
            newGameState.filterMode = FilterMode.Point;
            newGameState.enableRandomWrite = true;
            newGameState.Create();

            //print the colorFormats
            Debug.Log("currentGameState.format: " + currentGameState.format);
            Debug.Log("startImage.format: " + startImage.format);

            // Copy the loaded image into the temporary RenderTexture
            Graphics.CopyTexture(startImage, currentGameState);
            gameOfLifeDisplayMaterial.mainTexture = currentGameState;
            gameOfLifeDisplayOldMaterial.mainTexture = currentGameState;
        }

        private void OnDestroy() {
            currentGameState.Release();
            newGameState.Release();
        }

        private bool resultPrinted = false;

        private void Update() {
            if (Time.frameCount < 2)
                return;
            for (int i = 0; i < iterationsPerFrame; i++)
            {
                // Render the new state using the shader
                Graphics.Blit(currentGameState, newGameState, gameOfLifeMaterial);
                gameOfLifeDisplayOldMaterial.mainTexture = currentGameState;
                gameOfLifeDisplayMaterial.mainTexture = newGameState;

                (currentGameState, newGameState) = (newGameState, currentGameState);
            }

            iterationCount += iterationsPerFrame;
            unityFrames++;
            if (Time.time > targetTime && !resultPrinted)
            {
                Debug.LogWarningFormat("Reached iteration count: {0} in time: {1}. Total fps: {2}, Unity frames: {3},Unity fps: {4}", iterationCount, targetTime, iterationCount / targetTime, unityFrames, unityFrames / targetTime);
                resultPrinted = true;
            }
        }
    }
}