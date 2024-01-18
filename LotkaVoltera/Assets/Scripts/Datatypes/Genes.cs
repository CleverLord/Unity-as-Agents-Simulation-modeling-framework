using System.Linq;
using UnityEngine;
using static System.Math;

public class Genes {

    const float mutationChance = .2f;
    const float maxMutationAmount = .3f;
    static readonly System.Random prng = new System.Random ();

    public readonly bool isMale;
    // genes
    // [0] - sexappeal
    public readonly float[] values;

    public Genes (float[] values) {
        isMale = RandomValue () < 0.5f;
        this.values = values;
    }

    public static Genes RandomGenes (int num) {
        float[] values = new float[num];
        for (int i = 0; i < num; i++) {
            values[i] = RandomValue ();
        }
        return new Genes (values);
    }

    public static Genes InheritedGenes (Genes mother, Genes father) {
        float[] values = new float[mother.values.Length];
        // TODO: implement inheritance
        Genes genes = new Genes (values);
        return genes;
    }

    public float GeneticDesirability ()
    {
        float resirablility = 0f;
        // TODO: genes can have different weights
        float singleGeneWeight = ((float)values.Length + 1f) / ((float)values.Length);
        for (int i = 0; i < values.Length; i++)
        {
            resirablility += values[i] * singleGeneWeight;
        }
        return resirablility;
    }

    static float RandomValue () {
        return (float) prng.NextDouble ();
    }

    static float RandomGaussian () {
        double u1 = 1 - prng.NextDouble ();
        double u2 = 1 - prng.NextDouble ();
        double randStdNormal = Sqrt (-2 * Log (u1)) * Sin (2 * PI * u2);
        return (float) randStdNormal;
    }
}