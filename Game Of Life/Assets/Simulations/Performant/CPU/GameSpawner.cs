using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using GameOfLife.Commons;
using UnityEngine.Serialization;

namespace GameOfLife.Performant
{
    public class GameSpawner : MonoBehaviour
    {
        public GridMap gridMap;
        public Vector2Int gridSize => gridMap.dimensions;
        public GameObject cellPrefab;
        public float cellSize = 1f;
        public float cellSpacing = 0f;
        public GameOfLifeCell[,] grid;
        public List<GameOfLifeCell> gridList;
        public bool[,] mapState;
        public int iterationCount = 100;

        void Start() { SpawnCells(); }

        public void Update() {
            if (Time.frameCount == iterationCount)
            {
                Debug.LogWarningFormat("Reached iteration count: {0} in time: {1}", iterationCount, Time.time);
#if !UNITY_EDITOR
                    Application.Quit();
#endif
            }
            UpdateLogic();
        }

        # region Initialization

        private void SpawnCells() {
            grid = new GameOfLifeCell[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
            {
                grid[x, y] = SpawnCell(x, y, mapState[x, y]);
                gridList.Add(grid[x, y]);
            }

            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                ConnectNeighbours(grid[x, y], x, y);
        }

        GameOfLifeCell SpawnCell(int x, int y, bool isAlive) {
            Vector3 position = new Vector3(x * (cellSize + cellSpacing), 0, y * (cellSize + cellSpacing));
            GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
            GameOfLifeCell golcell = cell.GetComponent<GameOfLifeCell>();
            golcell.SetInitialState(isAlive);
            golcell.positionInGrid = new Vector2Int(x, y);
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

        #endregion

        private bool[,] newMapState;
        public void UpdateLogic() {
            newMapState = new bool[gridSize.x, gridSize.y];
            Parallel.ForEach(gridList, UpdateLogicParallelOperation);
            mapState = newMapState;

            //Following code cannot be accelerated,since visuals must be updated on main thread
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                grid[x, y].SetState(mapState[x, y]);
        }
        public void UpdateLogicParallelOperation(GameOfLifeCell golc) {
            newMapState[golc.positionInGrid.x, golc.positionInGrid.y] = GetNewStateForCell(golc);
        }
        //This function is the most simple and readable, but it's also the slowest
        public bool GetNewStateForCell(GameOfLifeCell golc) {
            List<bool> neighbours = golc.neighbours.Select(n => n.isAlive).ToList();
            int aliveNeighbours = neighbours.Count(n => n);
            return golc.isAlive ? aliveNeighbours is 2 or 3 : aliveNeighbours is 3;
        }
        //This one on the other hand does not use System.Linq, and that makes it faster
        //foreach (C#) is simmilar in performaance to for, and they both are faster than List.ForEach
        public bool GetNewStateForCell_V2(GameOfLifeCell golc) {
            int aliveNeighbours = 0;
            foreach (GameOfLifeCell neighbour in golc.neighbours)
                if (neighbour.isAlive)
                    aliveNeighbours++;
            //We won't replace following expression with a typical if, because we believe that the compiler will do that internally
            return golc.isAlive ? aliveNeighbours is 2 or 3 : aliveNeighbours is 3; 
        }
    }
}