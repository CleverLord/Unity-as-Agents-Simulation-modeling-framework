#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InitialMapState))]
public class GridMapSOEditor : Editor
{
    public static int spacing = 8;
    public static int maxDisplaySize = 25;

    private Vector2 scrollPosition;

    public override void OnInspectorGUI()
    {
        InitialMapState initialMapState = (InitialMapState)target;

        EditorGUI.BeginChangeCheck();
        
        Vector2Int newDimensions = EditorGUILayout.Vector2IntField("Grid Dimensions", initialMapState.dimensions);
        if (newDimensions != initialMapState.dimensions)
        {
            initialMapState.dimensions = newDimensions;
            EditorUtility.SetDirty(initialMapState);
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(initialMapState, "Grid Dimensions Change");
            initialMapState.OnValidate();
        }

        EditorGUILayout.Space();

        GUILayout.Label("Grid Map");

        // Begin a horizontal scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        for (int y = Mathf.Min(initialMapState.dimensions.y - 1, maxDisplaySize); y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < initialMapState.dimensions.x && x < maxDisplaySize; x++)
            {
                bool newVal = EditorGUILayout.Toggle(initialMapState.Map[x, y], GUILayout.Width(12 + spacing), GUILayout.Height(13 + spacing)); // yes, box is not a square
                if (initialMapState.Map[x, y] != newVal)
                {
                    initialMapState.Map[x, y] = newVal;
                    EditorUtility.SetDirty(initialMapState);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // End the horizontal scroll view
        EditorGUILayout.EndScrollView();
    }
}
#endif