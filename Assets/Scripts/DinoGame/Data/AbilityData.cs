using UnityEngine;

namespace DinoGame.Data
{
    [CreateAssetMenu(menuName = "Dino Game/Data/Ability", fileName = "AbilityData")]
    public sealed class AbilityData : ScriptableObject
    {
        public string abilityId = "bite";
        public string displayName = "Bite";
        [Min(0f)] public float damage = 15f;
        [Min(0f)] public float range = 2.2f;
        [Min(0f)] public float cooldown = 1.25f;
        public AudioClip sound;
        public GameObject vfxPrefab;
    }
}
