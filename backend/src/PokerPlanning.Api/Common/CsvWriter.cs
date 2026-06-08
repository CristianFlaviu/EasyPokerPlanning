using System.Text;

namespace PokerPlanning.Api.Common;

// CSV serialization is a presentation concern, so it lives in the API layer.
public static class CsvWriter
{
    // Builds CSV text from a header row and data rows, RFC-4180 escaped, CRLF separated.
    public static string Build(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder();
        AppendRow(sb, header);
        foreach (var row in rows)
            AppendRow(sb, row);
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, IReadOnlyList<string> fields)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(Escape(fields[i]));
        }
        sb.Append("\r\n");
    }

    // Quote fields containing comma, quote, CR or LF; double any embedded quotes.
    private static string Escape(string? field)
    {
        var value = field ?? string.Empty;
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
