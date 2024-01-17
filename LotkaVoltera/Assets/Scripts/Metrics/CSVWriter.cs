using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CSVWriter : MonoBehaviour
{
    // reference to metrics manager
    public SimulationMetricsManager simMetricsMngr;

    // output file full path
    // TODO: field should be selectable, scrollable, but non editable
    public string fullFilePath;
    // File name base for the species count over time metric
    [ReadOnlyWhenPlaying]
    public string csvFileBaseName = GetSpeciesCountFileName();
    // File name suffix for the scpecies count over time metric
    [ReadOnlyWhenPlaying]
    public string csvFileNameSufix = "species_count_over_time";


    // Data to be written to the CSV file
    [ReadOnlyWhenPlaying]
    public string[] header = { };

    private void Start()
    {
        // full path to file
        fullFilePath = GetFullFilePath();
        // set default file name
        csvFileBaseName = GetSpeciesCountFileName();
    }

    // write data to csv file
    public void writeData(string[] data)
    {
        // Create the CSV file if it does not exist
        if (!File.Exists(fullFilePath))
        {
            CreateCSVFile();
        }

        // Append the new data to the CSV file
        AppendToCSV(data);
    }

    // Method to append data to the CSV file
    void AppendToCSV(string[] data)
    {
        // Append the data to the CSV file
        using (StreamWriter sw = new StreamWriter(fullFilePath, true))
        {
            // Write the data
            sw.WriteLine(string.Join(System.Environment.NewLine, data));
        }
    }

    // Method to create the CSV file if it does not exist
    void CreateCSVFile()
    {   
        // If the file does not exist, create the necessary folders
        string directoryPath = Path.GetDirectoryName(fullFilePath);

        // Create required directories and log information in debug console
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            Debug.Log($"Folders created at: {directoryPath}");
        }

        using (StreamWriter sw = new StreamWriter(fullFilePath, true))
        {
            // Write the header
            sw.WriteLine(string.Join(",", header));
        }
    }

    private static string GetSpeciesCountFileName()
    {
        return "Simulation_start_at_" + System.DateTime.Now.ToString("yyyy_MM_dd__hh_mm_ss");
    }

    private string GetFullFileName()
    {
        return csvFileBaseName + "__" + csvFileNameSufix;
    }

    private string GetFullFilePath()
    {
        return Application.dataPath +
            simMetricsMngr.metricsFolder + 
            simMetricsMngr.csvFilesPath + "/" 
            + GetFullFileName() + ".csv";
    }
}
