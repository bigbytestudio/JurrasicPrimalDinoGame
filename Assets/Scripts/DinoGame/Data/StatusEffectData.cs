using UnityEngine;

namespace DinoGame.Data
{
    public enum StatusEffectType { Bleed, Stun, Poison, Rage, Slow, Burn }

    [CreateAssetMenu(menuName = "Dino Game/Data/Status Effect", fileName = "StatusEffectData")]
    public sealed class StatusEffectData : ScriptableObject
    {
        public StatusEffectType type = StatusEffectType.Bleed;
        [Min(0f)] public float duration = 3f;
        [Min(0f)] public float tickRate = 1f;
        [Min(0f)] public float tickDamage = 0f;
        [Range(0.05f, 2f)] public float movementMultiplier = 1f;
        public GameObject vfxPrefab;
        public AudioClip sfx;
    }
}
