using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile", menuName = "Wand/Projectile")]
public class ProjectileData : ScriptableObject
{   
    public enum projectileType{
        singleBlast,
        burst,
        rapid,
    }

    public projectileType type;

    [Header("General Projectile Settings")]
    public GameObject projectilePrefab;
    public GameObject hitEffect;
    public float speed;
    public float damage;
    public float lifetime;
    public Sprite projectileSprite;

    [Header("Burst Settings")]
    public int burstCount = 3;
    public float spreadAngle = 15f;

    [Header("Rapid Settings")]
    public float fireRate = 0.1f;


}
