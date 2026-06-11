using UnityEngine;
using DinoGame.Core;

namespace DinoGame.Systems
{
    public sealed class QuestSystem : MonoBehaviour
    {
        [SerializeField] private int killsRequired = 3;
        [SerializeField] private int rewardCurrency = 50;
        [SerializeField] private EconomySystem economy;
        private int kills;

        private void OnEnable() => GameEventBus.CreatureDied += OnCreatureDied;
        private void OnDisable() => GameEventBus.CreatureDied -= OnCreatureDied;
        private void Awake() => economy ??= FindFirstObjectByType<EconomySystem>();

        private void OnCreatureDied(Creature creature, GameObject source)
        {
            if (source == null || !source.TryGetComponent(out Creature killer) || killer.TeamId != (int)TeamType.Player) return;
            kills++;
            if (kills >= killsRequired)
            {
                economy?.AddCurrency(rewardCurrency);
                kills = 0;
            }
        }
    }
}
