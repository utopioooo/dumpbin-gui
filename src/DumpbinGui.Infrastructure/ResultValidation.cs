using System.Data;
namespace DumpbinGui.Infrastructure;

public static class ResultValidation
{
    public static bool HasValue(object? value) => value is not null && value is not DBNull &&
        (value is not string text || !string.IsNullOrWhiteSpace(text));

    public static void RemoveEmptyData(DataTable table)
    {
        foreach (var row in table.Rows.Cast<DataRow>().Where(row => !row.ItemArray.Any(HasValue)).ToArray())
            table.Rows.Remove(row);
        foreach (var column in table.Columns.Cast<DataColumn>()
                     .Where(column => !table.Rows.Cast<DataRow>().Any(row => HasValue(row[column]))).ToArray())
            table.Columns.Remove(column);
    }
}
