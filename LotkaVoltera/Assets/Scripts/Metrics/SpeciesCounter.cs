using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SpeciesCounter : MonoBehaviour
{
    [Range(1f, 50f)]
    // times a second data is read
    public float samplingRate = 2f;

    // current spiecies count
    private Dictionary<Species, int> lastSpeciesCount = new Dictionary<Species, int>();
    private List<Dictionary<Species, int>> speciesCountAgregator = new List<Dictionary<Species, int>>();

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
        lastSpeciesCount = Environment.speciesMaps
            .ToDictionary(entry => entry.Key, entry => entry.Value.numEntities);
        speciesCountAgregator.Add(lastSpeciesCount);
    }

    private void LogSpieciesCount()
    {
        // Print the results
        if (lastSpeciesCount == null)
            return;
        foreach (var entry in lastSpeciesCount)
        {
            Debug.Log($"{entry.Key}: {entry.Value} occurrences");
        }
    }

    // TODO: add different agregation methods: mean, median, min, max
    public Dictionary<Species, int> getAgregatedData()
    {
        // If any data in agregate buffor then agregate data
        if (speciesCountAgregator == null)
            return new Dictionary<Species, int>();        
        // Use LINQ to sum up the values for each species
        Dictionary<Species, int> agregatedData = speciesCountAgregator
            .SelectMany(dict => dict)
            .GroupBy(kvp => kvp.Key, kvp => kvp.Value)
            .ToDictionary(group => group.Key, group => group.Sum() / speciesCountAgregator.Count());
        // clear agregator buffor array
        speciesCountAgregator.Clear();
        // return agregated values
        return agregatedData;
    }
}
