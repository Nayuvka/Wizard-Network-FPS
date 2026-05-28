using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile", menuName = "Wand/Projectile")]
public class ProjectileData : ScriptableObject
{
    public enum ProjectileType
    {
        Normal,
        Fireball,
        Frostball,
        Lightning
    }

    [Header("Identity")]
    public string projectileName;
    public ProjectileType type;

    [Header("Projectile References")]
    public GameObject projectilePrefab;
    public GameObject hitEffect;

    [Header("Visuals")]
    public Material projectileMaterial;
    public Color projectileColour = Color.white;

    [Header("Core Stats")]
    public float speed = 40f;
    public float damage = 10f;
    public float lifetime = 5f;

    [Header("Combat")]
    public bool homing;
    public bool pierceTargets;
    public int maxPierceCount = 1;

    [Header("Area Effects")]
    public bool useSplashDamage;
    public float splashRadius = 5f;
    public LayerMask splashTargetMask;

    [Header("Fire Settings")]
    public bool applyBurn;
    public float burnDuration = 5f;
    public float burnTickDamage = 5f;

    [Header("Frost Settings")]
    public bool applyFreeze;
    public float freezeDuration = 3f;
    [Range(0f, 1f)]
    public float freezeSlowMultiplier = 0.2f;

    [Header("Lightning Settings")]
    public bool chainLightning;
    public int maxChainTargets = 3;
    public float chainRadius = 5f;

    [Header("Screen Shake")]
    public float cameraShakeForce = 1f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip impactSound;

    [Header("Debug")]
    [TextArea]
    public string developerNotes;
}