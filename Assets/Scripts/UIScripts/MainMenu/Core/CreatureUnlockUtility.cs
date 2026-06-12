using System;
using DinoGame.Data;

namespace DinoGame.Data
{
    public static class CreatureUnlockUtility
    {
        public static event Action UnlockStateChanged;

        public static bool IsUnlocked(CreatureProfile profile)
        {
            if (profile == null)
                return false;

            if (profile.unlockedByDefault)
                return true;

            GameDataSave data = GameDataSave.Instance;
            return data != null && data.IsCreatureUnlocked(profile.creatureId);
        }

        public static bool CanAfford(CreatureProfile profile)
        {
            if (profile == null)
                return false;

            GameDataSave data = GameDataSave.Instance;
            return data != null && data.bonesCurrency >= profile.bonePurchaseCost;
        }

        public static bool TryPurchase(CreatureProfile profile)
        {
            if (profile == null || IsUnlocked(profile))
                return IsUnlocked(profile);

            GameDataSave data = GameDataSave.Instance;
            if (data == null || data.bonesCurrency < profile.bonePurchaseCost)
                return false;

            data.AddBonesCurrency(-profile.bonePurchaseCost);
            data.UnlockCreature(profile.creatureId);
            UnlockStateChanged?.Invoke();
            return true;
        }
    }
}
