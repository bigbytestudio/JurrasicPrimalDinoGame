using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public static class UIFillBarAnimator
    {
        public static void Animate(
            MonoBehaviour host,
            ICollection<Coroutine> bucket,
            Image fillBar,
            TMP_Text percentText,
            float targetFill,
            int targetPercent,
            float delay,
            float duration)
        {
            if (host == null || bucket == null)
                return;

            if (fillBar == null && percentText == null)
                return;

            if (fillBar != null)
            {
                fillBar.type = Image.Type.Filled;
                fillBar.fillAmount = 0f;
            }

            if (percentText != null)
                percentText.text = FormatPercent(0);

            bucket.Add(host.StartCoroutine(AnimateRoutine(fillBar, percentText, targetFill, targetPercent, delay, duration)));
        }

        public static void StopAll(ICollection<Coroutine> bucket, MonoBehaviour host)
        {
            if (bucket == null || host == null)
                return;

            foreach (Coroutine routine in bucket)
            {
                if (routine != null)
                    host.StopCoroutine(routine);
            }

            bucket.Clear();
        }

        private static IEnumerator AnimateRoutine(
            Image fillBar,
            TMP_Text percentText,
            float targetFill,
            int targetPercent,
            float delay,
            float duration)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            targetFill = Mathf.Clamp01(targetFill);
            targetPercent = Mathf.Clamp(targetPercent, 0, 100);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);

                if (fillBar != null)
                    fillBar.fillAmount = Mathf.Lerp(0f, targetFill, t);

                if (percentText != null)
                    percentText.text = FormatPercent(Mathf.RoundToInt(Mathf.Lerp(0f, targetPercent, t)));

                yield return null;
            }

            if (fillBar != null)
                fillBar.fillAmount = targetFill;

            if (percentText != null)
                percentText.text = FormatPercent(targetPercent);
        }

        private static string FormatPercent(int value) => $"{value} %";
    }
}
