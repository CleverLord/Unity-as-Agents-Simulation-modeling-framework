using UnityEngine;

namespace Traditional
{
    public class GameSpawner : MonoBehaviour
    {
        public GameObject cellPrefab;
        public Vector2Int gridSize = new Vector2Int(32, 18);
        public float cellSize = 1f;
        public float cellSpacing = 0.1f;
        public GameOfLifeCell[,] grid;
        
        void Start() {
            grid = new GameOfLifeCell[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                grid[x, y] = SpawnCell(x, y);
            for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                SetNeighbors(x, y);
        }
        
        GameOfLifeCell SpawnCell(int x, int y) {
            Vector3 position = new Vector3(x * (cellSize + cellSpacing), 0, y * (cellSize + cellSpacing));
            GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
            return cell.GetComponent<GameOfLifeCell>();
        }
        
        void SetNeighbors(int x, int y) {
            GameOfLifeCell cell = grid[x, y];
            if (x > 0) cell.neighbors.Add(Direction.Left, grid[x - 1, y]);
            if (x < gridSize.x - 1) cell.neighbors.Add(Direction.Right, grid[x + 1, y]);
            if (y > 0) cell.neighbors.Add(Direction.Bottom, grid[x, y - 1]);
            if (y < gridSize.y - 1) cell.neighbors.Add(Direction.Top, grid[x, y + 1]);
        }
    }
}