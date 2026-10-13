using System.Text;

namespace Reflect.Web.Models.ViewModels.Provider.Shared;

/// <summary>Minimal RFC 4180 CSV writer. Prefixes formula-looking cells so spreadsheets don't execute them.</summary>
public sealed class CsvBuilder
{
    private readonly StringBuilder _sb = new();

    public CsvBuilder Row(params object[] cells)
    {
        _sb.AppendLine(string.Join(",", cells.Select(Escape)));
        return this;
    }

    public override string ToString() => _sb.ToString();

    private static string Escape(object cell)
    {
        var text = Convert.ToString(cell, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        if (text.Length > 0 && "=+-@".Contains(text[0]) && !double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
            text = "'" + text;
        return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
    }
}
