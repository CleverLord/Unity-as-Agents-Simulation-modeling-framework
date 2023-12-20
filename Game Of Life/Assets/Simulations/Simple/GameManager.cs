using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameOfLife.Simple
{
    public class GameManager : MonoBehaviour
    {
        [Header("Simulation data")]
        public GridMap gridMap;
        public Vector2Int gridSize => gridMap.dimensions;
        private GameOfLifeCell[,] grid;
        private bool[,] mapState;
        
        [Header("Visuals")]
        public GameObject cellPrefab;
        void Start() {
            SpawnCells();
        }
        
        public void Update() {
            // To avoid sketchy time implementation, we will use the space bar to update the simulation
            if(Input.GetKeyDown(KeyCode.Space))
                UpdateLogic();
        }

        # region Initialization

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
            Vector3 position = new Vector3(x, 0, y);
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
        
        public void UpdateLogic() {
            bool[,] newMapState = new bool[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                newMapState[x, y] = GetNewStateForCell(grid[x, y]);

            mapState = newMapState;

            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                grid[x, y].SetState(mapState[x, y]);
        }
        public bool GetNewStateForCell(GameOfLifeCell golc) {
            List<bool> neighbours = golc.neighbours.Select(n => n.isAlive).ToList();
            int aliveNeighbours = neighbours.Count(n => n);
            return golc.isAlive ? aliveNeighbours is 2 or 3 : aliveNeighbours is 3;
        }
    }
}