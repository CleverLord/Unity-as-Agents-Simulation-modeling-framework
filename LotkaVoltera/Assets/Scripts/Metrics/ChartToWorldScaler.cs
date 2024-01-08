using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainGeneration
{
    public class ChartToWorldScaler : MonoBehaviour
    {
        public TerrainGenerator terrGen;
        public GameObject terrMesh;
        public GameObject viewPort;

        [ReadOnlyWhenPlaying, Range(-100, 100)]
        public float OffsetX = 0;
        [ReadOnlyWhenPlaying, Range(-100, 100)]
        public float OffsetY = 2;


        // Start is called before the first frame update
        void Start()
        {

        }

        public void scaleViewportToWorld()
        {
            if (terrGen == null || viewPort == null)
                return;
            float viewPortHeightToWidthRatio = viewPort.transform.localScale.y / viewPort.transform.localScale.x;
            float worldSize = (float)terrGen.worldSize;
            // set correct
            viewPort.transform.localScale = new Vector3(
                worldSize,
                worldSize * viewPortHeightToWidthRatio,
                viewPort.transform.localScale.z
            );
            // move chart object behind the simulation world
            gameObject.transform.position = new Vector3(
                terrMesh.gameObject.transform.position.x + OffsetX,
                terrMesh.gameObject.transform.position.y + OffsetY,
                terrMesh.gameObject.transform.position.z + worldSize
            );
        }
    }
}
