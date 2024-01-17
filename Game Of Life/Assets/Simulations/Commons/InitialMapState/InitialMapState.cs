using UnityEngine;

[CreateAssetMenu(fileName = "InitialMapState", menuName = "GameOfLife/InitialMapState")]
public class InitialMapState : ScriptableObject
{
    [Header("Grid Dimensions")]
    public Vector2Int dimensions = new Vector2Int(5, 5);

    [Header("Grid Map")]
    private bool[,] _map;
    public bool[,] Map
    {
        get
        {
            Sync2DMap();
            return _map;
        }
        set
        {
            _map = value;
            SyncFlatMap();
        }
    }
    
    public bool[,] MapForVisualScripting => Map; // getter-setter's are not recognized by visual scripting, but properties are
    public void SyncMapForVisualScripting() => Sync2DMap(true);
    
    [HideInInspector] public bool[] flatMap; // 1D representation of the map
    //private bool isDirty = true; // Flag to indicate if 2D map needs to be synced with 1D map

    public void OnValidate() {
        // Ensure dimensions are positive
        dimensions.x = Mathf.Max(1, dimensions.x);
        dimensions.y = Mathf.Max(1, dimensions.y);

        // Create a new map array with updated dimensions
        bool[,] newMap = new bool[dimensions.x, dimensions.y];

        if (_map == null)
            Sync2DMap();
        
        // Copy existing values to the new map within the valid range
        for (int x = 0; x < Mathf.Min(_map.GetLength(0), dimensions.x); x++)
        {
            for (int y = 0; y < Mathf.Min(_map.GetLength(1), dimensions.y); y++)
            {
                newMap[x, y] = _map[x, y];
            }
        }

        _map = newMap;
    }

    // Sync the 1D array with the 2D array
    private void SyncFlatMap() {
        dimensions= new Vector2Int(_map.GetLength(0), _map.GetLength(1));
        flatMap = new bool[dimensions.x * dimensions.y];
        if (_map != null)
        {
            for (int x = 0; x < dimensions.x; x++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    flatMap[x + y * dimensions.x] = _map[x, y];
                }
            }
        }
    }

    private void Sync2DMap(bool forceUpdate = false) {
        if (!(forceUpdate || _map == null))
            return;

        _map = new bool[dimensions.x, dimensions.y];
        if (flatMap != null)
        {
            for (int x = 0; x < dimensions.x; x++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    _map[x, y] = flatMap[x + y * dimensions.x];
                }
            }
        }
    }
}