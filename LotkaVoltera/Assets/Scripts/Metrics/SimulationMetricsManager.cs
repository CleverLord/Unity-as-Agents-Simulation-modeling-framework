using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class SimulationMetricsManager : MonoBehaviour
{
    // File path for the metrics files
    [ReadOnlyWhenPlaying]
    public string metricsFolder = "/Metrics";
    
    // File path for the metrics CSV files
    [ReadOnlyWhenPlaying]
    public string csvFilesPath = "/csv";
    [Range((1 / 50f), 50f)]
    // times a second data is agregated and written to file
    public float writeRate = 1 / 5f;
    // Link apropriate species counter script in Unity UI
    public SpeciesCounter speciesCounter;
    // Link apropriate CSV file writer in Unity UI
    public CSVWriter speciesCountCSVWriter;

    // Start is called before the first frame update
    void Start()
    {
        // start writing data to file
        StartCoroutine(WriteMetricsDataToFiles());
    }

    IEnumerator WriteMetricsDataToFiles()
    {
        while (true)
        {
            // get curr game time
            float currElapsedTime = Time.time;
            // get agregated species count data and
            Dictionary<Species, int> data = speciesCounter.getAggregatedData();
            // write to file if there is any data to be written
            if (data.Count != 0)
            {
                string[] csvReadyData = speciesCountToCSV(currElapsedTime, data);

                
                // write data to csv file
                speciesCountCSVWriter.writeData(csvReadyData);
            }
            // wait until next data write time
            yield return new WaitForSeconds(1 / writeRate);
        }
    }

    // Convert key value pairs to csv file string data lines
    private string[] speciesCountToCSV(float time, Dictionary<Species, int> speciesCounts)
    {
        // Format the float value as a string with period as decimal separator
        string[] dataLines = speciesCounts
        .Select(entry => $"{time.ToString(CultureInfo.InvariantCulture)},{entry.Key.ToString()},{entry.Value.ToString()}")
        .ToArray();

        return dataLines;
    }
}
