using UnityEngine;

namespace DinoGame.Data
{
    [CreateAssetMenu(menuName = "Dino Game/Data/Audio Config", fileName = "AudioConfig")]
    public sealed class AudioConfig : ScriptableObject
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.7f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        public AudioClip[] roars;
        public AudioClip[] footsteps;
    }
}
