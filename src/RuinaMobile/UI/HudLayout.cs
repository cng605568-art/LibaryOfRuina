using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace RuinaMobile
{
    /// <summary>Button positions, stored as screen fractions so they survive resolution changes.</summary>
    internal static class HudLayout
    {
        internal static readonly string[] Ids = { "gear", "key1", "key2", "zin", "zout", "zreset" };

        internal static Dictionary<string, Vector2> Defaults()
        {
            return new Dictionary<string, Vector2>
            {
                { "gear", new Vector2(0.035f, 0.075f) },
                { "key1", new Vector2(0.92f, 0.62f) },
                { "key2", new Vector2(0.955f, 0.30f) },
                { "zin", new Vector2(0.045f, 0.40f) },
                { "zout", new Vector2(0.045f, 0.54f) },
                { "zreset", new Vector2(0.045f, 0.68f) }
            };
        }

        internal static Dictionary<string, Vector2> Parse(string text)
        {
            var result = Defaults();
            if (string.IsNullOrEmpty(text)) return result;

            foreach (var part in text.Split(';'))
            {
                var kv = part.Split(':');
                if (kv.Length != 2) continue;
                var xy = kv[1].Split(',');
                if (xy.Length != 2) continue;

                float x, y;
                if (!float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)) continue;
                if (!float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)) continue;
                result[kv[0].Trim()] = new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
            }

            return result;
        }

        internal static string Serialize(Dictionary<string, Vector2> layout)
        {
            var defaults = Defaults();
            return string.Join(";", Ids.Select(id =>
            {
                Vector2 v;
                if (!layout.TryGetValue(id, out v)) v = defaults[id];
                return id + ":" + v.x.ToString("0.###", CultureInfo.InvariantCulture) + ","
                       + v.y.ToString("0.###", CultureInfo.InvariantCulture);
            }).ToArray());
        }
    }
}
