using UnityEngine;
using GameOfLife.Commons;
using UnityEngine.Serialization;

namespace GameOfLife.DoubleBuffer
{
    public class GameSpawner : MonoBehaviour
    {
        public GameObject cellPrefab;
        public GridMap gridMap;
        public int iterationCount = 100;
        
        [Header("Visuals")]
        public float cellSize = 1f;
        public float cellSpacing = 0f;
        public GameOfLifeCell[,] grid;
        public Vector2Int gridSize => gridMap.dimensions;
        void Start() {
            if (gridMap == null)
            {  
                Debug.LogError("GridMap not found in GameSpawner. Disabling GameSpawner.");
                this.gameObject.SetActive(false);
            }
            SpawnCells();
        }
        public void Update() {
            if (Time.frameCount == iterationCount)
            {
                Debug.LogWarningFormat("Reached iteration count: {0} in time: {1}", iterationCount, Time.time);
                //Application.Quit();
            }
        }
        
        private void SpawnCells() {
            grid = new GameOfLifeCell[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                grid[x, y] = SpawnCell(x, y, gridMap[x, y]);
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                SetNeighbors(x, y);
        }

        GameOfLifeCell SpawnCell(int x, int y,bool isAlive) {
            Vector3 position = new Vector3(x * (cellSize + cellSpacing), 0, y * (cellSize + cellSpacing));
            GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
            GameOfLifeCell golcell= cell.GetComponent<GameOfLifeCell>();
            golcell.SetInitialState(isAlive);
            return golcell;
        }

        void SetNeighbors(int x, int y) {
            GameOfLifeCell cell = grid[x, y];
            //go through all 8 neighbors
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++) {
                    //skip self
                    if (i == 0 && j == 0)
                        continue;
                    //skip out of bounds
                    if (x + i < 0 || x + i >= gridSize.x || y + j < 0 || y + j >= gridSize.y)
                        continue;
                    cell.neighbors.Add(grid[x + i, y + j]);
                }
        }
    }
}