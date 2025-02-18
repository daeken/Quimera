using System;

namespace IronVisor {
	enum HvReturn {
		Success = 0, 
		Error = (0xba5 << 14) | (0x3e << 26) | 1, 
		Busy = (0xba5 << 14) | (0x3e << 26) | 2, 
		BadArgument = (0xba5 << 14) | (0x3e << 26) | 3,
		NoResources = (0xba5 << 14) | (0x3e << 26) | 4,
		NoDevice = (0xba5 << 14) | (0x3e << 26) | 5,
		Denied = (0xba5 << 14) | (0x3e << 26) | 6,
		Unsupported = (0xba5 << 14) | (0x3e << 26) | 7 
	}

	public class HvException : Exception {
		public HvException(string message) : base(message) {}
	}
	public class BusyException : HvException { public BusyException() : base("Busy") {} }
	public class BadArgumentException : HvException { public BadArgumentException() : base("Bad argument") {} }
	public class NoResourcesException : HvException { public NoResourcesException() : base("No resources") {} }
	public class NoDeviceException : HvException { public NoDeviceException() : base("No device") {} }
	public class DeniedException : HvException { public DeniedException() : base("Denied") {} }
	public class UnsupportedException : HvException { public UnsupportedException() : base("Unsupported") {} }
	public class UnalignedMemoryException : HvException { public UnalignedMemoryException() : base("Unaligned memory") {} }
	
	public static class Hypervisor {
		internal static void Guard(this HvReturn ret) {
			switch(ret) {
				case HvReturn.Success: return;
				case HvReturn.Error: throw new HvException("Unspecified HV error");
				case HvReturn.Busy: throw new BusyException();
				case HvReturn.BadArgument: throw new BadArgumentException();
				case HvReturn.NoResources: throw new NoResourcesException();
				case HvReturn.NoDevice: throw new NoDeviceException();
				case HvReturn.Denied: throw new DeniedException();
				case HvReturn.Unsupported: throw new UnsupportedException();
				default: throw new HvException($"Unexpected HV error: {ret} -- 0x{(uint) ret:X}");
			}
		}
	}
}