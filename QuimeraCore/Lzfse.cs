using System.Runtime.InteropServices;

namespace QuimeraCore;

public class Lzfse {
	// TODO: Replace with C# implementation
	// TODO: Add stream interface to handle larger files
	public static unsafe int Decompress(ReadOnlySpan<byte> input, Span<byte> output) {
		fixed(void* src = input) fixed(void* dst = output)
			return (int) lzfse_decode_buffer(dst, output.Length, src, input.Length, IntPtr.Zero);
	}

	public static Span<byte> Decompress(ReadOnlySpan<byte> input, int length) {
		var buf = new byte[length];
		var nlen = Decompress(input, buf);
		return buf.AsSpan(0, nlen);
	}

	public static Span<byte> Decompress(ReadOnlySpan<byte> input) {
		var length = input.Length * 2;
		while(true) {
			var buf = new byte[length];
			var nlen = Decompress(input, buf);
			if(nlen == length) {
				length *= 2;
				continue;
			}
			return buf.AsSpan(0, nlen);
		}
	}

	[DllImport("/opt/homebrew/lib/liblzfse.dylib")]
	static extern unsafe nint lzfse_decode_buffer(void* dst_buffer, nint dst_size, void* src_buffer, nint src_size, IntPtr scratch_buffer);
}