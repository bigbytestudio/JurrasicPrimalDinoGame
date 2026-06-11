using System;
using UnityEngine;

namespace DinoGame.Systems
{
    public sealed class EconomySystem : MonoBehaviour
    {
        [SerializeField] private SaveSystem saveSystem;
        public event Action<int> CurrencyChanged;
        public int Currency => saveSystem != null ? saveSystem.SoftCurrency : 0;

        private void Awake() => saveSystem ??= FindFirstObjectByType<SaveSystem>();
        public bool SpendCurrency(int amount)
        {
            if (amount <= 0 || saveSystem == null || saveSystem.SoftCurrency < amount) return false;
            saveSystem.SoftCurrency -= amount;
            CurrencyChanged?.Invoke(saveSystem.SoftCurrency);
            return true;
        }
        public void AddCurrency(int amount)
        {
            if (amount <= 0 || saveSystem == null) return;
            saveSystem.SoftCurrency += amount;
            CurrencyChanged?.Invoke(saveSystem.SoftCurrency);
        }
    }
}
