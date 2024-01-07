using System;
using UnityEngine;

public class LivingEntity : MonoBehaviour {

    public int colourMaterialIndex;
    public Species species;
    public Material material;

    // Environment reproductive method reference for spawning offspring 
    public Action<LivingEntity> reproduce;
    [Range(1, 10)]
    public float offspringSpawnRadious = 1.5f;
    
    public Coord coord;
    
    [HideInInspector]
    public int mapIndex;
    [HideInInspector]
    public Coord mapCoord;

    protected bool dead;

    public virtual LivingEntity GetOffspring() { return null; }

    public virtual void Init (Coord coord) {
        this.coord = coord;
        transform.position = Environment.tileCentres[coord.x, coord.y];

        // Set material to the instance material
        var meshRenderer = transform.GetComponentInChildren<MeshRenderer> ();
        for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
        {
            if (meshRenderer.sharedMaterials[i] == material) {
                material = meshRenderer.materials[i];
                break;
            }
        }
    }

    protected virtual void Die (CauseOfDeath cause) {
        if (!dead) {
            dead = true;
            Environment.RegisterDeath (this);
            Destroy (gameObject);
        }
    }

}