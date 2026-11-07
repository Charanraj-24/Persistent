using System.Text.RegularExpressions;

namespace Reflect.Domain.ProviderDashboard.Assistant;

/// <summary>
/// Swaps client names for tokens like [C03] before text goes to an AI service, and swaps them back in the answer.
/// Names, MRNs and birthdates never leave the server.
/// </summary>
public sealed class Pseudonymizer
{
    private readonly List<(string Token, string FullName, string First, string Last)> _people = new();

    public string Register(string token, string firstName, string lastName)
    {
        _people.Add((token, $"{firstName} {lastName}", firstName, lastName));
        return token;
    }

    /// <summary>Real names to tokens (used on prior chat turns typed by the provider or shown to them).</summary>
    public string Apply(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        foreach (var p in _people.OrderByDescending(p => p.FullName.Length))
        {
            text = Regex.Replace(text, Regex.Escape(p.FullName), p.Token, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, Regex.Escape($"{p.Last}, {p.First}"), p.Token, RegexOptions.IgnoreCase);
        }
        // Single names: case-sensitive and whole-word, so ordinary words like "grace" are left alone.
        foreach (var p in _people)
        {
            text = Regex.Replace(text, $@"\b{Regex.Escape(p.First)}\b", p.Token);
            text = Regex.Replace(text, $@"\b{Regex.Escape(p.Last)}\b", p.Token);
        }
        return text;
    }

    /// <summary>Tokens back to real names (used on the AI's answer before the provider sees it).</summary>
    public string Restore(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        foreach (var p in _people)
        {
            var bare = p.Token.Trim('[', ']');
            text = text.Replace(p.Token, p.FullName, StringComparison.Ordinal);
            text = Regex.Replace(text, $@"\b{Regex.Escape(bare)}\b", p.FullName);
        }
        return text;
    }
}
