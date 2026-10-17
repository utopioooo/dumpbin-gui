using DumpbinGui.Infrastructure;
var inspector = new FileInspector();
string binary=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"kernel32.dll");
var result=inspector.Inspect(binary);
if(result.Sections.Count==0 || result.Resources.Count==0) throw new Exception("PE sections/resources missing");
foreach(var resource in result.Resources) if(inspector.ReadBytes(binary,resource.FileOffset,resource.Size).Length!=resource.Size) throw new Exception("Resource range mismatch");
try { inspector.ReadBytes(binary,int.MaxValue,100); throw new Exception("Expected invalid range rejection"); } catch(InvalidDataException) {}
string temp=Path.GetTempFileName();
try { File.WriteAllBytes(temp,System.Text.Encoding.ASCII.GetBytes("!<arch>\n")); if(!inspector.Inspect(temp).Kind.Contains("library")) throw new Exception("Archive not detected"); File.WriteAllBytes(temp,[1,2]); try { inspector.Inspect(temp); throw new Exception("Expected truncated file rejection"); } catch(EndOfStreamException) {} } finally { File.Delete(temp); }
Console.WriteLine($"PASS: PE sections={result.Sections.Count}, resources={result.Resources.Count}; resource reads; invalid ranges; archive detection; truncated input");

var parsed=OutputParser.Parse("Section contains the following exports\n ordinal hint RVA name\n 1 0 00001000 Example\nSection contains the following imports\n KERNEL32.dll\n 123 ExitProcess\nFILE HEADER VALUES\n 8664  machine (x64)");
if(parsed.Single(x=>x.Name.Contains("Exports")).Rows.Count!=1 || parsed.Single(x=>x.Name.Contains("Imports")).Rows.Count!=1 || parsed.Single(x=>x.Name.Contains("Dependencies")).Rows.Count!=1) throw new Exception("Structured output parsing failed");
Console.WriteLine("PASS: structured exports/imports/dependencies");
if(OutputParser.Parse("").Count!=0 || OutputParser.Parse("FILE HEADER VALUES\n 8664  machine (x64)").Any(c=>c.Rows.Count==0)) throw new Exception("Empty categories must be hidden");

var emptyData=new System.Data.DataTable();
emptyData.Columns.Add("Empty"); emptyData.Columns.Add("Value");
emptyData.Rows.Add(DBNull.Value," "); emptyData.Rows.Add("\t","0"); emptyData.Rows.Add("", "00000000");
ResultValidation.RemoveEmptyData(emptyData);
if(emptyData.Rows.Count!=2 || emptyData.Columns.Count!=1 || emptyData.Columns[0].ColumnName!="Value") throw new Exception("Empty data validation failed");
if(!ResultValidation.HasValue(0) || !ResultValidation.HasValue(false) || ResultValidation.HasValue(DBNull.Value) || ResultValidation.HasValue("\t ")) throw new Exception("Valid zero/boolean values must remain");
Console.WriteLine("PASS: empty rows/columns removed; zero/address/boolean values preserved");

using(var archive=new MemoryStream()) {
void Write(string text)=>archive.Write(System.Text.Encoding.ASCII.GetBytes(text));
void Member(string name,string contents) { Write(name.PadRight(16)+"0".PadRight(12)+"0".PadRight(6)+"0".PadRight(6)+"0".PadRight(8)+contents.Length.ToString().PadRight(10)+"`\n"); Write(contents); if((contents.Length&1)!=0) Write("\n"); }
Write("!<arch>\n"); Member("/","index"); Member("//","very_long_object_name.obj\0"); Member("a.obj/","abc"); Member("/0","data");
var members=ArchiveReader.Read(archive);
if(members.Count!=2 || members[0].Name!="a.obj" || members[1].Name!="very_long_object_name.obj" || members[0].Size!=3) throw new Exception("LIB member parsing failed");
Console.WriteLine("PASS: LIB short/long names, odd alignment and index filtering");
}

var libFunctions=OutputParser.Parse("Archive member name at 100: sample.obj\nCOFF SYMBOL TABLE\n000 00000000 SECT1 notype () External | ?Example@@YAXXZ\n001 00000000 UNDEF notype () External | _Other\n002 00000000 SECT2 notype External | _data");
var funcs=libFunctions.Single(c=>c.Name.Contains("Functions")).Rows;
if(funcs.Count!=2 || (string)funcs[0]["Member"]!="sample.obj" || (string)funcs[1]["Definition"]!="Reference") throw new Exception("LIB functions parsing failed");
if(ResultValidation.HasValue("\0\u200b \t") || OutputParser.Parse("\u200b\0").Count!=0) throw new Exception("Invisible strings must be empty");
Console.WriteLine("PASS: LIB functions and invisible string validation");

var emptySections=OutputParser.Parse("COFF SYMBOL TABLE\n 00000000 number of symbols\n-----\nRELOCATIONS #1\nAddress Type Applied To Symbol Index\n 0 relocations\nSummary\n none\nFILE HEADER VALUES\n 0  time date stamp");
if(emptySections.Any(c=>c.Name.StartsWith("COFF") || c.Name.StartsWith("RELOCATIONS") || c.Name=="Summary")) throw new Exception("Empty section placeholders leaked");
if(!emptySections.Any(c=>c.Name=="FILE HEADER VALUES")) throw new Exception("Valid zero field removed");
Console.WriteLine("PASS: empty headings/separators/counts hidden; valid zero fields retained");
