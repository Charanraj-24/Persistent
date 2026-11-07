using System.Globalization;
using System.Text;

namespace Reflect.Web.Services.Art;

/// <summary>
/// DEMO ONLY. Draws a deterministic, abstract "watercolor" SVG for a session so the artwork timeline has something to show.
/// In production the image would be the client's real artwork, shared from Reflect with the client's consent.
/// Output uses presentation attributes only (no style attributes), so it is safe under a strict CSP.
/// </summary>
public static class SyntheticArtworkRenderer
{
    private static readonly Dictionary<string, string[]> Palettes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["calm"] = new[] { "#6F8FAF", "#9DB4C8", "#C9D6E2", "#5B7A99" },
        ["relaxed"] = new[] { "#7FA6A3", "#A9C7C2", "#D7E5E1", "#5E8C88" },
        ["peaceful"] = new[] { "#8FA9C9", "#B7C9DD", "#E1E8F0", "#6C88AB" },
        ["hopeful"] = new[] { "#F2A65A", "#F7C989", "#FBE3BE", "#E98A6A" },
        ["grateful"] = new[] { "#F09C7F", "#F6C1A6", "#FBE0CF", "#E9B44C" },
        ["proud"] = new[] { "#E9B44C", "#F3D38A", "#F9EBC6", "#E98A6A" },
        ["sad"] = new[] { "#3E4A7A", "#6B76A3", "#A3AAC6", "#8A93A6" },
        ["grief"] = new[] { "#2F3A66", "#57628F", "#8E95B5", "#B9BCC8" },
        ["anxious"] = new[] { "#E5735C", "#F0A08E", "#F7CFC4", "#C65A4A" },
        ["angry"] = new[] { "#C8473B", "#E5735C", "#F2B0A2", "#8E2F2A" },
        ["overwhelmed"] = new[] { "#B5506A", "#D98595", "#EEC0C8", "#6B4E7A" },
        ["curious"] = new[] { "#5FA8A0", "#94C9C1", "#CDE7E2", "#E9B44C" },
    };
    private static readonly string[] Default = { "#7B6FA8", "#A9A0CB", "#D6D1E8", "#E98A6A" };

    public static string Render(Guid seedId, IEnumerable<string> emotionTags, string idPrefix)
    {
        var tags = emotionTags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList();
        var colors = tags.SelectMany(t => Palettes.TryGetValue(t, out var p) ? p : Array.Empty<string>()).ToList();
        if (colors.Count == 0) colors.AddRange(Default);

        var rng = new Random(BitConverter.ToInt32(seedId.ToByteArray(), 0));
        string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
        var fid = idPrefix + "-wc";

        var sb = new StringBuilder();
        sb.Append($"<svg viewBox=\"0 0 160 120\" xmlns=\"http://www.w3.org/2000/svg\" preserveAspectRatio=\"xMidYMid slice\" class=\"rpd-art-svg\" aria-hidden=\"true\" focusable=\"false\">");
        sb.Append($"<defs><filter id=\"{fid}\" x=\"-20%\" y=\"-20%\" width=\"140%\" height=\"140%\">");
        sb.Append($"<feTurbulence type=\"fractalNoise\" baseFrequency=\"{F(0.02 + rng.NextDouble() * 0.02)}\" numOctaves=\"3\" seed=\"{rng.Next(1, 999)}\"/>");
        sb.Append($"<feDisplacementMap in=\"SourceGraphic\" scale=\"{F(10 + rng.NextDouble() * 10)}\"/><feGaussianBlur stdDeviation=\"1.2\"/></filter></defs>");
        sb.Append("<rect width=\"160\" height=\"120\" fill=\"#FBF8F4\"/>");

        var blobs = 6 + rng.Next(0, 4);
        for (var b = 0; b < blobs; b++)
        {
            var cx = 15 + rng.NextDouble() * 130;
            var cy = 12 + rng.NextDouble() * 96;
            var r = 14 + rng.NextDouble() * 30;
            var color = colors[rng.Next(colors.Count)];
            var opacity = 0.28 + rng.NextDouble() * 0.35;
            sb.Append($"<path d=\"{BlobPath(rng, cx, cy, r, F)}\" fill=\"{color}\" fill-opacity=\"{opacity.ToString("0.00", CultureInfo.InvariantCulture)}\" filter=\"url(#{fid})\"/>");
        }

        // A few brush strokes on top.
        var strokes = 2 + rng.Next(0, 3);
        for (var s = 0; s < strokes; s++)
        {
            var x1 = rng.NextDouble() * 160; var y1 = rng.NextDouble() * 120;
            var x2 = rng.NextDouble() * 160; var y2 = rng.NextDouble() * 120;
            var qx = rng.NextDouble() * 160; var qy = rng.NextDouble() * 120;
            var color = colors[rng.Next(colors.Count)];
            sb.Append($"<path d=\"M{F(x1)} {F(y1)} Q{F(qx)} {F(qy)} {F(x2)} {F(y2)}\" stroke=\"{color}\" stroke-width=\"{F(1.5 + rng.NextDouble() * 3)}\" stroke-linecap=\"round\" fill=\"none\" stroke-opacity=\"0.7\" filter=\"url(#{fid})\"/>");
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string BlobPath(Random rng, double cx, double cy, double r, Func<double, string> f)
    {
        const int n = 7;
        var pts = Enumerable.Range(0, n).Select(i =>
        {
            var a = Math.PI * 2 * i / n;
            var rr = r * (0.7 + rng.NextDouble() * 0.5);
            return (X: cx + Math.Cos(a) * rr, Y: cy + Math.Sin(a) * rr);
        }).ToList();
        // Closed smooth curve through midpoints (quadratic Bezier).
        (double X, double Y) Mid((double X, double Y) a, (double X, double Y) b) => ((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        var sb = new StringBuilder();
        var start = Mid(pts[^1], pts[0]);
        sb.Append($"M{f(start.X)} {f(start.Y)}");
        for (var i = 0; i < n; i++)
        {
            var m = Mid(pts[i], pts[(i + 1) % n]);
            sb.Append($" Q{f(pts[i].X)} {f(pts[i].Y)} {f(m.X)} {f(m.Y)}");
        }
        return sb.Append('Z').ToString();
    }
}
