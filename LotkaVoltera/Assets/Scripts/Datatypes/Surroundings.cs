using System.Collections.Generic;

public class Surroundings {
    public Coord nearestWaterTile;
    public LivingEntity nearestFoodSource;
    // TODO: calculate suitable mates
    public List<Coord> moveDestinations;
    // TODO: calculate nearest danger
    public List<Animal> dangersInView;
}