#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridMap))]
public class GridMapSOEditor : Editor
{
    public static int spacing = 8;
    public static int maxDisplaySize = 25;

    private Vector2 scrollPosition;

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

        // Begin a horizontal scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        for (int y = Mathf.Min(gridMap.dimensions.y - 1, maxDisplaySize); y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < gridMap.dimensions.x && x < maxDisplaySize; x++)
            {
                bool newVal = EditorGUILayout.Toggle(gridMap[x, y], GUILayout.Width(12 + spacing), GUILayout.Height(13 + spacing)); // yes, box is not a square
                if (gridMap[x, y] != newVal)
                {
                    gridMap[x, y] = newVal;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // End the horizontal scroll view
        EditorGUILayout.EndScrollView();
    }
}
#endif