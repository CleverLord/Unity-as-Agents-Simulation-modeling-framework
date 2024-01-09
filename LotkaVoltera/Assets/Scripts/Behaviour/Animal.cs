using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[SelectionBase]
public class Animal : LivingEntity {

    public const int maxViewDistance = 10;

    [EnumFlags]
    public Species diet;

    public CreatureAction currentAction;
    public Genes genes;
    public Color maleColour;
    public Color femaleColour;

    // Settings:
    float timeBetweenActionChoices = 1;
    float moveSpeed = 1.5f;
    float timeToDeathByHunger = 200;
    float timeToDeathByThirst = 200;

    [Tooltip("How simmilar in danger are distressed kin and predator to the animal"), Range(0f, 1f)]
    public float distressToDangerPreference = 0.5f;

    float drinkDuration = 6;
    float eatDuration = 10;

    [Range(0.1f,1f)]
    float criticalPercent = 0.7f;
    [Range(1, 100)]
    public float reproductionTime;

    // Visual settings:
    protected float moveArcHeight = .2f;

    // State:
    [Header ("State")]
    public float hunger;
    public float thirst;

    protected LivingEntity foodTarget;
    protected Coord waterTarget;
    protected List<Animal> predatorsInView;
    protected List<Animal> fleeingKinInView;

    // Move data:
    bool animatingMovement;
    Coord moveFromCoord;
    Coord moveTargetCoord;
    Vector3 moveStartPos;
    Vector3 moveTargetPos;
    float moveTime;
    float moveSpeedFactor;
    float moveArcHeightFactor;
    Coord[] path;
    int pathIndex;

    // Other
    float lastActionChooseTime;
    const float sqrtTwo = 1.4142f;
    const float oneOverSqrtTwo = 1 / sqrtTwo;
    // TODO: set reproduction start when it starts
    float reproductionStartTime;


    public override void Init (Coord coord) {
        base.Init (coord);
        moveFromCoord = coord;
        genes = Genes.RandomGenes (1);

        material.color = (genes.isMale) ? maleColour : femaleColour;

        ChooseNextAction ();
    }

    protected virtual void Update () {

        // Increase hunger and thirst over time
        hunger += Time.deltaTime * 1 / timeToDeathByHunger;
        thirst += Time.deltaTime * 1 / timeToDeathByThirst;
        // TODO: implement libido factor
        // libido += Time.deltaTime * 1 / timeToMaxLibido;

        // Animate movement. After moving a single tile, the animal will be able to choose its next action
        if (animatingMovement) {
            AnimateMove ();
        } else {
            // Handle interactions with external things, like food, water, mates
            HandleInteractions ();
            float timeSinceLastActionChoice = Time.time - lastActionChooseTime;
            if (timeSinceLastActionChoice > timeBetweenActionChoices) {
                ChooseNextAction ();
            }
        }

        if (hunger >= 1) {
            Die (CauseOfDeath.Hunger);
        } else if (thirst >= 1) {
            Die (CauseOfDeath.Thirst);
        }
    }

    // Get consumed in whole
    public void Consume()
    {
        Die(CauseOfDeath.Eaten);
    }

    // Animals choose their next action after each movement step (1 tile),
    // or, when not moving (e.g interacting with food etc), at a fixed time interval
    protected virtual void ChooseNextAction () {
        lastActionChooseTime = Time.time;
        // Get info about surroundings
        SenceDangers();

        // Decide next action:
        // set the default action
        currentAction = CreatureAction.Exploring;

        // if there are no dengares near by
        if (predatorsInView.Count == 0 && fleeingKinInView.Count == 0)
        {
            // Eat if (more hungry than thirsty) or (currently eating and not critically thirsty)
            bool currentlyEating = currentAction == CreatureAction.Eating && foodTarget && hunger > 0;
            if (hunger >= thirst || currentlyEating && thirst < criticalPercent)
            {
                FindFood();
            }
            // More thirsty than hungry
            else
            {
                FindWater();
            }
        } else
        {
            FindSaferGround();
        }

        // Execute current action action
        Act ();
    }

    protected virtual void FindSaferGround()
    {
        if (predatorsInView.Count == 0 && fleeingKinInView.Count > 0)
        {
            currentAction = CreatureAction.Distressed;
        }
        else
        {
            currentAction = CreatureAction.FleeingFromDanger;
        }

        // create path to neighbour
        CreatePath(Environment.SenseSafestNeighbour(coord, this));
    }

    protected virtual void FindFood () {
        LivingEntity foodSource = Environment.SenseFood (coord, this, FoodPreferencePenalty);
        if (foodSource) {
            currentAction = CreatureAction.GoingToFood;
            foodTarget = foodSource;
            CreatePath (foodTarget.coord);
        } else {
            currentAction = CreatureAction.Exploring;
        }
    }

    public void SenceDangers()
    {
        predatorsInView = Environment.SensePredators(coord, this);
        fleeingKinInView = Environment.SenceFleeingKin(coord, this);
    }

    protected virtual void FindWater () {
        Coord waterTile = Environment.SenseWater (coord);
        if (waterTile != Coord.invalid) {
            currentAction = CreatureAction.GoingToWater;
            waterTarget = waterTile;
            CreatePath (waterTarget);

        } else {
            currentAction = CreatureAction.Exploring;
        }
    }

    // When choosing from multiple food sources, the one with the lowest penalty will be selected
    protected virtual int FoodPreferencePenalty (LivingEntity self, LivingEntity food) {
        return Coord.SqrDistance (self.coord, food.coord);
    }

    protected void Act () {
        switch (currentAction) {
            case CreatureAction.Exploring:
                StartMoveToCoord (Environment.GetNextTileWeighted (coord, moveFromCoord));
                break;
            case CreatureAction.GoingToFood:
                if (Coord.AreNeighbours (coord, foodTarget.coord)) {
                    LookAt (foodTarget.coord);
                    currentAction = CreatureAction.Eating;
                } else {
                    StartMoveToCoord (path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.GoingToWater:
                if (Coord.AreNeighbours (coord, waterTarget)) {
                    LookAt (waterTarget);
                    currentAction = CreatureAction.Drinking;
                } else {
                    StartMoveToCoord (path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.SearchingForMate:
                // TODO: implement
                break;
            case CreatureAction.Reproducing:
                // TODO: do nothing until reproducing is finished
                break;
            case CreatureAction.FleeingFromDanger:
                if (coord != path[pathIndex])
                {
                    StartMoveToCoord(path[pathIndex]);
                    pathIndex++;
                } else
                {
                    currentAction = CreatureAction.Resting;
                }
                break;
            case CreatureAction.Distressed:
                if (coord != path[pathIndex])
                {
                    StartMoveToCoord(path[pathIndex]);
                    pathIndex++;
                } else
                {
                    currentAction = CreatureAction.Resting;
                }
                break;
        }
    }

    protected void CreatePath (Coord target) {
        // Create new path if current is not already going to target
        if (path == null || pathIndex >= path.Length || (path[path.Length - 1] != target || path[pathIndex - 1] != moveTargetCoord)) {
            path = EnvironmentUtility.GetPath (coord.x, coord.y, target.x, target.y);
            pathIndex = 0;
        }
    }

    protected void StartMoveToCoord (Coord target) {
        moveFromCoord = coord;
        moveTargetCoord = target;
        moveStartPos = transform.position;
        moveTargetPos = Environment.tileCentres[moveTargetCoord.x, moveTargetCoord.y];
        animatingMovement = true;

        bool diagonalMove = Coord.SqrDistance (moveFromCoord, moveTargetCoord) > 1;
        moveArcHeightFactor = (diagonalMove) ? sqrtTwo : 1;
        moveSpeedFactor = (diagonalMove) ? oneOverSqrtTwo : 1;

        LookAt (moveTargetCoord);
    }

    protected void LookAt (Coord target) {
        if (target != coord) {
            Coord offset = target - coord;
            transform.eulerAngles = Vector3.up * Mathf.Atan2 (offset.x, offset.y) * Mathf.Rad2Deg;
        }
    }

    void HandleInteractions () {
        if (currentAction == CreatureAction.Eating) {
            if (foodTarget && hunger > 0) {
                
                // Eat a rabbit
                if (diet == Species.Rabbit)
                {
                    ((Animal) foodTarget).Die(CauseOfDeath.Eaten);
                    hunger = 0;
                }
                else if (diet == Species.Fox)
                {
                    // At the moment foxes can not get eaten
                    Debug.LogError("Not implemented Eating interaction for eating a Fox!");
                }
                else if (diet == Species.Plant)
                {
                    float eatAmount = Mathf.Min (hunger, Time.deltaTime * 1 / eatDuration);
                    eatAmount = ((Plant) foodTarget).Consume(eatAmount);
                    hunger -= eatAmount;
                } else
                {
                    Debug.LogError("Not implemented Eating interaction!");
                }
            }
        } else if (currentAction == CreatureAction.Drinking) {
            if (thirst > 0) {
                thirst -= Time.deltaTime * 1 / drinkDuration;
                thirst = Mathf.Clamp01 (thirst);
            }
        } else if (currentAction == CreatureAction.Reproducing)
        {
            if (reproductionStartTime + reproductionTime > Time.time)
            {
                // TODO: if female then spawn offspring
            }
        }
    }

    public List<Animal> GetVisibleDangers()
    {
        return predatorsInView.Concat(fleeingKinInView).ToList();
    }

    void AnimateMove () {
        // Move in an arc from start to end tile
        moveTime = Mathf.Min (1, moveTime + Time.deltaTime * moveSpeed * moveSpeedFactor);
        float height = (1 - 4 * (moveTime - .5f) * (moveTime - .5f)) * moveArcHeight * moveArcHeightFactor;
        transform.position = Vector3.Lerp (moveStartPos, moveTargetPos, moveTime) + Vector3.up * height;

        // Finished moving
        if (moveTime >= 1) {
            Environment.RegisterMove (this, coord, moveTargetCoord);
            coord = moveTargetCoord;

            animatingMovement = false;
            moveTime = 0;
            ChooseNextAction ();
        }
    }

    void OnDrawGizmosSelected () {
        if (Application.isPlaying) {
            var surroundings = Environment.Sense (coord);
            Gizmos.color = Color.white;
            if (surroundings.nearestFoodSource != null) {
                Gizmos.DrawLine (transform.position, surroundings.nearestFoodSource.transform.position);
            }
            if (surroundings.nearestWaterTile != Coord.invalid) {
                Gizmos.DrawLine (transform.position, Environment.tileCentres[surroundings.nearestWaterTile.x, surroundings.nearestWaterTile.y]);
            }

            if (currentAction == CreatureAction.GoingToFood) {
                var path = EnvironmentUtility.GetPath(coord.x, coord.y, foodTarget.coord.x, foodTarget.coord.y);
                Gizmos.color = Color.black;
                if (path != null)
                {
                    for (int i = 0; i < path.Length; i++)
                    {
                        Gizmos.DrawSphere(Environment.tileCentres[path[i].x, path[i].y], .2f);
                    }
                }
            }
        }
    }

}