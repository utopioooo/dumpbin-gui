using System.Data;
using System.Text.RegularExpressions;
namespace DumpbinGui.Infrastructure;

public static class ResultValidation
{
    public static bool IsContentLine(string text)
    {
        if (!HasValue(text)) return false;
        text = text.Trim();
        if (Regex.IsMatch(text, @"^[-=_*\s]+$")) return false;
        if (Regex.IsMatch(text, @"^(?:none|<none>|\(none\)|no (?:symbols|imports|exports|relocations|resources)(?: found)?)[.!]?$", RegexOptions.IgnoreCase)) return false;
        if (Regex.IsMatch(text, @"^0+\s+(?:number of\s+)?(?:symbols|imports|exports|relocations|resources|entries|functions|public symbols)(?:\s.*)?$", RegexOptions.IgnoreCase)) return false;
        if (Regex.IsMatch(text, @"^(?:ordinal\s+hint\s+RVA\s+name|Address\s+Type\s+Applied To\s+Symbol.*|Symbol name|Offset\s+Name|Index\s+Name)$", RegexOptions.IgnoreCase)) return false;
        return true;
    }
    public static bool HasValue(object? value) => value is not null && value is not DBNull &&
        (value is not string text || text.Any(c => !char.IsWhiteSpace(c) && !char.IsControl(c) &&
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.Format));

    public static void RemoveEmptyData(DataTable table)
    {
        foreach (var row in table.Rows.Cast<DataRow>().Where(row => !row.ItemArray.Any(HasValue)).ToArray())
            table.Rows.Remove(row);
        foreach (var column in table.Columns.Cast<DataColumn>()
                     .Where(column => !table.Rows.Cast<DataRow>().Any(row => HasValue(row[column]))).ToArray())
            table.Columns.Remove(column);
    }
}
