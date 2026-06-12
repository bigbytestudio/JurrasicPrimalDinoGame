using System;
using System.Collections.Generic;
using UnityEngine;

namespace DinoGame.UI.Menu
{
    public static class StoreIapCardBinder
    {
        public static void BindPanel(Transform panelRoot, IReadOnlyList<StoreIapOffer> offers)
        {
            if (panelRoot == null)
                return;

            List<RectTransform> cards = CollectSortedStoreCards(panelRoot);
            if (cards.Count == 0)
                return;

            for (int i = 0; i < cards.Count; i++)
            {
                RectTransform card = cards[i];
                if (card == null)
                    continue;

                StoreIapOffer offer = ResolveOffer(card.name, i, offers);
                StoreIapCardView view = card.GetComponent<StoreIapCardView>();
                if (view == null)
                    view = card.gameObject.AddComponent<StoreIapCardView>();

                view.Apply(offer);
            }
        }

        private static StoreIapOffer ResolveOffer(string cardName, int index, IReadOnlyList<StoreIapOffer> offers)
        {
            if (offers == null || offers.Count == 0)
                return default;

            if (!string.IsNullOrWhiteSpace(cardName))
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    if (string.Equals(offers[i].cardId, cardName, StringComparison.OrdinalIgnoreCase))
                        return offers[i];
                }
            }

            return index < offers.Count ? offers[index] : default;
        }

        private static List<RectTransform> CollectSortedStoreCards(Transform panelRoot)
        {
            List<RectTransform> cards = UIDominoTween.CollectStoreCards(panelRoot);
            cards.Sort(CompareCardOrder);
            return cards;
        }

        private static int CompareCardOrder(RectTransform left, RectTransform right)
        {
            int leftOrder = ExtractCardOrder(left != null ? left.name : string.Empty);
            int rightOrder = ExtractCardOrder(right != null ? right.name : string.Empty);

            int byOrder = leftOrder.CompareTo(rightOrder);
            if (byOrder != 0)
                return byOrder;

            return string.Compare(left?.name, right?.name, StringComparison.Ordinal);
        }

        private static int ExtractCardOrder(string cardName)
        {
            if (string.IsNullOrWhiteSpace(cardName))
                return int.MaxValue;

            int underscore = cardName.LastIndexOf('_');
            if (underscore < 0 || underscore >= cardName.Length - 1)
                return int.MaxValue;

            return int.TryParse(cardName[(underscore + 1)..], out int order)
                ? order
                : int.MaxValue;
        }
    }
}
