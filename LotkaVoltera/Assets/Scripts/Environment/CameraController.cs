using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainGeneration
{

    public class CameraController : MonoBehaviour
    {
        public TerrainGenerator generator;

        // Start is called before the first frame update
        void Start()
        {
            float halfWorldSize = generator.worldSize / 2;
            transform.position = new Vector3(halfWorldSize, 10, halfWorldSize);
            GetComponent<Camera>().orthographicSize = halfWorldSize;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}