using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TerrainGeneration;
using UnityEditor;
using UnityEditorInternal.VR;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.Rendering.DebugUI.Table;
using static UnityEngine.UI.Image;

public class Environment : MonoBehaviour {

    const int mapRegionSize = 10;

    public int seed;

    [Header ("Trees")]
    public MeshRenderer treePrefab;
    [Range (0, 1)]
    public float treeProbability;

    [Header ("Populations")]
    public Population[] initialPopulations;

    [Header ("Debug")]
    public bool showMapDebug;
    public Transform mapCoordTransform;
    public float mapViewDst;

    // Cached data:
    static List<Coord> spawnableCoords; // coordinates of all the unocupied map regions

    public static Vector3[, ] tileCentres;
    public static bool[, ] walkable;
    static int size;
    static Coord[, ][] walkableNeighboursMap;
    static List<Coord> walkableCoords;

    static Dictionary<Species, List<Species>> preyBySpecies;
    static Dictionary<Species, List<Species>> predatorsBySpecies;
    static protected Dictionary<Species, float[,]> safetyMapBySpecies;

    // array of visible tiles from any tile; value is Coord.invalid if no visible water tile
    static Coord[, ] closestVisibleWaterMap;

    static System.Random prng;
    TerrainGenerator.TerrainData terrainData;

    public static Dictionary<Species, Map> speciesMaps;

    static float baseSafetyScore = (10 * Animal.maxViewDistance) + (2f * (float)(Math.Sqrt(2f * (Animal.maxViewDistance + 1))));

    void Start () {
        prng = new System.Random (seed);

        Init (); // Trees are spawned here
        SpawnInitialPopulations (); // Species are spawned here
    }

    void OnDrawGizmos () {
        /* 
        if (showMapDebug) {
            if (preyMap != null && mapCoordTransform != null) {
                Coord coord = new Coord ((int) mapCoordTransform.position.x, (int) mapCoordTransform.position.z);
                preyMap.DrawDebugGizmos (coord, mapViewDst);
            }
        }
        */
    }

    // recalculate map - use after move and spawn (in general when location of any predator changes)
    static void RecalculateDangerMap(Species speciesInDanger, string callReason)
    {
        // Don't calculate danger maps for plants
        if (speciesInDanger == Species.Plant)
            return;
        // if species has no predators
        if (predatorsBySpecies[speciesInDanger].Count == 0)
            return;
        // get all the animals of species in danger
        Map speciesMap = speciesMaps[speciesInDanger];

        // get minimal animals list representing vision for all animals of that species
        List<Animal> animalVisionRepresentants = speciesMap.map.Cast<List<LivingEntity>>()
        .Where(list => list != null && list.Count > 0)
        .Select(list => list.First())
        .Select(livingEntity => (Animal) livingEntity)
        .ToList();


        // array for checking if any entity already evaluated tile safety score
        bool[,] evaluatedTile = new bool[
            safetyMapBySpecies[speciesInDanger].GetLength(0),
            safetyMapBySpecies[speciesInDanger].GetLength(1)
        ];
        
        // clear animal safety map
        for (int i = 0; i < safetyMapBySpecies[speciesInDanger].GetLength(0); i++)
        {
            for (int j = 0; j < safetyMapBySpecies[speciesInDanger].GetLength(1); j++)
            {
                // clear also the evaluater tile array
                evaluatedTile[i, j] = false;
                if (walkable[i, j] == false)
                    continue;

                safetyMapBySpecies[speciesInDanger][i, j] = baseSafetyScore;
            }
        }

        // for each animal representing the general animal vision for the animal position
        // calculate the safety values of all the tiles in animal surounding
        foreach (Animal self in animalVisionRepresentants)
        {
            List<Animal> dangers = self.GetVisibleDangers();

            // if there are no visible dangers
            // skip calculating safety score for this animal field of view
            if (dangers.Count == 0)
                continue;

            // consider all walkable tiles in view of the animal (visible and walkable)
            List<Coord> potentialDestinations = walkableCoords.Where(
                c => EnvironmentUtility.TileIsVisibile(self.coord.x, self.coord.y, c.x, c.y) &&
                Animal.maxViewDistance >= Coord.Distance(self.coord, c)
            ).ToList();

            // For only the walkable coordinates
            foreach (Coord inViewCoord in potentialDestinations)
            {
                // Calculate safety score for the current tile
                float safetyScore = 0f;
                

                foreach (Animal danger in dangers)
                {
                    float safetyMultiplyer = 1f;
                    if (danger.species == self.species)
                        safetyMultiplyer += self.distressToDangerPreference;

                    float dst = safetyMultiplyer * Coord.Distance(inViewCoord, danger.coord);
                    safetyScore = dst;

                    int walkableNeighboursCount = walkableNeighboursMap[inViewCoord.x, inViewCoord.y].Length + 1;
                    safetyScore *= walkableNeighboursCount;

                    float oldSafetyScore = safetyMapBySpecies[speciesInDanger][inViewCoord.x, inViewCoord.y];
                    if (evaluatedTile[inViewCoord.x, inViewCoord.y] == false || oldSafetyScore > safetyScore)
                    {
                        safetyMapBySpecies[speciesInDanger][inViewCoord.x, inViewCoord.y] = safetyScore;
                        evaluatedTile[inViewCoord.x, inViewCoord.y] = true;
                    }
                }
            }
        }

        float safetyScoreTrulySafeOrNoInfo = safetyMapBySpecies[speciesInDanger].Cast<float>().Max() + 1;

        // clear animal safety map
        for (int i = 0; i < safetyMapBySpecies[speciesInDanger].GetLength(0); i++)
        {
            for (int j = 0; j < safetyMapBySpecies[speciesInDanger].GetLength(1); j++)
            {
                if (evaluatedTile[i, j] == true)
                    continue;
                if (walkable[i, j] == false)
                    continue;

                safetyMapBySpecies[speciesInDanger][i, j] = safetyScoreTrulySafeOrNoInfo;
            }
        }

        if (speciesInDanger == Species.Rabbit)
        {
            List<float> flattened = safetyMapBySpecies[speciesInDanger].Cast<float>().ToList();
            float maxSafetyScore = flattened.Max();
            Debug.Log($"Recalculated safety map for species: {speciesInDanger}\nCall reason: {callReason}");
        }
    }

    static void RecalculateDangerMaps(LivingEntity entity, string callReason)
    {
        // Don't calculate danger maps for plants
        if (entity.species == Species.Plant)
            return;
        // Recalculate danger maps for species beeing in danger of this entity
        List<Species> preySpecies = preyBySpecies[((Animal)entity).species];
        foreach (Species prey in preySpecies)
        {
            RecalculateDangerMap(prey, callReason + " -> " + $"{prey} dangers changed");
        }
        // If this species is also a prey to some species
        // recalculate safety map for species of entity
        if (predatorsBySpecies[((Animal)entity).species].Count > 0)
        {
            RecalculateDangerMap(entity.species, callReason + " -> " + $"{entity.species} collective vision changed");
        }
    }

    public Coord? ChooseReproductionSpace (Coord coord, float reproductionRadious)
    {
        // GetUnoccupiedNeighbours
        List<Coord> spawnCoords = spawnableCoords.Where(c => Coord.Distance(c, coord) <= reproductionRadious).ToList();

        // entity neighbourhood fully occupied
        if (spawnCoords.Count() == 0)
            return null;

        // get random possible pawn coordinates and return them
        int offspringCoordIndex = prng.Next(0, spawnCoords.Count());
        return spawnCoords[offspringCoordIndex];
    }

    // Register new entity spawn by preventing another entity spawn at the same coordinates
    public static void RegisterSpawn(LivingEntity entity, Coord spawnCoord)
    {
        // spawnable coords not yet initialized
        if (spawnableCoords == null)
            return;

        // Remove new spawn coordinates from spawnable list
        // if spawn possible
        if (spawnableCoords.Any(c => c == spawnCoord))
        {
            // Add new entity
            speciesMaps[entity.species].Add(entity, spawnCoord);
            // remove coordinates from avaliable to spawn new entities
            spawnableCoords.Remove(spawnCoord);
        } else
        {
            Debug.LogError($"Spawn was not possible for entity of species: {entity.species.ToString()} at coorinates: {spawnCoord.ToString()}");
        }

        Debug.Log("Entity spawned");
        // recalculate safetyMaps
        RecalculateDangerMaps(entity, $"{entity.species} spawned");
    }

    public static void RegisterMove (LivingEntity entity, Coord from, Coord to) {
        // Move entity
        speciesMaps[entity.species].Move (entity, from, to);
        // Remove new coordinates from spawnable and add old ones
        spawnableCoords.Remove(to);
        spawnableCoords.Add(from);

        Debug.Log("Entity moved");
        // recalculate safetyMaps
        RecalculateDangerMaps(entity, $"{entity.species} moved");
    }

    public static void RegisterDeath (LivingEntity entity) {
        // Remove entity
        speciesMaps[entity.species].Remove (entity, entity.coord);
        // Add freed coordinates to spawnable
        spawnableCoords.Add(entity.coord);

        Debug.Log("Entity died");
        // recalculate safetyMaps
        RecalculateDangerMaps(entity, $"{entity.species} died");
    }

    public static Coord SenseWater (Coord coord) {
        var closestWaterCoord = closestVisibleWaterMap[coord.x, coord.y];
        if (closestWaterCoord != Coord.invalid) {
            float sqrDst = (tileCentres[coord.x, coord.y] - tileCentres[closestWaterCoord.x, closestWaterCoord.y]).sqrMagnitude;
            if (sqrDst <= Animal.maxViewDistance * Animal.maxViewDistance) {
                return closestWaterCoord;
            }
        }
        return Coord.invalid;
    }

    public static LivingEntity SenseFood (Coord coord, Animal self, System.Func<LivingEntity, LivingEntity, int> foodPreference) {
        var foodSources = new List<LivingEntity> ();

        List<Species> prey = preyBySpecies[self.species];
        for (int i = 0; i < prey.Count; i++) {

            Map speciesMap = speciesMaps[prey[i]];

            foodSources.AddRange (speciesMap.GetEntities (coord, Animal.maxViewDistance));
        }

        // Sort food sources based on preference function
        foodSources.Sort ((a, b) => foodPreference (self, a).CompareTo (foodPreference (self, b)));

        // Return first visible food source
        for (int i = 0; i < foodSources.Count; i++) {
            Coord targetCoord = foodSources[i].coord;
            if (EnvironmentUtility.TileIsVisibile (coord.x, coord.y, targetCoord.x, targetCoord.y)) {
                return foodSources[i];
            }
        }

        return null;
    }

    public static List<Animal> SensePredators(Coord coord, Animal self)
    {
        List<Animal> predators = new List<Animal>();

        // Get predators based on the species of the current animal
        List<Species> predatorSpecies = predatorsBySpecies[self.species];

        // Iterate through the predator species and get entities of those species
        for (int i = 0; i < predatorSpecies.Count; i++)
        {
            Map predatorSpeciesMap = speciesMaps[predatorSpecies[i]];

            // Add all visible predators of the given species within the max view distance
            predators.AddRange(predatorSpeciesMap.GetEntities(coord, Animal.maxViewDistance)
                .OfType<Animal>()
                .Where(predator => predator != self && EnvironmentUtility.TileIsVisibile(coord.x, coord.y, predator.coord.x, predator.coord.y)));
        }

        return predators;
    }

    public static List<Animal> SenseFleeingKin(Coord coord, Animal self)
    {
        Map speciesMap = speciesMaps[self.species];
        List<Animal> fleeingKin = speciesMap.GetEntities(
            coord,
            Animal.maxViewDistance
        ).Select(
            livingEntity => (Animal) livingEntity
        ).Where(animal => animal != self && animal.currentAction == CreatureAction.FleeingFromDanger).ToList();

        return fleeingKin;
    }

    // Return list of animals of the same species, with the opposite gender, who are also searching for a mate
    public static List<Animal> SensePotentialMates (Coord coord, Animal self) {
        Map speciesMap = speciesMaps[self.species];
        List<LivingEntity> visibleEntities = speciesMap.GetEntities (coord, Animal.maxViewDistance);
        var potentialMates = new List<Animal> ();

        for (int i = 0; i < visibleEntities.Count; i++) {
            var visibleAnimal = (Animal) visibleEntities[i];
            if (visibleAnimal != self && visibleAnimal.genes.isMale != self.genes.isMale) {
                if (visibleAnimal.currentAction == CreatureAction.SearchingForMate) {
                    // judge if animal resire each other 
                    if (visibleAnimal.JudgeMate(coord, self) && self.JudgeMate(visibleAnimal.coord, visibleAnimal))
                    {
                        potentialMates.Add (visibleAnimal);
                    }
                }

            }
        }

        return potentialMates;
    }

    // get random not neighbouring tile with the highest safety score in animal view
    public static Coord? SenseEscapeDestination (Coord coord, Animal self)
    {
        List<Coord> potentialEscapeDestinations = walkableCoords.Where(
            c => EnvironmentUtility.TileIsVisibile(self.coord.x, self.coord.y, c.x, c.y) &&
            Animal.maxViewDistance >= Coord.Distance(self.coord, c)
        ).Except(walkableNeighboursMap[self.coord.x, self.coord.y].ToList()).ToList();

        float maxSafetyValue = potentialEscapeDestinations
        .Select(c => safetyMapBySpecies[self.species][c.x, c.y])
        .DefaultIfEmpty(baseSafetyScore) // Handle the case when there are no visible coordinates
        .Max();

        List<Coord> bestEscapeDestinations = potentialEscapeDestinations.Where(
            c => safetyMapBySpecies[self.species][c.x, c.y] >= maxSafetyValue
        ).ToList();
        
        // If there are safe escape destinations, choose a random one
        if (bestEscapeDestinations.Count > 0)
        {   
            int escapeCoordIndex = prng.Next(0, bestEscapeDestinations.Count);
            return bestEscapeDestinations[escapeCoordIndex];
        } else
        {
            return null;
        }
    }

    public static Surroundings Sense (Coord coord) {
        var closestPlant = speciesMaps[Species.Plant].ClosestEntity(coord, Animal.maxViewDistance);
        var surroundings = new Surroundings();
        surroundings.nearestFoodSource = closestPlant;
        surroundings.nearestWaterTile = closestVisibleWaterMap[coord.x, coord.y];
        surroundings.moveDestinations = walkableCoords.Where(
            c => EnvironmentUtility.TileIsVisibile(coord.x, coord.y, c.x, c.y) &&
            Animal.maxViewDistance >= Coord.Distance(coord, c)
        ).ToList();

        return surroundings;
    }

    public static Coord GetNextTileRandom (Coord current) {
        var neighbours = walkableNeighboursMap[current.x, current.y];
        if (neighbours.Length == 0) {
            return current;
        }
        return neighbours[prng.Next (neighbours.Length)];
    }

    /// Get random neighbour tile, weighted towards those in similar direction as currently facing
    public static Coord GetNextTileWeighted (Coord current, Coord previous, double forwardProbability = 0.2, int weightingIterations = 3) {

        if (current == previous) {

            return GetNextTileRandom (current);
        }

        Coord forwardOffset = (current - previous);
        // Random chance of returning foward tile (if walkable)
        if (prng.NextDouble () < forwardProbability) {
            Coord forwardCoord = current + forwardOffset;

            if (forwardCoord.x >= 0 && forwardCoord.x < size && forwardCoord.y >= 0 && forwardCoord.y < size) {
                if (walkable[forwardCoord.x, forwardCoord.y]) {
                    return forwardCoord;
                }
            }
        }

        // Get walkable neighbours
        var neighbours = walkableNeighboursMap[current.x, current.y];
        if (neighbours.Length == 0) {
            return current;
        }

        // From n random tiles, pick the one that is most aligned with the forward direction:
        Vector2 forwardDir = new Vector2 (forwardOffset.x, forwardOffset.y).normalized;
        float bestScore = float.MinValue;
        Coord bestNeighbour = current;

        for (int i = 0; i < weightingIterations; i++) {
            Coord neighbour = neighbours[prng.Next (neighbours.Length)];
            Vector2 offset = neighbour - current;
            float score = Vector2.Dot (offset.normalized, forwardDir);
            if (score > bestScore) {
                bestScore = score;
                bestNeighbour = neighbour;
            }
        }

        return bestNeighbour;
    }

    // Call terrain generator and cache useful info
    void Init () {
        var sw = System.Diagnostics.Stopwatch.StartNew ();

        var terrainGenerator = FindObjectOfType<TerrainGenerator> ();
        terrainData = terrainGenerator.Generate ();

        tileCentres = terrainData.tileCentres;
        walkable = terrainData.walkable;
        size = terrainData.size;

        int numSpecies = System.Enum.GetNames (typeof (Species)).Length;
        preyBySpecies = new Dictionary<Species, List<Species>> ();
        predatorsBySpecies = new Dictionary<Species, List<Species>> ();

        // Init species maps
        speciesMaps = new Dictionary<Species, Map> ();
        for (int i = 0; i < numSpecies; i++) {
            Species species = (Species) (1 << i);
            speciesMaps.Add (species, new Map (size, mapRegionSize));

            preyBySpecies.Add (species, new List<Species> ());
            predatorsBySpecies.Add (species, new List<Species> ());
        }

        // Store predator/prey relationships for all species
        for (int i = 0; i < initialPopulations.Length; i++) {

            if (initialPopulations[i].prefab is Animal) {
                Animal hunter = (Animal) initialPopulations[i].prefab;
                Species diet = hunter.diet;

                for (int huntedSpeciesIndex = 0; huntedSpeciesIndex < numSpecies; huntedSpeciesIndex++) {
                    int bit = ((int) diet >> huntedSpeciesIndex) & 1;
                    // this bit of diet mask set (i.e. the hunter eats this species)
                    if (bit == 1) {
                        int huntedSpecies = 1 << huntedSpeciesIndex;
                        preyBySpecies[hunter.species].Add ((Species) huntedSpecies);
                        predatorsBySpecies[(Species) huntedSpecies].Add (hunter.species);
                    }
                }
            }
        }

        //LogPredatorPreyRelationships ();

        // Init species danger maps
        safetyMapBySpecies = new Dictionary<Species, float[,]>();
        foreach (Species preySpecies in preyBySpecies.Keys)
        {
            int dangerMapBySpeciesRowCount = walkable.GetLength(0);
            int dangerMapBySpeciesColCount = walkable.GetLength(1);
            // initialize danger map array with default float values
            Debug.Log($"Initializing safety maps");
            safetyMapBySpecies[preySpecies] = new float[dangerMapBySpeciesRowCount, dangerMapBySpeciesColCount];
        }

        SpawnTrees ();

        walkableNeighboursMap = new Coord[size, size][];

        // Find and store all walkable neighbours for each walkable tile on the map
        for (int y = 0; y < terrainData.size; y++) {
            for (int x = 0; x < terrainData.size; x++) {
                if (walkable[x, y]) {
                    List<Coord> walkableNeighbours = new List<Coord> ();
                    for (int offsetY = -1; offsetY <= 1; offsetY++) {
                        for (int offsetX = -1; offsetX <= 1; offsetX++) {
                            if (offsetX != 0 || offsetY != 0) {
                                int neighbourX = x + offsetX;
                                int neighbourY = y + offsetY;
                                if (neighbourX >= 0 && neighbourX < size && neighbourY >= 0 && neighbourY < size) {
                                    if (walkable[neighbourX, neighbourY]) {
                                        walkableNeighbours.Add (new Coord (neighbourX, neighbourY));
                                    }
                                }
                            }
                        }
                    }
                    walkableNeighboursMap[x, y] = walkableNeighbours.ToArray ();
                }
            }
        }

        // Generate offsets within max view distance, sorted by distance ascending
        // Used to speed up per-tile search for closest water tile
        List<Coord> viewOffsets = new List<Coord> ();
        int viewRadius = Animal.maxViewDistance;
        int sqrViewRadius = viewRadius * viewRadius;
        for (int offsetY = -viewRadius; offsetY <= viewRadius; offsetY++) {
            for (int offsetX = -viewRadius; offsetX <= viewRadius; offsetX++) {
                int sqrOffsetDst = offsetX * offsetX + offsetY * offsetY;
                if ((offsetX != 0 || offsetY != 0) && sqrOffsetDst <= sqrViewRadius) {
                    viewOffsets.Add (new Coord (offsetX, offsetY));
                }
            }
        }
        viewOffsets.Sort ((a, b) => (a.x * a.x + a.y * a.y).CompareTo (b.x * b.x + b.y * b.y));
        Coord[] viewOffsetsArr = viewOffsets.ToArray ();

        // Find closest accessible water tile for each tile on the map:
        closestVisibleWaterMap = new Coord[size, size];
        for (int y = 0; y < terrainData.size; y++) {
            for (int x = 0; x < terrainData.size; x++) {
                bool foundWater = false;
                if (walkable[x, y]) {
                    for (int i = 0; i < viewOffsets.Count; i++) {
                        int targetX = x + viewOffsetsArr[i].x;
                        int targetY = y + viewOffsetsArr[i].y;
                        if (targetX >= 0 && targetX < size && targetY >= 0 && targetY < size) {
                            if (terrainData.shore[targetX, targetY]) {
                                if (EnvironmentUtility.TileIsVisibile (x, y, targetX, targetY)) {
                                    closestVisibleWaterMap[x, y] = new Coord (targetX, targetY);
                                    foundWater = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!foundWater) {
                    closestVisibleWaterMap[x, y] = Coord.invalid;
                }
            }
        }
        Debug.Log ("Init time: " + sw.ElapsedMilliseconds);
    }

    void SpawnTrees () {
        // Settings:
        float maxRot = 4;
        float maxScaleDeviation = .2f;
        float colVariationFactor = 0.15f;
        float minCol = .8f;

        var spawnPrng = new System.Random (seed);
        var treeHolder = new GameObject ("Tree holder").transform;
        walkableCoords = new List<Coord> ();

        for (int y = 0; y < terrainData.size; y++) {
            for (int x = 0; x < terrainData.size; x++) {
                if (walkable[x, y]) {
                    if (prng.NextDouble () < treeProbability) {
                        // Randomize rot/scale
                        float rotX = Mathf.Lerp (-maxRot, maxRot, (float) spawnPrng.NextDouble ());
                        float rotZ = Mathf.Lerp (-maxRot, maxRot, (float) spawnPrng.NextDouble ());
                        float rotY = (float) spawnPrng.NextDouble () * 360f;
                        Quaternion rot = Quaternion.Euler (rotX, rotY, rotZ);
                        float scale = 1 + ((float) spawnPrng.NextDouble () * 2 - 1) * maxScaleDeviation;

                        // Randomize colour
                        float col = Mathf.Lerp (minCol, 1, (float) spawnPrng.NextDouble ());
                        float r = col + ((float) spawnPrng.NextDouble () * 2 - 1) * colVariationFactor;
                        float g = col + ((float) spawnPrng.NextDouble () * 2 - 1) * colVariationFactor;
                        float b = col + ((float) spawnPrng.NextDouble () * 2 - 1) * colVariationFactor;

                        // Spawn
                        MeshRenderer tree = Instantiate (treePrefab, tileCentres[x, y], rot);
                        tree.transform.parent = treeHolder;
                        tree.transform.localScale = Vector3.one * scale;
                        tree.material.color = new Color (r, g, b);

                        // Mark tile unwalkable
                        walkable[x, y] = false;
                    } else {
                        walkableCoords.Add (new Coord (x, y));
                    }
                }
            }
        }
    }

    void SpawnInitialPopulations () {

        var spawnPrng = new System.Random (seed);
        var spawnCoords = new List<Coord> (walkableCoords);

        foreach (var pop in initialPopulations) {
            for (int i = 0; i < pop.count; i++) {
                if (spawnCoords.Count == 0) {
                    Debug.Log ("Ran out of empty tiles to spawn initial population");
                    break;
                }
                int spawnCoordIndex = spawnPrng.Next (0, spawnCoords.Count);
                Coord coord = spawnCoords[spawnCoordIndex];
                spawnCoords.RemoveAt (spawnCoordIndex);

                var entity = Instantiate (pop.prefab);
                var entityScript = entity.GetComponent<LivingEntity>();
                // register callback to method
                entityScript.reproduce += SpawnOffspring;
                entity.Init (coord);

                speciesMaps[entity.species].Add(entity, coord);
                // recalculate safetyMaps
                RecalculateDangerMaps(entity, $"Initial {entity.species} spawned");
            }
        }
        // set spawnable map regions coords
        spawnableCoords = spawnCoords;
    }

    // spawn new plants descendant from current ones
    public void SpawnOffspring(LivingEntity entity)
    {
        Debug.Log($"Trying to spawn new offspring for living entity of species: {entity.species}");
        // Get new offspring spawn position
        Coord? coord = ChooseReproductionSpace(entity.coord, entity.offspringSpawnRadious);

        // if there are no spawnable map regions left
        if (!coord.HasValue) {
            // Debug.LogWarning($"No space to spawn new entites!");
            return;
        }

        // create new entity offspring
        var offspring = entity.GetOffspring();
        // initialize offspring coordinates/position
        offspring.Init(coord.Value);
        // register spawning new offspring
        RegisterSpawn(offspring, coord.Value);
        Debug.Log($"Spawned new offspring for living entity of species: {entity.species}");
    }


    void LogPredatorPreyRelationships () {
        int numSpecies = System.Enum.GetNames (typeof (Species)).Length;
        for (int i = 0; i < numSpecies; i++) {
            string s = "(" + System.Enum.GetNames (typeof (Species)) [i] + ") ";
            int enumVal = 1 << i;
            var prey = preyBySpecies[(Species) enumVal];
            var predators = predatorsBySpecies[(Species) enumVal];

            s += "Prey: " + ((prey.Count == 0) ? "None" : "");
            for (int j = 0; j < prey.Count; j++) {
                s += prey[j];
                if (j != prey.Count - 1) {
                    s += ", ";
                }
            }

            s += " | Predators: " + ((predators.Count == 0) ? "None" : "");
            for (int j = 0; j < predators.Count; j++) {
                s += predators[j];
                if (j != predators.Count - 1) {
                    s += ", ";
                }
            }
            print (s);
        }
    }

    [System.Serializable]
    public struct Population {
        public LivingEntity prefab;
        public int count;
    }

    private void OnDrawGizmosSelected()
    {
        // indicate animal current state
        foreach (Species species in speciesMaps.Keys)
        {
            if (species == Species.Plant || species == Species.Undefined)
                continue;


            List<LivingEntity>[,] map = speciesMaps[species].map;

            float stateGizmoColorAlpha = 0.8f;
            float stateGizmoRadious = 0.3f;
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    foreach (LivingEntity entity in map[i, j])
                    {
                        Animal animal = (Animal)entity;

                        if (animal.currentAction == CreatureAction.Reproducing)
                        {
                            Gizmos.color = Color.green * new Color(1f, 1f, 1f, stateGizmoColorAlpha);
                        } else if (animal.currentAction == CreatureAction.SearchingForMate) {
                            Gizmos.color = Color.magenta * new Color(1f, 1f, 1f, stateGizmoColorAlpha);
                        }
                        else
                        {
                            Gizmos.color = Color.blue * new Color(1f, 1f, 1f, stateGizmoColorAlpha);
                        }

                        Gizmos.DrawSphere(animal.transform.position + new Vector3(0f, 1f, 0f), stateGizmoRadious);
                        Gizmos.color *= new Color(1f, 1f, 1f, 1f);
                        Gizmos.DrawWireSphere(animal.transform.position + new Vector3(0f, 1f, 0f), stateGizmoRadious);
                    }
                }
            }
        }

        // draw gizmo lines between animals that are current mates and going to each other
        foreach (Species species in speciesMaps.Keys)
        {
            // draw this gizmo only for foxes and rabbits
            if (species != Species.Rabbit ||
                species != Species.Fox)
                continue;

            List<LivingEntity> alreadyConsidered = new List<LivingEntity>(); 
            List<LivingEntity>[,] map = speciesMaps[species].map;
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    foreach (LivingEntity entity in map[i,j])
                    {
                        Animal animal = (Animal)entity;
                        Animal? currMate = animal.GetCurrMate();
                        if (currMate != null && !alreadyConsidered.Contains(animal))
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawLine(animal.transform.position, Environment.tileCentres[currMate.coord.x, currMate.coord.y]);
                            alreadyConsidered.Add(animal);
                            alreadyConsidered.Add(currMate);
                        }
                    }
                }
            }
        }

        // draw danger map for Rabbits
        foreach (Species species in safetyMapBySpecies.Keys)
        {
            if (species != Species.Rabbit)
                continue;

            List<float> flattened = safetyMapBySpecies[species].Cast<float>().ToList();
            float maxSafetyScore = flattened.Max();
            // consider the lowest safety score 1f even when it is not
            if (maxSafetyScore <= 0)
            {
                maxSafetyScore = 1f;
            }

            for (int i = 0; i < safetyMapBySpecies[species].GetLength(0); i++)
            {
                for (int j = 0; j < safetyMapBySpecies[species].GetLength(1); j++)
                {

                    float safetyValue = 1f * Math.Abs(safetyMapBySpecies[species][i, j]);
                    if (maxSafetyScore > .0f)
                    {
                        safetyValue /= maxSafetyScore;
                        float interpoler = Mathf.Clamp01(safetyValue);
                        Gizmos.color = Color.Lerp(Color.red, Color.white, interpoler) * new Color(1f, 1f, 1f, 1f);
                        float cubeSize = 1f;                        

                        if (walkable[i, j])
                        {
                            Gizmos.DrawCube(Environment.tileCentres[i, j], new Vector3(cubeSize, 0.1f, cubeSize));
                        }
                        else
                        {
                            Gizmos.DrawWireCube(Environment.tileCentres[i, j], new Vector3(cubeSize, 0.1f, cubeSize));
                        }
                    }
               
                }
            }
        }
    }
}