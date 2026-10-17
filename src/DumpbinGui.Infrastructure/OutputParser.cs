using System.Data;
using System.Text.RegularExpressions;
namespace DumpbinGui.Infrastructure;

public sealed record ResultCategory(string Name, DataView Rows);
public static class OutputParser
{
    public static IReadOnlyList<ResultCategory> Parse(string output)
    {
        var tables = new Dictionary<string, DataTable>();
        DataTable Table(string name, params string[] columns) {
            if(tables.TryGetValue(name,out var existing)) return existing;
            var table=new DataTable(name); foreach(var column in columns) table.Columns.Add(column);
            tables.Add(name,table); return table;
        }
        var exports=Table("导出 / Exports","Ordinal","Hint","RVA","Name");
        var imports=Table("导入 / Imports","Module","Address / Hint","Name");
        var dependencies=Table("依赖 / Dependencies","Module");
        var functions=Table("函数 / Functions","Member","Name","Value","Section","Linkage","Definition");
        var publicSymbols=Table("库公开符号 / Library symbols","Member offset","Name");
        string member=""; bool linkerMembers=false;
        string category="文件信息 / File", module=""; bool importing=false, exporting=false;
        var modules=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(string raw in output.Split('\n')) {
            string line=raw.Trim(); if(line.Length==0) continue;
            if(!ResultValidation.HasValue(line)) continue;
            if(line.StartsWith("Dump of file") || line.StartsWith("Microsoft") || line.StartsWith("Copyright") || line.StartsWith("Exit code:")) continue;
            if(line.Contains("Section contains the following exports",StringComparison.OrdinalIgnoreCase)) { exporting=true; importing=false; category="导出元数据 / Export metadata"; continue; }
            if(line.Contains("Section contains the following imports",StringComparison.OrdinalIgnoreCase) || line.Contains("delay load imports",StringComparison.OrdinalIgnoreCase)) { importing=true; exporting=false; module=""; category="导入元数据 / Import metadata"; continue; }
            if(line.StartsWith("Archive member name",StringComparison.OrdinalIgnoreCase)) {
                member=Regex.Replace(line,@"^Archive member name at [^:]+:\s*", "",RegexOptions.IgnoreCase);
                linkerMembers=false;
            }
            if(line.Contains("public symbols",StringComparison.OrdinalIgnoreCase)) { linkerMembers=true; continue; }
            var function=Regex.Match(line,@"^[0-9A-Fa-f]+\s+([0-9A-Fa-f]+)\s+(\S+)\s+.*?\(\)\s+(\S+)\s+\|\s*(.+)$");
            if(function.Success && ResultValidation.HasValue(function.Groups[4].Value)) {
                functions.Rows.Add(member,function.Groups[4].Value,function.Groups[1].Value,function.Groups[2].Value,function.Groups[3].Value,
                    function.Groups[2].Value.Equals("UNDEF",StringComparison.OrdinalIgnoreCase)?"Reference":"Defined");
                continue;
            }
            if(linkerMembers) {
                var symbol=Regex.Match(line,@"^([0-9A-Fa-f]{4,16})\s+(\S.*)$");
                if(symbol.Success) { publicSymbols.Rows.Add(symbol.Groups[1].Value,symbol.Groups[2].Value); continue; }
            }
            if(Regex.IsMatch(line,@"^(SECTION HEADER #|FILE HEADER VALUES|OPTIONAL HEADER VALUES|COFF SYMBOL TABLE|RELOCATIONS #|RAW DATA #|Summary|Archive member name|LINKER|CLR Header|TLS|Load Configuration|Exception|Debug Directories)",RegexOptions.IgnoreCase)) {
                linkerMembers=false;
                importing=false; exporting=false; category=line; continue;
            }
            if(!ResultValidation.IsContentLine(line)) continue;
            if(exporting) {
                var match=Regex.Match(line,@"^(\d+)\s+([0-9A-Fa-f]+)\s+([0-9A-Fa-f]{8,16})\s+(.+)$");
                if(match.Success) { exports.Rows.Add(match.Groups[1].Value,match.Groups[2].Value,match.Groups[3].Value,match.Groups[4].Value); continue; }
                if(line.StartsWith("ordinal",StringComparison.OrdinalIgnoreCase)) continue;
            }
            if(importing) {
                if(Regex.IsMatch(line,@"^[^\s]+\.(dll|exe|drv|ocx)$",RegexOptions.IgnoreCase)) {
                    module=line; if(modules.Add(module)) dependencies.Rows.Add(module); continue;
                }
                var match=Regex.Match(line,@"^([0-9A-Fa-f]+(?:\s+[0-9A-Fa-f]+)?)\s+([^\s]+)(.*)$");
                if(module.Length>0 && match.Success && !Regex.IsMatch(line,@"(Import Address Table|Import Name Table|time date stamp|Index of first forwarder)",RegexOptions.IgnoreCase)) {
                    imports.Rows.Add(module,match.Groups[1].Value,match.Groups[2].Value+match.Groups[3].Value); continue;
                }
            }
            var fields=Regex.Match(line,@"^(\S+)\s{2,}(.+)$");
            var table=Table(category,"Value / Address","Field / Details");
            if(fields.Success) table.Rows.Add(fields.Groups[1].Value,fields.Groups[2].Value);
            else table.Rows.Add("",line);
        }
        foreach(var table in tables.Values) ResultValidation.RemoveEmptyData(table);
        return tables.Where(t=>t.Value.Rows.Count>0 && t.Value.Columns.Count>0)
            .Select(t=>new ResultCategory(t.Key,t.Value.DefaultView)).ToArray();
    }
}
