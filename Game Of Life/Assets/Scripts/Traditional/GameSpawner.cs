using UnityEngine;
using GameOfLife.Commons;
using UnityEngine.Serialization;

namespace GameOfLife.Traditional
{
    public class GameSpawner : MonoBehaviour
    {
        public GameObject cellPrefab;
        public Vector2Int gridSize = new Vector2Int(32, 18);
        public float cellSize = 1f;
        public float cellSpacing = 0f;
        public GameOfLifeCell[,] grid;
        public bool[,] mapInitialState;
        public int iterationCount = 100;
        void Start() {
            GetAttributesFromCommandLine();
            SpawnCells();
        }

        void GetAttributesFromCommandLine() {
            if (!CommandLineDataExtractor.GetIterationCount(ref iterationCount))
            {
                iterationCount = 100;
                Debug.LogWarning("Iteration count not found in command line arguments. Using default value: " + iterationCount);
            }
            if(!CommandLineDataExtractor.GetMap(ref mapInitialState))
            {
                Debug.LogWarning("Map not found in command line arguments. Using default value.");
                GetMockData();
            }
            gridSize = new Vector2Int(mapInitialState.GetLength(0), mapInitialState.GetLength(1));
        }

        void GetMockData() {
            mapInitialState = new bool[gridSize.x, gridSize.y];
            //Glider facing +x +y
            mapInitialState[0,2] = true;
            mapInitialState[1,2] = true;
            mapInitialState[2,2] = true;
            mapInitialState[2,1] = true;
            mapInitialState[1,0] = true;
            
        }

        private void SpawnCells() {
            grid = new GameOfLifeCell[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                grid[x, y] = SpawnCell(x, y, mapInitialState[x, y]);
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