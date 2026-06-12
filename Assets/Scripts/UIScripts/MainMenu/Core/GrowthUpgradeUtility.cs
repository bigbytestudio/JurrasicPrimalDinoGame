using System;
using DinoGame.Data;

namespace DinoGame.Data
{
    public enum GrowthUpgradeResult
    {
        Success,
        InsufficientDna,
        MaxLevel,
        Invalid
    }

    public static class GrowthUpgradeUtility
    {
        private static readonly string[] DefaultStageLabels = { "Juvenile", "Teen", "Adult", "Elder" };

        public static event Action GrowthLevelChanged;

        public static int GetGrowthLevel(CreatureProfile profile)
        {
            if (profile == null)
                return 1;

            GameDataSave data = GameDataSave.Instance;
            return data != null
                ? data.GetCreatureGrowthLevel(profile.creatureId, profile.growthLevel)
                : profile.growthLevel;
        }

        public static int GetUpgradeCost(CreatureProfile profile)
        {
            return profile != null ? profile.growthUpgradeDnaCost : 0;
        }

        public static bool IsMaxGrowth(CreatureProfile profile)
        {
            if (profile == null)
                return true;

            return GetGrowthLevel(profile) >= profile.maxGrowthLevel;
        }

        public static bool CanAffordUpgrade(CreatureProfile profile)
        {
            if (profile == null || IsMaxGrowth(profile))
                return false;

            GameDataSave data = GameDataSave.Instance;
            return data != null && data.dnaCurrency >= GetUpgradeCost(profile);
        }

        public static GrowthUpgradeResult TryUpgrade(CreatureProfile profile)
        {
            if (profile == null)
                return GrowthUpgradeResult.Invalid;

            if (IsMaxGrowth(profile))
                return GrowthUpgradeResult.MaxLevel;

            GameDataSave data = GameDataSave.Instance;
            if (data == null)
                return GrowthUpgradeResult.Invalid;

            int cost = GetUpgradeCost(profile);
            if (data.dnaCurrency < cost)
                return GrowthUpgradeResult.InsufficientDna;

            data.AddDnaCurrency(-cost);
            data.SetCreatureGrowthLevel(profile.creatureId, GetGrowthLevel(profile) + 1);
            GrowthLevelChanged?.Invoke();
            return GrowthUpgradeResult.Success;
        }

        public static string GetStageLabel(CreatureProfile profile)
        {
            if (profile == null)
                return string.Empty;

            return GetStageLabelForLevel(GetGrowthLevel(profile), profile.growthStageLabel);
        }

        public static string GetStageLabelForLevel(int level, string fallback = null)
        {
            if (level >= 1 && level <= DefaultStageLabels.Length)
                return DefaultStageLabels[level - 1];

            return string.IsNullOrWhiteSpace(fallback) ? $"Stage {level}" : fallback;
        }
    }
}
