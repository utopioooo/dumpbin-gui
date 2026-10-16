using System.Text;
namespace DumpbinGui.Core;
public static class CommandArguments
{
    public static IReadOnlyList<string> Parse(string text)
    {
        var result = new List<string>();
        var value = new StringBuilder();
        bool quoted = false;
        foreach (char character in text)
        {
            if (character == '"') { quoted = !quoted; continue; }
            if (char.IsWhiteSpace(character) && !quoted)
            {
                if (value.Length > 0) { result.Add(value.ToString()); value.Clear(); }
            }
            else value.Append(character);
        }
        if (quoted) throw new ArgumentException("Unclosed quote / 参数引号未闭合");
        if (value.Length > 0) result.Add(value.ToString());
        return result;
    }
}
