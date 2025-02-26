using System.Runtime.CompilerServices;
using DoubleSharp.Buffers;
using DoubleSharp.Pretty;

namespace QuimeraCore;

public class DeviceTree {
	public readonly Dictionary<string, (bool ShouldReplace, byte[] Data)> Properties = [];
	public readonly List<DeviceTree> Children = [];

	public static DeviceTree Parse(ReadOnlySpan<byte> data) => Parse(ref data);
	static DeviceTree Parse(ref ReadOnlySpan<byte> data) {
		var temp = data.Cast<byte, uint>();
		var numProps = temp[0];
		var numChildren = temp[1];
		var ntree = new DeviceTree();
		data = data[8..];
		for(var i = 0; i < numProps; i++) {
			var pname = System.Text.Encoding.ASCII.GetString(data[..32]).TrimEnd('\0');
			var size = BitConverter.ToUInt32(data[32..36]); // Hate mixing this with the span cast
			var replace = (size & 0x8000_0000U) != 0; 
			if(replace)
				size ^= 0x8000_0000U;
			ntree.Properties[pname] = (replace, data[36..(36 + (int) size)].ToArray());
			data = data[(36 + (int) ((size + 0x3U) & ~0x3U))..];
		}
		for(var i = 0; i < numChildren; i++)
			ntree.Children.Add(Parse(ref data));
		return ntree;
	}

	public static void Dump(DeviceTree tree, int indent = 0) {
		Console.WriteLine($"{new string(' ', indent * 2)}{System.Text.Encoding.ASCII.GetString(tree.Properties["name"].Data).TrimEnd('\0')} {{");
		foreach(var (k, (_, d)) in tree.Properties) {
			if(k == "name") continue;
			var endingNulls = d.Reverse().TakeWhile(v => v == 0).Count();
			var isAscii = d.Take(d.Length - endingNulls).All(v => v is >= 0x20 and <= 0x7E);
			if(isAscii)
				Console.WriteLine($"{new string(' ', indent * 2 + 2)}{k}: {System.Text.Encoding.ASCII.GetString(d).TrimEnd('\0').ToPrettyString()}");
			else
				Console.WriteLine($"{new string(' ', indent * 2 + 2)}{k}: [{string.Join(" ", d.Select(v => $"{v:X02}"))}]");
		}
		foreach(var child in tree.Children)
			Dump(child, indent + 1);
		Console.WriteLine($"{new string(' ', indent * 2)}}}");
	}
}