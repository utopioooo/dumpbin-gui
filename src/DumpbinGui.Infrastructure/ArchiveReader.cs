using System.Globalization;
using System.Text;
using DumpbinGui.Core;
namespace DumpbinGui.Infrastructure;
public static class ArchiveReader
{
    public static IReadOnlyList<ArchiveMember> Read(Stream stream)
    {
        var entries=new List<(string Name,long Offset,long Size)>();
        byte[] names=[];
        stream.Position=8;
        var header=new byte[60];
        while(stream.Position<stream.Length) {
            if(stream.Length-stream.Position<60) throw new InvalidDataException("Truncated LIB member header");
            stream.ReadExactly(header);
            if(header[58]!=0x60 || header[59]!=0x0a) throw new InvalidDataException("Invalid LIB member header");
            string name=Encoding.ASCII.GetString(header,0,16).Trim();
            if(!long.TryParse(Encoding.ASCII.GetString(header,48,10).Trim(),NumberStyles.None,CultureInfo.InvariantCulture,out long size) || size<0 || size>stream.Length-stream.Position)
                throw new InvalidDataException("Invalid LIB member size");
            long offset=stream.Position;
            if(name=="//") { if(size>int.MaxValue) throw new InvalidDataException("LIB name table too large"); names=new byte[(int)size]; stream.ReadExactly(names); }
            else if(name!="/" && name!="/SYM64/") entries.Add((name,offset,size));
            stream.Position=offset+size;
            if((stream.Position&1)!=0 && stream.Position<stream.Length) stream.Position++;
        }
        return entries.Select(entry=> {
            string name=entry.Name;
            if(name.StartsWith('/') && int.TryParse(name.AsSpan(1),out int index)) {
                if(index<0 || index>=names.Length) throw new InvalidDataException("Invalid LIB long name offset");
                int end=index; while(end<names.Length && names[end]!=0 && names[end]!=10) end++;
                name=Encoding.UTF8.GetString(names,index,end-index).TrimEnd('/','\r');
            } else name=name.TrimEnd('/');
            return new ArchiveMember(name,entry.Offset,entry.Size);
        }).ToArray();
    }
}
