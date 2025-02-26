using System.Runtime.InteropServices;

namespace QuimeraCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct BootArgs {
	public ushort Revision;               // Revision of boot_args structure
	public ushort Version;                // Version of boot_args structure
	public ulong VirtBase;                // Virtual base of memory
	public ulong PhysBase;                // Physical base of memory
	public ulong MemSize;                 // Size of memory
	public ulong TopOfKernelData;         // Highest physical address used in kernel data area

	public ulong VideoBaseAddr;           // Base address of video memory
	public ulong VideoDisplay;            // Display Code (if applicable)
	public ulong VideoRowBytes;           // Number of bytes per pixel row
	public ulong VideoWidth;              // Width
	public ulong VideoHeight;             // Height
	public ulong VideoDepth;              // Pixel depth and other parameters

	public uint MachineType;              // Machine Type
	public ulong DeviceTreeP;             // Base of flattened device tree (pointer)
	public uint DeviceTreeLength;         // Length of flattened tree

	public fixed byte _CommandLine[1024];  // Passed in command line

	public ulong BootFlags;               // Additional flags specified by the bootloader
	public ulong MemSizeActual;           // Actual size of memory

	// Helper method to retrieve the command line as a string
	public string CommandLine {
		get {
			fixed(byte* cmdPtr = _CommandLine)
				return System.Text.Encoding.ASCII.GetString(cmdPtr, 1024).TrimEnd('\0');
		}
		set {
			fixed(byte* cmdPtr = _CommandLine) {
				var cmdSpan = new Span<byte>(cmdPtr, 1024);
				cmdSpan.Clear();
				System.Text.Encoding.ASCII.GetBytes(value.AsSpan()[..1023], cmdSpan);
			}
		}
	}
}