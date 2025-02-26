using System.Runtime.InteropServices;
using System.Text;
using DoubleSharp.Pretty;

namespace QuimeraCore;

public class MachOLoader {
	const uint MH_MAGIC_64 = 0xFEEDFACF;

	public readonly ulong EntryPoint;
	public readonly List<(SegmentCommand64, List<Section64> Sections)> Segments = new();
	
	public MachOLoader(ReadOnlySpan<byte> machOData) {
		using var reader = new BinaryReader(new MemoryStream(machOData.ToArray()));

		// Read Mach-O Header
		var header = ReadStruct<MachOHeader>(reader);
		if(header.Magic != MH_MAGIC_64)
			throw new Exception("Invalid Mach-O file");

		// Read Load Commands
		for(var i = 0; i < header.NCmds; i++) {
			var cmdStart = reader.BaseStream.Position;
			var loadCmd = ReadStruct<LoadCommand>(reader);
			var cmdEnd = cmdStart + loadCmd.CmdSize;

			switch(loadCmd.Cmd) {
				case 0x19: // LC_SEGMENT_64
					var segCmd = ReadStruct<SegmentCommand64>(reader);
					var sects = Enumerable.Range(0, (int) segCmd.NSections)
						.Select(_ => ReadStruct<Section64>(reader)).ToList();
					Segments.Add((segCmd, sects));
					break;

				case 0x5: // LC_UNIXTHREAD
					var threadCmd = ReadStruct<UnixThreadCommand>(reader);
					if(threadCmd.Count != 68)
						throw new NotImplementedException(
							$"Mismatch in thread state count: {threadCmd.Count}");
					EntryPoint = threadCmd.PC;
					break;

				/*case 0x35 | 0x8000_0000: // LC_FILESET_ENTRY | LC_REQ_DYLD
					Console.WriteLine("LC_FILESET_ENTRY found, marking kexts...");
					// Handle kexts here
					break;*/

				default:
					reader.BaseStream.Position = cmdStart + loadCmd.CmdSize;
					break;
			}

			if(reader.BaseStream.Position != cmdEnd)
				throw new Exception(
					$"Failed to match size in cmd 0x{loadCmd.Cmd:X} -- {reader.BaseStream.Position} vs {cmdEnd}");
		}

		Console.WriteLine($"Parsed Mach-O. Entry point: 0x{EntryPoint:X}");
	}

	static T ReadStruct<T>(BinaryReader reader) where T : struct {
		var size = Marshal.SizeOf<T>();
		var bytes = reader.ReadBytes(size);
		var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		var structure = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
		handle.Free();
		return structure;
	}

	public struct MachOHeader {
		public uint Magic;
		public int CpuType;
		public int CpuSubtype;
		public uint FileType;
		public uint NCmds;
		public uint SizeOfCmds;
		public uint Flags;
		public uint Reserved;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct LoadCommand {
		public uint Cmd;
		public uint CmdSize;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct UnixThreadCommand {
		public uint Flavor;
		public uint Count;
		public ulong X0,
			X1,
			X2,
			X3,
			X4,
			X5,
			X6,
			X7,
			X8,
			X9,
			X10,
			X11,
			X12,
			X13,
			X14,
			X15,
			X16,
			X17,
			X18,
			X19,
			X20,
			X21,
			X22,
			X23,
			X24,
			X25,
			X26,
			X27,
			X28,
			FP,
			LR,
			SP,
			PC;
		public uint CPSR;
		uint Padding;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct SegmentCommand64 {
		public fixed byte _SegName[16];
		public ulong VMAddr;
		public ulong VMSize;
		public ulong FileOff;
		public ulong FileSize;
		public uint MaxProt;
		public uint InitProt;
		public uint NSections;
		public uint Flags;

		public string SegName {
			get {
				fixed(byte* namePtr = _SegName) {
					return Encoding.ASCII.GetString(namePtr, 16).TrimEnd('\0');
				}
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct Section64 {
		public fixed byte _SectName[16];
		public fixed byte _SegName[16];
		public ulong VMAddr;
		public ulong VMSize;
		public ulong FileOff;
		public ulong FileSize;
		public uint MaxProt;
		public uint InitProt;
		public uint NSections;
		public uint Flags;

		public string SectName {
			get {
				fixed(byte* namePtr = _SectName) {
					return Encoding.ASCII.GetString(namePtr, 16).TrimEnd('\0');
				}
			}
		}

		public string SegName {
			get {
				fixed(byte* namePtr = _SegName) {
					return Encoding.ASCII.GetString(namePtr, 16).TrimEnd('\0');
				}
			}
		}
	}
}