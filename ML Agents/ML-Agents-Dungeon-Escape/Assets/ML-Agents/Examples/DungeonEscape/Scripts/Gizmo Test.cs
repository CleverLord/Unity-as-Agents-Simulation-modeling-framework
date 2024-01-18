using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoTest : MonoBehaviour
{
    public Vector3 cubeSize = new Vector3(1,0.1f,1);
    public int gridSize = 10;
    public int scale = 1;
    public float maxDistance = 10f;
    public Color color = Color.white;
    
    public AnimationCurve krzywa=AnimationCurve.Linear(0,0,1,1);
    void OnDrawGizmos() {
        Gizmos.color = color;
        Vector3 center = transform.position;
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                //alpha proportional to the distance
                float alpha = Mathf.Clamp01(1.0f - Vector3.Distance(center, new Vector3(x * scale, 0, z * scale) + center) / maxDistance);
                alpha = krzywa.Evaluate(alpha);
                Gizmos.color = new Color(color.r, color.g, color.b, alpha);
                Gizmos.DrawCube(new Vector3(x * scale, 0, z * scale) + center, cubeSize);
            }
        }
    }
}