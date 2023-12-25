using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CommandLineManager : MonoBehaviour
{
    public List<GridMap> gridMaps = new List<GridMap>();

    public void Process(ref GridMap selectedMap, ref float targetTime) {
        //SetGridMap
        int mapSize= 0;
        if (GetMapSize(ref mapSize)) {
            GridMap newSelectedMap = gridMaps.FirstOrDefault(map => map.dimensions.x == mapSize);
            if (selectedMap == null) {
                Debug.LogWarning("Map size " + mapSize + " not found in gridMaps. Using default map.");
            }
            else {
                Debug.Log("Map size " + mapSize + " found in gridMaps. Using it.");
                selectedMap = newSelectedMap;
            }
        }
        else {
            Debug.LogWarning("Map size not found in command line arguments. Using default map.");
        }
        
        //SetTargetTime
        if (GetTargetTime(ref targetTime)) {
            Debug.Log("Target time found in command line arguments. Using it.");
        }
        else {
            Debug.LogWarning("Target time not found in command line arguments. Using default target time.");
        }
    }
    
    private static bool GetTargetTime(ref float targetTime) {
        string targetArgName = "--targetTime";
        string argValue = GetArgValue(targetArgName);
        if (argValue == null) return false;
        return float.TryParse(argValue, out targetTime);
    }
    
    private static bool GetMapSize(ref int mapSize) {
        string targetArgName = "--mapSize";
        string argValue = GetArgValue(targetArgName);
        if (argValue == null) return false;
        return int.TryParse(argValue, out mapSize);
    }
    
    private static string GetArgValue(string targetArgName) {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Contains(targetArgName) && args[i].Contains("="))
                return args[i].Split('=')[1];
        }

        Debug.LogWarning("Argument " + targetArgName + " not found in command line arguments.");
        return null;
    }
}
