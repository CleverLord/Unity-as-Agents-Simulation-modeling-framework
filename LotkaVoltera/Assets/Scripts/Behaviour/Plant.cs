using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Plant : LivingEntity {
    // TODO: add functionality after merging task-16 into Lotka-Volterra
    //[ReadOnly]
    public float amountRemaining = 1;
    const float consumeSpeed = 8;
    [Range(1, 100)]
    public float growDuration = 15; // Time in seconds for regrowth
    [Range(1, 100)]
    public float growthDelayAfterConsumption = 10; // Time in seconds to wait after consumption before starting to regrow

    private float growStartTime;
    private float growAmmount;

    private void Update()
    {
        // if consumption happened earlier than growth delay and
        // plant is alive and is not already fully grown
        if (growStartTime < Time.time && amountRemaining > 0 && amountRemaining < 1)
        {
            Grow();
        }
    }

    private void Grow()
    {
        Debug.Log($"Growing");

        amountRemaining += Time.deltaTime * (growAmmount / growDuration); // fraction by which the plant should grow
        amountRemaining = Mathf.Clamp01(amountRemaining);
        transform.localScale = Vector3.one * amountRemaining; // adjust scale to curr size
    }

    public float Consume (float amount) {
        float amountConsumed = Mathf.Max (0, Mathf.Min (amountRemaining, amount));
        amountRemaining -= amount * consumeSpeed;

        // Register last consumption
        growStartTime = Time.time + growthDelayAfterConsumption;
        growAmmount = 1 - amountRemaining;

        transform.localScale = Vector3.one * amountRemaining;


        if (amountRemaining <= 0) {
            Die (CauseOfDeath.Eaten);
        }

        return amountConsumed;
    }

    public float AmountRemaining {
        get {
            return amountRemaining;
        }
    }
}