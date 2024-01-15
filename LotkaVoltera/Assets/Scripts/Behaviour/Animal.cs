using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using System;
using Unity.Mathematics;
using UnityEditor;

[SelectionBase]
public class Animal : LivingEntity
{

    [ReadOnly]
    public float maturity = 1f;
    float growAmmount;
    public const int maxViewDistance = 5;

    [EnumFlags]
    public Species diet;

    public CreatureAction currentAction;
    public Genes genes;
    public Color maleColour;
    public Color femaleColour;

    // Settings:
    float timeBetweenActionChoices = 1;
    float moveSpeed = 1.5f;
    float timeToDeathByHunger = 300;
    float timeToDeathByThirst = 200;

    [Tooltip("How simmilar in danger are distressed kin and predator to the animal"), Range(0f, 1f)]
    public float distressToDangerPreference = 0.99f;

    float drinkDuration = 6;
    float eatDuration = 10;

    [Tooltip("How thirsty before seeking water"), Range(0.01f, 1f)]
    public float thirstThreshold = 0.3f;
    [Tooltip("How hungry before seeking food"), Range(0.01f, 1f)]
    public float hungerThreshold = 0.5f;
    [Range(1, 100)]
    public float reproductionTime = 5.0f;

    // Visual settings:
    protected float moveArcHeight = .2f;

    // State:
    [Header("State")]
    public float hunger;
    public float thirst;

    protected LivingEntity foodTarget;
    protected Coord waterTarget;
    protected List<Animal> predatorsInView;
    protected List<Animal> fleeingKinInView;
    protected Animal mateTarget;

    // animal parents
    Animal mother;
    Animal father;

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
    [Range(0f, 10f)]
    public float panicDuration = 2f;
    float lastDangerSeenTime;
    const float sqrtTwo = 1.4142f;
    const float oneOverSqrtTwo = 1 / sqrtTwo;
    // TODO: set reproduction start when it starts
    float reproductionStartTime;
    
    [Range(0f, 100f)]
    float growDelay = 5f;
    private float growStartTime;
    // offspring grow duration
    [Range(1, 100)]
    public float growDuration = 20;

    [Range(1, 100)]
    public int maxOffspringPerMating = 2;
    [Range(1, 100)]
    public float minTimeBetweenReproducing = 40f;
    public float libido;
    
    private List<Animal> desiredMates = new List<Animal>();
    private List<Animal> undesiredMates = new List<Animal>();

    public override void Init(Coord coord)
    {
        base.Init(coord);
        moveFromCoord = coord;
        // TODO: set child genetics based on parent and random mutation chance
        genes = Genes.RandomGenes(1);

        if (mother != null && father != null)
        {
            // add mother to undesired mates
            if (genes.isMale == true)
                undesiredMates.Add(mother);
            else // add father to undesired mates
                undesiredMates.Add(father);

            // look at father when bourne (mother might have the same position)
            LookAt(father.coord);
        }

        material.color = (genes.isMale) ? maleColour : femaleColour;

        ChooseNextAction();
    }

    private void Start()
    {
        // libido = Time.time;
        transform.localScale = Vector3.one * maturity;
        growStartTime = Time.time + growDelay;
    }

    static double GetRandomFloat(System.Random random, double minValue, double maxValue)
    {
        // Generate a random double between 0.0 (inclusive) and 1.0 (exclusive)
        double randomDouble = random.NextDouble();

        // Scale and shift the random double to fit within the specified range
        double result = minValue + (randomDouble * (maxValue - minValue));

        return result;
    }

    protected virtual void Update()
    {
        if (Time.time > growStartTime && Time.time < growStartTime + growDuration &&
            maturity > 0f && maturity < 1f)
        {
            Grow();
        }

        // Increase hunger and thirst over time
        hunger += Time.deltaTime * 1f / timeToDeathByHunger;
        thirst += Time.deltaTime * 1f / timeToDeathByThirst;
        // increase libido
        libido += Time.deltaTime * 1f / minTimeBetweenReproducing;

        // Animate movement. After moving a single tile, the animal will be able to choose its next action
        if (animatingMovement)
        {
            AnimateMove();
        }
        else
        {
            // Handle interactions with external things, like food, water, mates
            HandleInteractions();
            float timeSinceLastActionChoice = Time.time - lastActionChooseTime;
            if (timeSinceLastActionChoice > timeBetweenActionChoices)
            {
                ChooseNextAction();
            }
        }

        if (hunger >= 1)
        {
            Debug.Log($"{species} died of hunger");
            Die(CauseOfDeath.Hunger);
        }
        else if (thirst >= 1)
        {
            Debug.Log($"{species} died of thirst");
            Die(CauseOfDeath.Thirst);
        }
    }

    private void Grow()
    {
        maturity += growAmmount * (Time.deltaTime / growDuration); // fraction by which the animal should grow
        maturity = Mathf.Clamp01(maturity);
        transform.localScale = Vector3.one * maturity; // adjust scale to curr size
    }

    public override LivingEntity GetOffspring()
    {
        Animal offspring = Instantiate(this.gameObject).GetComponent<Animal>();        
        // set initial state based on parents
        offspring.thirst = 0f;
        offspring.hunger = 0f;
        offspring.libido = 0f;
        offspring.mateTarget = null;
        offspring.maturity = 0.5f;
        offspring.mother = this;
        offspring.father = mateTarget;
        offspring.currentAction = CreatureAction.None;

        // clear relations with kin
        offspring.desiredMates = new List<Animal>();
        offspring.undesiredMates = new List<Animal>();

        // scale game object to apropriate size
        offspring.transform.localScale = Vector3.one * maturity;
        offspring.growAmmount = 1f - offspring.maturity;
        // reference to environment offspring spawner method
        offspring.reproduce = reproduce;
        return offspring;
    }

    // Get consumed in whole
    public void Consume()
    {
        Debug.Log($"{species} was eaten");
        Die(CauseOfDeath.Eaten);
    }

    // Animals choose their next action after each movement step (1 tile),
    // or, when not moving (e.g interacting with food etc), at a fixed time interval
    protected virtual void ChooseNextAction()
    {
        lastActionChooseTime = Time.time;
        // Reproduction blinds animal
        if (currentAction == CreatureAction.Reproducing)
            return;
        
        // Get info about surroundings
        SenceDangers();

        // Decide next action:
        // set the default action
        // currentAction = CreatureAction.FleeingFromDanger;

        // if there are no dengares near by
        if (lastDangerSeenTime + panicDuration > Time.time && predatorsInView.Count == 0 && fleeingKinInView.Count == 0)
        {
            bool stillThirsty = currentAction == CreatureAction.Drinking && thirst > 0;
            bool stillHungry = currentAction == CreatureAction.Eating && hunger > 0 && foodTarget != null;
            
            // if currently dringing or eating
            if (thirst > thirstThreshold || stillThirsty || hunger > hungerThreshold || stillHungry)
            {
                // Eat if (more hungry than thirsty) or (currently eating and not critically thirsty)
                bool currentlyEating = currentAction == CreatureAction.Eating && foodTarget && hunger > 0;
                if (hunger >= thirst || currentlyEating && thirst < hungerThreshold)
                {
                    Debug.LogWarning($"{species} searching for food");
                    FindFood();
                }
                // More thirsty than hungry
                else
                {
                    Debug.LogWarning($"{species} searching for water");
                    FindWater();
                }
            } else if (mateTarget == null && libido >= 1f && CanReproduce())
            {                
                FindMate();
            }
        }
        else
        {
            lastDangerSeenTime = Time.time;
            FindSaferGround();
        }

        // Execute current action action
        Act();
    }

    public bool CanReproduce()
    {
        return maturity >= 1f;
    }

    public bool JudgeMate(Coord coord, Animal potentialMate)
    {
        // can not reproduce with non existing partner
        // do not remember judgement for non existing partner
        if (potentialMate == null)
            return false;
        if (undesiredMates.Contains(potentialMate))
        {
            return false;
        }
        if (desiredMates.Contains(potentialMate))
        {
            return true;
        }

        bool isMature = potentialMate.CanReproduce();
        bool isOppositeSex = genes.isMale != potentialMate.genes.isMale;
        bool isSearchingForMate = potentialMate.currentAction == CreatureAction.SearchingForMate;
        bool isGeneticalyDesired = false;
        // TODO: consider also: isParent, isSibling, isChild

        float desirability = genes.GeneticDesirability();
        float desirabilityHalfed = desirability / 2f;
        float potentialMateDesirability = potentialMate.genes.GeneticDesirability();
        // females only want to reproduce with not less desirable mates than themselves
        if (genes.isMale == false) {
            isGeneticalyDesired = desirability <= potentialMateDesirability;
        } else
        {
            // males want to reproduce with females at least half as desirable as them
            isGeneticalyDesired = desirabilityHalfed <= potentialMateDesirability;
        }

        bool isDesired = isMature && isOppositeSex && isSearchingForMate && isGeneticalyDesired;
        
        isDesired = isOppositeSex; // TODO: Remove (set only for debug purposes, calculate value correctly)

        // remember mate resirebility
        if (isDesired) { desiredMates.Add(potentialMate); } else { undesiredMates.Add(potentialMate); }

        return isDesired;
    }

    protected virtual void FindMate()
    {
        // search for mate
        currentAction = CreatureAction.SearchingForMate;

        List<Animal> potentialMates = Environment.SensePotentialMates(coord, this);
        
        // if has no mate yet and sees potential mates in state of searching for one
        if (mateTarget == null && potentialMates.Count > 0)
        {
            // go to mate
            currentAction = CreatureAction.GoingToMate;
            mateTarget = potentialMates.First();
            CreatePath(mateTarget.coord);
            
            // make mate go this animal
            mateTarget.currentAction = CreatureAction.GoingToMate;
            mateTarget.mateTarget = this;
            mateTarget.CreatePath(coord);
        } else
        {
            mateTarget = null;
        }
    }

    protected virtual void FindSaferGround()
    {
        if (predatorsInView.Count == 0 && fleeingKinInView.Count == 0)
        {
            currentAction = CreatureAction.Exploring;
            return;
        }
        else if (predatorsInView.Count == 0 && fleeingKinInView.Count > 0)
        {
            currentAction = CreatureAction.Distressed;
        } else
        {
            currentAction = CreatureAction.FleeingFromDanger;
        }

        // create path to neighbour
        Coord? newSafeGround = Environment.SenseEscapeDestination(coord, this);
        if (newSafeGround != null)
        {
            CreatePath(newSafeGround.Value);
        } else {
            Debug.LogWarning($"Animal of species: {species} could not find safer position in view, now Exploring!");
            currentAction = CreatureAction.Exploring;
        }
    }

    protected virtual void FindFood()
    {
        LivingEntity foodSource = Environment.SenseFood(coord, this, FoodPreferencePenalty);
        if (foodSource)
        {
            currentAction = CreatureAction.GoingToFood;
            foodTarget = foodSource;
            CreatePath(foodTarget.coord);
        }
        else
        {
            currentAction = CreatureAction.Exploring;
        }
    }

    public void SenceDangers()
    {
        predatorsInView = Environment.SensePredators(coord, this);
        fleeingKinInView = Environment.SenseFleeingKin(coord, this);
    }

    protected virtual void FindWater()
    {
        Coord waterTile = Environment.SenseWater(coord);
        if (waterTile != Coord.invalid)
        {
            currentAction = CreatureAction.GoingToWater;
            waterTarget = waterTile;
            CreatePath(waterTarget);
        }
        else
        {
            currentAction = CreatureAction.Exploring;
        }
    }

    // When choosing from multiple food sources, the one with the lowest penalty will be selected
    protected virtual int FoodPreferencePenalty(LivingEntity self, LivingEntity food)
    {
        return Coord.SqrDistance(self.coord, food.coord);
    }

    protected void Act()
    {
        switch (currentAction)
        {
            case CreatureAction.Exploring:
                StartMoveToCoord(Environment.GetNextTileWeighted(coord, moveFromCoord));
                break;
            case CreatureAction.GoingToFood:
                if (Coord.AreNeighbours(coord, foodTarget.coord))
                {
                    LookAt(foodTarget.coord);
                    currentAction = CreatureAction.Eating;
                }
                else
                {
                    StartMoveToCoord(path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.GoingToWater:
                if (Coord.AreNeighbours(coord, waterTarget))
                {
                    LookAt(waterTarget);
                    currentAction = CreatureAction.Drinking;
                }
                else
                {
                    StartMoveToCoord(path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.SearchingForMate:
                // exploring in hopes of finding mate
                StartMoveToCoord(Environment.GetNextTileWeighted(coord, moveFromCoord));
                break;
            case CreatureAction.GoingToMate:
                LookAt(mateTarget.coord);
                if (Coord.AreNeighbours(coord, mateTarget.coord))
                {
                    path = null;
                    currentAction = CreatureAction.Reproducing;
                    mateTarget.path = null;
                    reproductionStartTime = Time.time;
                    mateTarget.currentAction = CreatureAction.Reproducing;
                    mateTarget.reproductionStartTime = reproductionStartTime;
                } else
                {
                    StartMoveToCoord(path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.FleeingFromDanger:
                if (path != null && coord != path[pathIndex])
                {
                    StartMoveToCoord(path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.Distressed:
                if (path != null && coord != path[pathIndex])
                {
                    StartMoveToCoord(path[pathIndex]);
                    pathIndex++;
                }
                break;
        }
    }

    protected void CreatePath(Coord target)
    {
        // Create new path if current is not already going to target
        if (path == null || pathIndex >= path.Length || (path[path.Length - 1] != target || path[pathIndex - 1] != moveTargetCoord))
        {
            path = EnvironmentUtility.GetPath(coord.x, coord.y, target.x, target.y);
            pathIndex = 0;
        }
    }

    protected void StartMoveToCoord(Coord target)
    {
        moveFromCoord = coord;
        moveTargetCoord = target;
        moveStartPos = transform.position;
        moveTargetPos = Environment.tileCentres[moveTargetCoord.x, moveTargetCoord.y];
        animatingMovement = true;

        bool diagonalMove = Coord.SqrDistance(moveFromCoord, moveTargetCoord) > 1;
        moveArcHeightFactor = (diagonalMove) ? sqrtTwo : 1;
        moveSpeedFactor = (diagonalMove) ? oneOverSqrtTwo : 1;

        LookAt(moveTargetCoord);
    }

    protected void LookAt(Coord target)
    {
        if (target != coord)
        {
            Coord offset = target - coord;
            transform.eulerAngles = Vector3.up * Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg;
        }
    }

    void HandleInteractions()
    {
        if (currentAction == CreatureAction.Eating)
        {
            if (foodTarget && hunger > 0)
            {

                // Eat a rabbit
                if (diet == Species.Rabbit)
                {
                    ((Animal)foodTarget).Die(CauseOfDeath.Eaten);
                    hunger = 0;
                }
                else if (diet == Species.Fox)
                {
                    // At the moment foxes can not get eaten
                    Debug.LogError("Not implemented Eating interaction for eating a Fox!");
                }
                else if (diet == Species.Plant)
                {
                    float eatAmount = Mathf.Min(hunger, Time.deltaTime * 1 / eatDuration);
                    eatAmount = ((Plant)foodTarget).Consume(eatAmount);
                    hunger -= eatAmount;
                }
                else
                {
                    Debug.LogError("Not implemented Eating interaction!");
                }
            }
        }
        else if (currentAction == CreatureAction.Drinking)
        {
            if (thirst > 0)
            {
                thirst -= Time.deltaTime * 1 / drinkDuration;
                thirst = Mathf.Clamp01(thirst);
            }
        }
        else if (currentAction == CreatureAction.Reproducing)
        {
            if (reproductionStartTime + reproductionTime <= Time.time && genes.isMale == false)
            {
                // if female then spawn offspring
                for (int i = 0; i < (int) GetRandomFloat(new System.Random(), 0f, maxOffspringPerMating); i++)
                {
                    // try to produce offspring
                    reproduce(this);
                }
                // cleanup after reproducing
                currentAction = CreatureAction.None;
                libido = 0f;
                mateTarget = null;
                path = null;
            }
        }
    }

    public List<Animal> GetVisibleDangers()
    {
        return predatorsInView.Concat(fleeingKinInView).ToList();
    }

    void AnimateMove()
    {
        // Move in an arc from start to end tile
        moveTime = Mathf.Min(1, moveTime + Time.deltaTime * moveSpeed * moveSpeedFactor);
        float height = (1 - 4 * (moveTime - .5f) * (moveTime - .5f)) * moveArcHeight * moveArcHeightFactor;
        transform.position = Vector3.Lerp(moveStartPos, moveTargetPos, moveTime) + Vector3.up * height;

        // Finished moving
        if (moveTime >= 1)
        {
            Environment.RegisterMove(this, coord, moveTargetCoord);
            coord = moveTargetCoord;

            animatingMovement = false;
            moveTime = 0;
            ChooseNextAction();
        }
    }

    public Animal? GetCurrMate()
    {
        return mateTarget;
    }

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            var surroundings = Environment.Sense(coord);
            Gizmos.color = Color.white;
            if (surroundings.nearestFoodSource != null)
            {
                Gizmos.DrawLine(transform.position, surroundings.nearestFoodSource.transform.position);
            }
            if (surroundings.nearestWaterTile != Coord.invalid)
            {
                Gizmos.DrawLine(transform.position, Environment.tileCentres[surroundings.nearestWaterTile.x, surroundings.nearestWaterTile.y]);
            }

            if (currentAction == CreatureAction.GoingToFood)
            {
                var path = EnvironmentUtility.GetPath(coord.x, coord.y, foodTarget.coord.x, foodTarget.coord.y);
                //Gizmos.color = Color.black;
                if (path != null)
                {
                    for (int i = 0; i < path.Length; i++)
                    {
                        float interpoler = Mathf.Clamp01((i + 1) * 1.0f / path.Length);
                        Gizmos.color = Color.Lerp(Color.white, Color.black, interpoler);
                        Gizmos.DrawSphere(Environment.tileCentres[path[i].x, path[i].y], .2f);
                    }
                }
            }

            if (currentAction == CreatureAction.FleeingFromDanger || currentAction == CreatureAction.Distressed)
            {
                if (path != null)
                {
                    for (int i = 0; i < path.Length; i++)
                    {
                        float interpoler = Mathf.Clamp01((i+1) * 1.0f / path.Length);
                        Gizmos.color = Color.Lerp(Color.white, Color.black, interpoler);
                        Gizmos.DrawSphere(Environment.tileCentres[path[i].x, path[i].y], .2f);
                    }
                }
            }

            // draw gizmos for tiles in animal persivable surroundings
            foreach (Coord coord in surroundings.moveDestinations)
            {
                Gizmos.color = Color.blue * new Color(1f, 1f, 1f, 0.6f);
                float cubeSize = 1f;
                Gizmos.DrawCube(Environment.tileCentres[coord.x, coord.y], new Vector3(cubeSize, 0.1f, cubeSize));
            }

            if (currentAction == CreatureAction.GoingToMate)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, Environment.tileCentres[mateTarget.coord.x, mateTarget.coord.y]);
            }

            // draw red gizmo line for judged, rejected mates
            foreach (Animal mate in undesiredMates)
            {
                if (mate != null && mate.coord != null)
                {
                    Gizmos.color = Color.red * new Color(1f, 1f, 1f, 1f);
                    Gizmos.DrawLine(transform.position, new Vector3(mateTarget.coord.x, 0f, mateTarget.coord.y));
                }
            }

            // draw gray gizmo line line for judged, deired mates
            foreach (Animal mate in desiredMates)
            {
                if (mate != null && mate.coord != null)
                {
                    if (mate != mateTarget)
                    {
                        Gizmos.color = Color.gray * new Color(1f, 1f, 1f, 1f);
                    }
                    else // draw green gizmo line for current mate                    
                    {
                        Gizmos.color = Color.green * new Color(1f, 1f, 1f, 1f);
                    }
                    Gizmos.DrawLine(transform.position, new Vector3(mateTarget.coord.x, 0f, mateTarget.coord.y));
                }
            }
        }
    }

}