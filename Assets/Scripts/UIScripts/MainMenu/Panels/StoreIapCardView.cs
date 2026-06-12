using TMPro;
using UnityEngine;

namespace DinoGame.UI.Menu
{
    [DisallowMultipleComponent]
    public sealed class StoreIapCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text headingText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text priceText;

        private void Awake()
        {
            TryAutoBind();
        }

        public void Apply(StoreIapOffer offer, bool hideWhenEmpty = true)
        {
            TryAutoBind();

            if (string.IsNullOrWhiteSpace(offer.heading) && offer.amount <= 0)
            {
                if (hideWhenEmpty)
                    gameObject.SetActive(false);

                return;
            }

            gameObject.SetActive(true);

            if (headingText != null)
                headingText.text = offer.heading ?? string.Empty;

            if (amountText != null)
                amountText.text = FormatAmount(offer.amount);

            if (priceText != null)
                priceText.text = offer.priceDisplay ?? string.Empty;
        }

        private void TryAutoBind()
        {
            Transform root = transform;

            if (headingText == null)
            {
                Transform packageName = root.Find("heading/packageName");
                if (packageName != null)
                    headingText = packageName.GetComponent<TMP_Text>();
            }

            if (amountText == null)
            {
                Transform amountRoot = root.Find("Image");
                if (amountRoot != null)
                    amountText = amountRoot.GetComponentInChildren<TMP_Text>(true);
            }

            if (priceText == null)
            {
                Transform priceRoot = root.Find("priceImg");
                if (priceRoot != null)
                    priceText = priceRoot.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private static string FormatAmount(int amount)
        {
            return Mathf.Max(0, amount).ToString("N0");
        }
    }
}
