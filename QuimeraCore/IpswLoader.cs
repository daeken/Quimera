using System.IO.Compression;
using System.Text;
using Claunia.PropertyList;
using DoubleSharp.Pretty;

namespace QuimeraCore;

public class IpswLoader {
	readonly ZipArchive Ipsw;
	readonly string DeviceTreePath, KernelCachePath;
	public IpswLoader(string path) {
		Ipsw = new ZipArchive(File.OpenRead(path));
		var manifest = ReadEntry("BuildManifest.plist");
		var mplist = (NSDictionary) PropertyListParser.Parse(manifest);
		DeviceTreePath =
			((NSString)
				(((NSDictionary)
					((NSDictionary) ((NSDictionary) ((NSDictionary) ((NSArray) mplist[
						"BuildIdentities"])[0])["Manifest"])["DeviceTree"])["Info"])["Path"]))
			.Content;
		KernelCachePath =
			((NSString)
				(((NSDictionary)
					((NSDictionary) ((NSDictionary) ((NSDictionary) ((NSArray) mplist[
						"BuildIdentities"])[0])["Manifest"])["KernelCache"])["Info"])["Path"]))
			.Content;
	}

	Span<byte> ReadEntry(string fn) {
		var entry = Ipsw.GetEntry(fn) ?? throw new FileNotFoundException($"File not found: {fn}");
		Span<byte> buf = new byte[entry.Length];
		entry.Open().ReadExactly(buf);
		return buf;
	}

	public byte[] DeviceTree {
		get {
			var buf = ReadEntry(DeviceTreePath);
			var pos = 0;
			if(buf[pos++] != 0x30) throw new NotSupportedException(); // Should be sequence + constructed
			ReadLength(ref pos, buf);
			if(ReadString(ref pos, buf) != "IM4P") throw new NotSupportedException();
			if(ReadString(ref pos, buf) != "dtre") throw new NotSupportedException();
			var desc = ReadString(ref pos, buf);
			var contents = ReadOctetString(ref pos, buf);
			return Lzfse.Decompress(contents).ToArray();
		}
	}

	public byte[] KernelCache {
		get {
			var buf = ReadEntry(KernelCachePath);
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