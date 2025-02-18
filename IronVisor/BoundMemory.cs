using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IronVisor {
	public class BoundMemory : IDisposable {
		public readonly Memory<byte> Memory;
		internal readonly IntPtr Pointer;
		readonly MemoryHandle Handle;
		readonly Action OnDispose;
			
		internal unsafe BoundMemory(ulong size, Action onDispose) {
			if((size & 0x3FFF) != 0) throw new ArgumentException("BoundMemory size must be a multiple of page size");
			Memory = new(new byte[size + 0x3FFF]);
			Handle = Memory.Pin();
			var tptr = (ulong) Handle.Pointer;
			if((tptr & 0x3FFF) != 0)
				tptr = (tptr & ~0x3FFFUL) + 0x4000;
			var offset = tptr - (ulong) Handle.Pointer;
			if(offset != 0)
				Memory = Memory[(int) offset..];
			Pointer = (IntPtr) tptr;
			OnDispose = onDispose;
		}

		~BoundMemory() => Dispose(false);

		public Span<T> AsSpan<T>(int offset = 0) where T : struct => MemoryMarshal.Cast<byte, T>(Memory.Span[offset..]);

		void Dispose(bool disposing) {
			OnDispose();
			if(disposing) Handle.Dispose();
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}