using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameOfLife.Performant
{
    public enum UpdateLogicMethod
    {
        Linq,
        NoLinq,
    }
    public class GameManagerCpu : MonoBehaviour
    {
        public GridMap gridMap;
        public Vector2Int gridSize => gridMap.dimensions;
        public GameObject cellPrefab;

        private GameOfLifeCell[,] grid;
        private List<GameOfLifeCell> gridList = new List<GameOfLifeCell>();
        public bool[,] mapState;
        public int iterationCount = 0;
        public float targetTime = 10;
        
        public UpdateLogicMethod updateLogicMethod = UpdateLogicMethod.Linq;
        private bool initializedCorrectly = false;

        //This is this way, so that we can connect all the assets in the inspector
        private CommandLineManager CommandLineManager;
        
        void Start() {
            CommandLineManager = GetComponent<CommandLineManager>();
            CommandLineManager.Process(ref gridMap, ref targetTime);
            Stopwatch sw = Stopwatch.StartNew();
            SpawnCells();
            sw.Stop();
            Debug.LogFormat("Spawned {0} cells in {1} ms", gridSize.x * gridSize.y, sw.ElapsedMilliseconds);
            initializedCorrectly = true;
        }
        
        bool resultPrinted = false;
        public void Update() {
            if (!initializedCorrectly)
                return;
            if (Time.time>targetTime && !resultPrinted)
            {
                Debug.LogWarningFormat("Reached iteration count: {0} in time: {1}. Total fps: {2}", iterationCount, targetTime, iterationCount/targetTime);
                //This part is for docker container, since it cannot be closed by pressing the stop button
#if !UNITY_EDITOR
                Serializer.SerializeAll(gridList,true);
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
                grid[x, y] = SpawnCell(x, y, gridMap[x, y]);
                gridList.Add(grid[x, y]);
            }

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

        private bool[,] newMapState;
        public void UpdateLogic() {
            newMapState = new bool[gridSize.x, gridSize.y];
            
            if (updateLogicMethod == UpdateLogicMethod.Linq)
                Parallel.ForEach(gridList, UpdateLogicParallelOperation);
            else
                Parallel.ForEach(gridList, UpdateLogicParallelOperation_NoLinq);
            
            mapState = newMapState;

            //Following code cannot be accelerated, since visuals must be updated on main thread
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                grid[x, y].SetState(mapState[x, y]);
            //We could remove it by replacing it with auto-update through Update() function on the cell itself
            //But that would run on the single thread anyway, and what's more, it would run even if there was no change in the cell's state
            
            iterationCount++;
        }
        public void UpdateLogicParallelOperation(GameOfLifeCell golc) {
            newMapState[golc.positionInGrid.x, golc.positionInGrid.y] = GetNewStateForCell(golc);
        }
        public void UpdateLogicParallelOperation_NoLinq(GameOfLifeCell golc) {
            newMapState[golc.positionInGrid.x, golc.positionInGrid.y] = GetNewStateForCell_NoLinq(golc);
        }

        //This function is the most simple and readable, but it's also the slowest
        public bool GetNewStateForCell(GameOfLifeCell golc) {
            int aliveNeighbours = golc.neighbours.Select(n => n.isAlive).Count(n => n);
            return golc.isAlive ? aliveNeighbours is 2 or 3 : aliveNeighbours is 3;
        }
        //This one on the other hand does not use System.Linq, and that makes it faster
        //foreach (C#) is simmilar in performaance to for, and they both are faster than List.ForEach
        public bool GetNewStateForCell_NoLinq(GameOfLifeCell golc) {
            int aliveNeighbours = 0;
            foreach (GameOfLifeCell neighbour in golc.neighbours)
                if (neighbour.isAlive)
                    aliveNeighbours++;
            //We won't replace following expression with a typical if, because we believe that the compiler will do that internally
            return golc.isAlive ? aliveNeighbours is 2 or 3 : aliveNeighbours is 3; 
        }
    }
}