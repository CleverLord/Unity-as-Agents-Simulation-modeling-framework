using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using GameOfLife.Commons;
using UnityEngine.Serialization;

namespace GameOfLife.PureCpu
{
    public class GameOfLife : MonoBehaviour
    {
        public Vector2Int gridSize = new Vector2Int(32, 18);
        public GameOfLifeCell[,] grid;
        public List<GameOfLifeCell> gridList = new List<GameOfLifeCell>();
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

            UpdateLogic();
        }

        void GetAttributesFromCommandLine() {
            if (!CommandLineDataExtractor.GetIterationCount(ref iterationCount))
            {
                iterationCount = Defaults.Instance.iterationCount;
                Debug.LogWarning("Iteration count not found in command line arguments. Using default value: " +
                                 iterationCount);
            }

            if (!CommandLineDataExtractor.GetMap(ref mapState))
            {
                Debug.LogWarning("Map not found in command line arguments. Using default value.");
                gridSize = Defaults.Instance.gridSize;
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
            {
                grid[x, y] = SpawnCell(x, y, mapState[x, y]);
                gridList.Add(grid[x, y]);
            }

            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                ConnectNeighbours(grid[x, y], x, y);
        }

        GameOfLifeCell SpawnCell(int x, int y, bool isAlive) {
            GameOfLifeCell golcell = new GameOfLifeCell();
            golcell.isAlive = isAlive;
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

        void ParalelForeachTask(GameOfLifeCell golc) {
            bool willBeAlive = GetNewStateForCell(golc);
            newMapState[golc.positionInGrid.x, golc.positionInGrid.y] = willBeAlive;
        }

        private volatile bool[,] newMapState;
 
        void ParallelForeachTask2(GameOfLifeCell golc) {
            golc.isAlive = mapState[golc.positionInGrid.x, golc.positionInGrid.y];
        }
        public void UpdateLogic() {
            newMapState = new bool[gridSize.x, gridSize.y];
            Parallel.ForEach(gridList, ParalelForeachTask);
            mapState = newMapState;
            Parallel.ForEach(gridList, ParallelForeachTask2);
        }
        
        public bool GetNewStateForCell(GameOfLifeCell golc) {
            //int aliveNeighbours = golc.neighbours.Count(neighbour => neighbour.isAlive);
            int aliveNeighbours = 0;
            for (int i = 0; i < golc.neighbours.Count; i++)
            {
                if (golc.neighbours[i].isAlive)
                {
                    aliveNeighbours++;
                }
            }
            bool willBeAlive = golc.isAlive ? aliveNeighbours is 2 or 3 : aliveNeighbours is 3;
            return willBeAlive;
        }
    }
}