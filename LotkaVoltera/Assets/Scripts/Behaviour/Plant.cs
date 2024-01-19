using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[SelectionBase]
public class Plant : LivingEntity {

    [ReadOnly]
    public float amountRemaining = 1;
    const float consumeSpeed = 8;
    [Range(1, 100)]
    public float regrowDuration = 15; // Time in seconds for regrowth
    [Range(1, 100)]
    public float regrowthDelayAfterConsumption = 10; // Time in seconds to wait after consumption before starting to regrow
    private float regrowStartTime;
    private float regrowAmmount;

    // new plant grow duration
    [Range(1, 100)]
    public float growDuration = 20;
    // new plant grow delay
    [Range(1, 100)]
    public float growDelay = 2;

    // reproductive threshold
    [Range(0.1f, 1)]
    public float minReproductiveMaturity = 0.6f;
    // reproduction delay
    [Range(1, 100)]
    public float reproductionDelay = 5;
    private float reproductionStartTime;

    private float growStartTime;
    private float growAmmount;

    private void Start()
    {
        // Plant config
        // set plant game object scale before displaying it on screen for the first time
        transform.localScale = Vector3.one * amountRemaining;
        growStartTime = Time.time + growDelay;
    }

    private void Update()
    {
        // Try to reproduce
        if (amountRemaining >= minReproductiveMaturity &&
            Time.time > reproductionStartTime)
        {
            // Try to reproduce when conditions for reprodustion are met
            reproduce(this);
            // Reset reproduction start time for next reproduction attempt 
            reproductionStartTime = Time.time + reproductionDelay;
        }

        // Grow only if in time period for growth
        // and regrowing has not started after growing has started
        // and not fully grow yet
        if (Time.time > growStartTime && Time.time < growStartTime + growDuration &&
            regrowStartTime < growStartTime &&
            amountRemaining > 0 && amountRemaining < 1)
        {
            Grow();
        }
        // if consumption happened earlier than regrowth delay and
        // plant is alive and is not already fully regrown
        if (regrowStartTime < Time.time && amountRemaining > 0 && amountRemaining < 1)
        {
            Regrow();
        }
    }

    private void Grow ()
    {
        amountRemaining += growAmmount * (Time.deltaTime / growDuration); // fraction by which the plant should grow
        amountRemaining = Mathf.Clamp01(amountRemaining);
        transform.localScale = Vector3.one * amountRemaining; // adjust scale to curr size
    }

    private void Regrow()
    {
        amountRemaining += Time.deltaTime * (regrowAmmount / regrowDuration); // fraction by which the plant should grow
        amountRemaining = Mathf.Clamp01(amountRemaining);
        transform.localScale = Vector3.one * amountRemaining; // adjust scale to curr size
    }

    public float Consume (float amount) {
        float amountConsumed = Mathf.Max (0, Mathf.Min (amountRemaining, amount));
        amountRemaining -= amount * consumeSpeed;

        // Register last consumption
        regrowStartTime = Time.time + regrowthDelayAfterConsumption;
        regrowAmmount = 1 - amountRemaining;

        transform.localScale = Vector3.one * amountRemaining;


        if (amountRemaining <= 0) {
            Die (CauseOfDeath.Eaten);
        }

        return amountConsumed;
    }

    public override LivingEntity GetOffspring()
    {
        Plant offspring = Instantiate(this.gameObject, Environment.speciesHolders[species]).GetComponent<Plant>();
        offspring.amountRemaining = 0.2f;
        // scale game object to apropriate size
        offspring.transform.localScale = Vector3.one * amountRemaining;
        offspring.growAmmount = 1f - offspring.amountRemaining;
        offspring.reproduce = reproduce;
        return offspring;
    }

    public float AmountRemaining {
        get {
            return amountRemaining;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // draw gizmos for tiles in plant can reproduce to
        foreach (Coord coord in Environment.spawnableCoords.Where(c => Coord.Distance(c, coord) <= offspringSpawnRadious).ToList())
        {
            Gizmos.color = Color.green * new Color(1f, 1f, 1f, 0.7f);
            float cubeSize = 1f;
            Gizmos.DrawCube(Environment.tileCentres[coord.x, coord.y], new Vector3(cubeSize, 0.1f, cubeSize));
        }
    }
}