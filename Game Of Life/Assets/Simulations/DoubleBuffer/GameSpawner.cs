using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace GameOfLife.DoubleBuffer
{
    public class GameSpawner : MonoBehaviour
    {
        public GameObject cellPrefab;
        [FormerlySerializedAs("gridMap")]
        public InitialMapState initialMapState;
        public Vector2Int gridSize => initialMapState.dimensions;
        
        [Header("Visuals")]
        public GameOfLifeCell[,] grid;
        void Start() {
            if (initialMapState == null)
            {  
                Debug.LogError("InitialMapState not found in GameManager. Disabling GameManager.");
                this.gameObject.SetActive(false);
            }

            Stopwatch sw = Stopwatch.StartNew();
            SpawnCells();
            sw.Stop();
            UnityEngine.Debug.Log($"SpawnCells() took {sw.ElapsedMilliseconds}ms");
        }
        
        private void SpawnCells() {
            grid = new GameOfLifeCell[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                grid[x, y] = SpawnCell(x, y, initialMapState.Map[x, y]);
            
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                ConnectNeighbours(grid[x, y], x, y);
        }

        GameOfLifeCell SpawnCell(int x, int y,bool isAlive) {
            Vector3 position = new Vector3(x, 0, y);
            GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
            GameOfLifeCell golcell = cell.GetComponent<GameOfLifeCell>();
            golcell.SetInitialState(isAlive);
            return golcell;
        }

        void ConnectNeighbours(GameOfLifeCell golc, int x, int y) {
            List<GameOfLifeCell> neighbours = new List<GameOfLifeCell>();
            for (int x1 = x - 1; x1 <= x + 1; x1++)
            for (int y1 = y - 1; y1 <= y + 1; y1++)
                if (x1 >= 0 && x1 < gridSize.x && y1 >= 0 && y1 < gridSize.y && !(x1 == x && y1 == y))
                    neighbours.Add(grid[x1, y1]);
            golc.neighbours = neighbours;
        }
        
    }
}