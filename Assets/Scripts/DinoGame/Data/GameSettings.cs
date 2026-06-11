using UnityEngine;

namespace DinoGame.Data
{
    [CreateAssetMenu(menuName = "Dino Game/Data/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettings : ScriptableObject
    {
        [Range(0.1f, 3f)] public float difficulty = 1f;
        [Range(0.25f, 2f)] public float timeScale = 1f;
        public bool mobileMode = true;
    }
}
