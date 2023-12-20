#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridMap))]
public class GridMapSOEditor : Editor
{
    public static int spacing = 8;
    public override void OnInspectorGUI()
    {
        GridMap gridMap = (GridMap)target;

        EditorGUI.BeginChangeCheck();

        gridMap.dimensions = EditorGUILayout.Vector2IntField("Grid Dimensions", gridMap.dimensions);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gridMap, "Grid Dimensions Change");
            gridMap.OnValidate();
        }

        EditorGUILayout.Space();

        GUILayout.Label("Grid Map");
        for (int y = gridMap.dimensions.y - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < gridMap.dimensions.x; x++)
            {
                bool newVal = EditorGUILayout.Toggle(gridMap[x, y], GUILayout.Width(12+spacing), GUILayout.Height(13+spacing)); // yes, box is not a square
                if (gridMap[x, y] != newVal)
                {
                    gridMap[x, y] = newVal;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif