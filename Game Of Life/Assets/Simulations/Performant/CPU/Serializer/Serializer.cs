using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace GameOfLife.Performant.CPU
{
    public enum SerializedType
    {
        Json,
        DictionaryLike,
        DictionaryLikeOnlyAlive
    }

    public static class Serializer
    {
        public static void Serialize(List<GameOfLifeCell> cells, SerializedType serializedType) {
            switch (serializedType)
            {
                case SerializedType.Json:
                    SaveJson(cells);
                    break;
                case SerializedType.DictionaryLike:
                    SaveDictionaryLike(cells);
                    break;
                case SerializedType.DictionaryLikeOnlyAlive:
                    SaveDictionaryLikeOnlyAlive(cells);
                    break;
            }
        }

        public static void SerializeAll(List<GameOfLifeCell> cells, bool printToTheConsole = false) {
            SaveJson(cells, printToTheConsole);
            SaveDictionaryLike(cells,printToTheConsole);
            SaveDictionaryLikeOnlyAlive(cells,printToTheConsole);
        }
        
        [Serializable]
        public class CellsWrapper
        {
            public List<GameOfLifeSerializedCell> cells;
        }
        [Serializable]
        public class GameOfLifeSerializedCell
        {
            public Vector2Int positionInGrid;
            public bool isAlive;
        }
        
        public static void SaveJson(List<GameOfLifeCell> cells, bool printToTheConsole = false) {
            string json = JsonUtility.ToJson(new CellsWrapper(){cells = cells.ConvertAll(cell => new GameOfLifeSerializedCell(){positionInGrid = cell.positionInGrid, isAlive = cell.isAlive})}, true);
            string filename = GetFileName("Json", "json");
            File.WriteAllText(Application.dataPath + "/" + filename, json);
            if (printToTheConsole)
                Debug.Log("\nJson:\n" + json);
        }

        public static void SaveDictionaryLike(List<GameOfLifeCell> cells, bool printToTheConsole = false) {
            string filename = GetFileName("DictionaryLike", "txt");
            using (var writer = new StreamWriter(Application.dataPath + "/" + filename))
            {
                //Save cells
                writer.WriteLine("x,y,isAlive");
                foreach (var cell in cells)
                {
                    writer.WriteLine(cell.positionInGrid.x + "," + cell.positionInGrid.y + "," + cell.isAlive);
                }
            }
            if (printToTheConsole)
                Debug.Log("\nDictionaryLike:\n" + File.ReadAllText(Application.dataPath + "/" + filename));
        }

        public static void SaveDictionaryLikeOnlyAlive(List<GameOfLifeCell> cells, bool printToTheConsole = false) {
            string filename = GetFileName("DictionaryOnlyAlive", "csv");
            using (var writer = new StreamWriter(Application.dataPath + "/" + filename))
            {
                writer.WriteLine("x,y");
                foreach (var cell in cells)
                {
                    if (cell.isAlive)
                        writer.WriteLine(cell.positionInGrid.x + "," + cell.positionInGrid.y);
                }
            }
            if (printToTheConsole)
                Debug.Log("\nDictionaryOnlyAlive:\n" + File.ReadAllText(Application.dataPath + "/" + filename) + "\n");
        }
        static string GetFileName(string prefix, string extension) {
            return prefix + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "." + extension;
        }
    }
}