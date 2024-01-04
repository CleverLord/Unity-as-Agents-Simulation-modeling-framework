using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SpeciesCounter : MonoBehaviour
{
    [Range((1/50f), 50f)]
    // times a second data is read
    public float samplingRate = 1/5f;

    // current spiecies count
    private Dictionary<Species, int> speciesCount;

    private void Start()
    {
        StartCoroutine(SampleData());
    }

    IEnumerator SampleData()
    {
        while (true)
        {
            CountSpiecies();
            LogSpieciesCount();
            yield return new WaitForSeconds(1 / samplingRate);
        }
    }

    // Function to count spiecies in environment
    private void CountSpiecies()
    {
        // Count occurrences of each species
        if (Environment.speciesMaps == null)
            return; 
        speciesCount = Environment.speciesMaps
            //.GroupBy(entry => entry.Key)
            .ToDictionary(entry => entry.Key, entry => entry.Value.numEntities);
    }

    private void LogSpieciesCount()
    {
        // Print the results
        if (speciesCount == null)
            return;
        foreach (var entry in speciesCount)
        {
            Debug.Log($"{entry.Key}: {entry.Value} occurrences");
        }
    }
}
