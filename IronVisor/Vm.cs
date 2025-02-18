using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace IronVisor {
	[Flags]
	public enum VmOptions {
		Default = 0, 
		SpecifyMitigations = 1, 
		MitigationAEnable = 2, 
		MitigationBEnable = 4, 
		MitigationCEnable = 8, 
		MitigationDEnable = 16, 
		MitigationEEnable = 32
	}

	[Flags]
	public enum MemoryFlags {
		Read = 1, 
		Write = 2, 
		Exec = 4, 
		RW = Read | Write, 
		RX = Read | Exec, 
		WX = Write | Exec, 
		RWX = Read | Write | Exec, 
	}
	
	public class Vm : IDisposable {
		public readonly int MaxVcpuCount;
		bool Disposed = false;

		readonly List<WeakReference<BoundMemory>> BoundMemoryBindings = new();
		readonly List<Vcpu> Vcpus = new();

		public Vm(VmOptions options = VmOptions.Default) {
			hv_vm_create(options).Guard();
			hv_vm_get_max_vcpu_count(out var maxVcpuCount);
			MaxVcpuCount = (int) maxVcpuCount;
		}

		public BoundMemory Map(ulong guestPhysAddr, ulong size, MemoryFlags flags) {
			var mapped = false;
			var bm = new BoundMemory(size, () => {
				if(mapped) hv_vm_unmap((IntPtr) guestPhysAddr, size).Guard();
			});
			hv_vm_map(bm.Pointer, (IntPtr) guestPhysAddr, size, flags).Guard();
			mapped = true;
			BoundMemoryBindings.Add(new(bm));
			return bm;
		}

		public void Map(IntPtr hostAddress, ulong guestPhysAddr, ulong size, MemoryFlags flags) =>
			hv_vm_map(hostAddress, (IntPtr) guestPhysAddr, size, flags).Guard();

		public void Unmap(ulong guestPhysAddr, ulong size) =>
			hv_vm_unmap((IntPtr) guestPhysAddr, size).Guard();

		public void Protect(ulong guestPhysAddr, ulong size, MemoryFlags flags) =>
			hv_vm_protect((IntPtr) guestPhysAddr, size, flags).Guard();

		public Vcpu CreateVcpu() {
			var vcpu = new Vcpu();
			Vcpus.Add(vcpu);
			return vcpu;
		}

		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vm_create(VmOptions flags);

		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vm_destroy();
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vm_map(IntPtr addr, IntPtr ipa, ulong size, MemoryFlags flags);

		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vm_unmap(IntPtr ipa, ulong size);

		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vm_protect(IntPtr ipa, ulong size, MemoryFlags flags);

		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vm_get_max_vcpu_count(out uint max_vcpu_count);

		public void Dispose() {
			if(Disposed) return;
			GC.SuppressFinalize(this);
			foreach(var bmr in BoundMemoryBindings)
				if(bmr.TryGetTarget(out var bm)) {
					bm.Dispose();
					bmr.SetTarget(null);
				}

			if(!Vcpus.All(x => x.Destroyed))
				throw new HvException("Some VCPUs not destroyed prior to VM disposal");
			hv_vm_destroy().Guard();
			Disposed = true;
		}
	}
}