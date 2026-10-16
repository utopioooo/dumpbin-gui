using System.Reflection.PortableExecutable;
using System.Text;
using DumpbinGui.Core;
namespace DumpbinGui.Infrastructure;
public sealed class FileInspector : IFileInspector {
 public byte[] ReadBytes(string path,int offset,int size) { using var f=File.OpenRead(path); if(offset<0 || size<0 || (long)offset+size>f.Length) throw new InvalidDataException("Invalid file range"); f.Position=offset; var b=new byte[size]; f.ReadExactly(b); return b; }
 public FileInspection Inspect(string path) {
 using var f=File.OpenRead(path); var magic=new byte[8]; f.ReadExactly(magic); f.Position=0;
 if(Encoding.ASCII.GetString(magic)=="!<arch>\n") return new("COFF library (static/import)",[],[]) { Members=ArchiveReader.Read(f) };
 using var pe=new PEReader(f); var h=pe.PEHeaders;
 var sections=h.SectionHeaders.Select(s=>new SectionInfo(s.Name,s.VirtualAddress,s.VirtualSize,s.PointerToRawData,s.SizeOfRawData,s.SectionCharacteristics.ToString())).ToArray();
 var resources=new List<ResourceInfo>();
 if(h.PEHeader is {} header && header.ResourceTableDirectory.RelativeVirtualAddress!=0) {
 var data=File.ReadAllBytes(path);
 int Map(int rva) { foreach(var s in sections) if(rva>=s.VirtualAddress && (long)rva<s.VirtualAddress+Math.Max(s.VirtualSize,s.Size)) return checked(s.FileOffset+rva-s.VirtualAddress); throw new InvalidDataException("Invalid resource address"); }
 int root=Map(header.ResourceTableDirectory.RelativeVirtualAddress);
 int limit=checked(root+header.ResourceTableDirectory.Size);
 void Check(int p,int size) { if(p<root || size<0 || (long)p+size>Math.Min(limit,data.Length)) throw new InvalidDataException("Invalid resource directory"); }
 ushort U16(int p) { Check(p,2); return BitConverter.ToUInt16(data,p); }
 uint U32(int p) { Check(p,4); return BitConverter.ToUInt32(data,p); }
 var visited=new HashSet<int>();
 void Walk(int relative,string prefix,int depth) {
 if(depth>8 || !visited.Add(relative)) return;
 int p=checked(root+relative); int count=U16(p+12)+U16(p+14);
 Check(p+16,checked(count*8));
 for(int i=0;i<count;i++) {
 int entry=p+16+i*8; uint name=U32(entry), target=U32(entry+4); string label;
 if((name&0x80000000)!=0) { int n=checked(root+(int)(name&0x7fffffff)); int length=U16(n)*2; Check(n+2,length); label=Encoding.Unicode.GetString(data,n+2,length); } else label=name.ToString();
 string key=prefix.Length==0?label:prefix+"/"+label;
 if((target&0x80000000)!=0) Walk((int)(target&0x7fffffff),key,depth+1);
 else { int d=checked(root+(int)target); int offset=Map(checked((int)U32(d))); int size=checked((int)U32(d+4)); if((long)offset+size>data.Length) throw new InvalidDataException("Invalid resource data"); resources.Add(new(key,offset,size)); }
 }
 }
 Walk(0,"",0);
 }
 return new(h.PEHeader is null?"COFF object":"PE / "+h.CoffHeader.Machine,sections,resources);
 }
}
