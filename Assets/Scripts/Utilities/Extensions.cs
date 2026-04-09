using System;
using System.Collections.Generic;
using UnityEngine;

namespace JungleVoodoo.Utilities
{
    public static class Extensions
    {
        // ── Number Formatting ─────────────────────────────────────────────────

        /// <summary>Format a resource amount with K/M/B suffix. e.g. 1500 → "1.5K"</summary>
        public static string ToResourceString(long value)
        {
            return value switch
            {
                >= 1_000_000_000 => $"{value / 1_000_000_000.0:0.#}B",
                >= 1_000_000     => $"{value / 1_000_000.0:0.#}M",
                >= 1_000         => $"{value / 1_000.0:0.#}K",
                _                => value.ToString()
            };
        }

        /// <summary>Format seconds into a human-readable timer string. e.g. 3661 → "1h 1m 1s"</summary>
        public static string ToTimeString(float totalSeconds)
        {
            if (totalSeconds <= 0) return "Done";

            var ts = TimeSpan.FromSeconds(totalSeconds);

            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m {ts.Seconds}s";

            return $"{ts.Seconds}s";
        }

        // ── Dictionary ────────────────────────────────────────────────────────

        public static TValue GetOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dict,
            TKey key,
            TValue defaultValue = default)
        {
            return dict.TryGetValue(key, out var val) ? val : defaultValue;
        }

        // ── Transform ─────────────────────────────────────────────────────────

        public static void DestroyChildren(this Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }

        public static void SetActiveChildren(this Transform parent, bool active)
        {
            foreach (Transform child in parent)
                child.gameObject.SetActive(active);
        }

        // ── Color ─────────────────────────────────────────────────────────────

        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        // ── GameObject ────────────────────────────────────────────────────────

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            return go.TryGetComponent<T>(out var comp) ? comp : go.AddComponent<T>();
        }
    }
}
