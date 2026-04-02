using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile", menuName = "Wand/Projectile")]
public class ProjectileData : ScriptableObject
{
    public GameObject projectilePrefab;
    public GameObject hitEffect;
    public float speed;
    public float damage;
    public float lifetime;
    public Sprite projectileSprite;

}
