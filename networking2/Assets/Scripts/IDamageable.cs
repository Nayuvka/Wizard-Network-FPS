
using UnityEngine;

public enum DamageType
{
    Normal,
    Fire,
    Frost,
    Lightning
}

public interface IDamageable
{
    void TakeDamage(float amount, Vector3 sourcePosition = default, ulong attackerId = ulong.MaxValue, DamageType damageType = DamageType.Normal);
}