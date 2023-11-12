using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameOfLife.Commons;
using UnityEngine.Serialization;

namespace GameOfLife.MasterScript
{
    public class GameSpawner : MonoBehaviour
    {
        public GameObject cellPrefab;
        public Vector2Int gridSize = new Vector2Int(32, 18);
        public float cellSize = 1f;
        public float cellSpacing = 0f;
        public GameOfLifeCell[,] grid;
        public bool[,] mapState;
        public int iterationCount = 100;

        void Start() {
            GetAttributesFromCommandLine();
            SpawnCells();
        }

        public void Update() {
            if (Time.frameCount == iterationCount)
            {
                Debug.LogWarningFormat("Reached iteration count: {0} in time: {1}", iterationCount, Time.time);
                //Application.Quit();
            }

            if (Time.frameCount > 2)
                UpdateLogic();
        }

        void GetAttributesFromCommandLine() {
            if (!CommandLineDataExtractor.GetIterationCount(ref iterationCount))
            {
                iterationCount = 100;
                Debug.LogWarning("Iteration count not found in command line arguments. Using default value: " +
                                 iterationCount);
            }

            if (!CommandLineDataExtractor.GetMap(ref mapState))
            {
                Debug.LogWarning("Map not found in command line arguments. Using default value.");
                GetMockData();
            }

            gridSize = new Vector2Int(mapState.GetLength(0), mapState.GetLength(1));
        }

        void GetMockData() {
            mapState = new bool[gridSize.x, gridSize.y];
            //Glider facing +x +y
            mapState[0, 2] = true;
            mapState[1, 2] = true;
            mapState[2, 2] = true;
            mapState[2, 1] = true;
            mapState[1, 0] = true;
        }

        private void SpawnCells() {
            grid = new GameOfLifeCell[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                grid[x, y] = SpawnCell(x, y, mapState[x, y]);
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                ConnectNeighbours(grid[x, y], x, y);
        }

        GameOfLifeCell SpawnCell(int x, int y, bool isAlive) {
            Vector3 position = new Vector3(x * (cellSize + cellSpacing), 0, y * (cellSize + cellSpacing));
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

        public void UpdateLogic() {
            bool[,] newMapState = new bool[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
            {
                GameOfLifeCell golc = grid[x, y];
                bool willBeAlive = GetNewStateForCell(golc);
                newMapState[x, y] = willBeAlive;
            }

            mapState = newMapState;
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                grid[x, y].SetState(mapState[x, y]);
        }

        public bool GetNewStateForCell(GameOfLifeCell golc) {
            List<bool> neighbours = golc.neighbours.Select(n => n.isAlive).ToList();
            int aliveNeighbours = neighbours.Count(n => n);
            bool willBeAlive = golc.isAlive ? aliveNeighbours is 2 or 3 : aliveNeighbours is 3;
            return willBeAlive;
        }
    }
}