using System.IO.Compression;
using System.Text;

namespace QuimeraCore;

public class IpswLoader {
	readonly ZipArchive Ipsw;
	public IpswLoader(string path) => Ipsw = new ZipArchive(File.OpenRead(path));

	ZipArchiveEntry FindFileStartingWith(string fn) =>
		Ipsw.Entries.FirstOrDefault(entry => entry.FullName.StartsWith(fn))
		?? throw new NotSupportedException();

	public byte[] KernelCache {
		get {
			var entry = FindFileStartingWith("kernelcache");
			Span<byte> buf = new byte[entry.Length];
			entry.Open().ReadExactly(buf);
			var pos = 0;
			if(buf[pos++] != 0x30) throw new NotSupportedException(); // Should be sequence + constructed
			ReadLength(ref pos, buf);
			if(ReadString(ref pos, buf) != "IM4P") throw new NotSupportedException();
			if(ReadString(ref pos, buf) != "krnl") throw new NotSupportedException();
			var desc = ReadString(ref pos, buf);
			var contents = ReadOctetString(ref pos, buf);
			return Lzfse.Decompress(contents).ToArray();
		}
	}

	static Span<byte> ReadOctetString(ref int pos, Span<byte> buf) {
		if(buf[pos++] != 0x04) throw new NotSupportedException();
		var len = ReadLength(ref pos, buf);
		var tbuf = buf[pos..(pos + len)];
		pos += len;
		return tbuf;
	}

	static string ReadString(ref int pos, Span<byte> buf) {
		if(buf[pos++] != 0x16) throw new NotSupportedException();
		var len = ReadLength(ref pos, buf);
		var str = Encoding.ASCII.GetString(buf[pos..(pos + len)]);
		pos += len;
		return str;
	}

	static int ReadLength(ref int pos, Span<byte> buf) {
		var tlen = buf[pos++];
		if((tlen & 0x80) == 0)
			return tlen & 0x7F;
		tlen ^= 0x80;
		var len = 0;
		for(var i = 0; i < tlen; ++i)
			len = (len << 8) | buf[pos++];
		return len;
	}
}