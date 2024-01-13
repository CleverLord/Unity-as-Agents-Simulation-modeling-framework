using System.Collections.Generic;

public class Surroundings {
    public Coord nearestWaterTile;
    public LivingEntity nearestFoodSource;
    // TODO: implement nearest danger
    public List<Coord> moveDestinations;
    public List<Animal> dangersInView;
}