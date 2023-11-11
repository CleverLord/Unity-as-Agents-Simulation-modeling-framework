using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace GameOfLife.Commons
{

    public static class CommandLineDataExtractor
    {
        //Main functions
        public static bool GetIterationCount(ref int iterationCount) {
            string targetArgName = "-iterationCount";
            string[] args = System.Environment.GetCommandLineArgs();
            string argValue = GetArgValue(args, targetArgName);
            if (argValue == null) return false;
            return int.TryParse(argValue, out iterationCount);
        }

        public static bool GetMap(ref bool[,] map) {
            string targetArgName = "-mapCSV";
            string[] args = System.Environment.GetCommandLineArgs();
            string argValue = GetArgValue(args, targetArgName);
            if (argValue == null) return false;
            return parseMap(ref map, argValue);
        }
        //Parsers
        private static bool parseMap(ref bool[,] map, string arg) {
            //get application run folder and add the path to the map file
            string mapPath = System.IO.Path.Combine(Application.dataPath, arg);
            //read the file
            string[] mapCSV = System.IO.File.ReadAllLines(mapPath);
            //parse the file
            map = new bool[mapCSV[0].Split(',').Length, mapCSV.Length];
            for (int y = 0; y < mapCSV.Length; y++) {
                string[] row = mapCSV[y].Split(',');
                for (int x = 0; x < row.Length; x++) {
                    map[x, y] = row[x] == "1";
                }
            }
            return true;
        }
        //Crude function
        [CanBeNull] private static string GetArgValue(string[] args, string targetArgName) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i].Contains(targetArgName) && args[i].Contains("="))
                    return args[i].Split('=')[1];
            }
            return null;
        }
    }
}