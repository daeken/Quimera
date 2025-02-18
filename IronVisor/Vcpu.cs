using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;

namespace IronVisor {
	public enum ExitReason : uint {
		/*! asynchronous exit requested explicitly by hv_vcpus_exit() call */
		Canceled,
		/*! synchronous exception to EL2 triggered by the guest */
		Exception,
		/*!
		 * ARM Generic VTimer became pending since the last hv_vcpu_run() call
		 * returned. The caller is expected to make the interrupt corresponding to
		 * the VTimer pending in the guest's interrupt controller.
		 *
		 * This exit automatically sets the VTimer mask.
		 * The VCPU will not exit with this status again until after the mask is cleared
		 * with hv_vcpu_set_vtimer_mask(), which should be called during a trap of
		 * the EOI for the guest's VTimer interrupt handler.
		 */
		VTimerActivated,
		/*!
		 * Unable to determine exit reason: this should not happen under normal
		 * operation.
		 */
		Unknown
	}

	public enum ExceptionCode : uint {
		TrappedWf_ = 0b000001, 
		ServiceCall = 0b010101,
		HyperCall = 0b010110, 
		MonitorCall = 0b010111, 
		MsrMrsTrap = 0b011000, 
		InsnAbort = 0b100000, 
		DataAbort = 0b100100, 
		PcAlignmentFault = 0b100010, 
		BrkInsn = 0b111100, 
	}

	public enum Reg : uint {
		X0,
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
		X29,
		FP = X29,
		X30,
		LR = X30,
		PC,
		FPCR,
		FPSR,
		CPSR,
	}

	public enum FpReg : uint {
		Q0,
		Q1,
		Q2,
		Q3,
		Q4,
		Q5,
		Q6,
		Q7,
		Q8,
		Q9,
		Q10,
		Q11,
		Q12,
		Q13,
		Q14,
		Q15,
		Q16,
		Q17,
		Q18,
		Q19,
		Q20,
		Q21,
		Q22,
		Q23,
		Q24,
		Q25,
		Q26,
		Q27,
		Q28,
		Q29,
		Q30,
		Q31,
	}

	public enum SysReg : ushort {
		DBGBVR0_EL1 = 0x8004,
		DBGBCR0_EL1 = 0x8005,
		DBGWVR0_EL1 = 0x8006,
		DBGWCR0_EL1 = 0x8007,
		DBGBVR1_EL1 = 0x800c,
		DBGBCR1_EL1 = 0x800d,
		DBGWVR1_EL1 = 0x800e,
		DBGWCR1_EL1 = 0x800f,
		MDCCINT_EL1 = 0x8010,
		MDSCR_EL1 = 0x8012,
		DBGBVR2_EL1 = 0x8014,
		DBGBCR2_EL1 = 0x8015,
		DBGWVR2_EL1 = 0x8016,
		DBGWCR2_EL1 = 0x8017,
		DBGBVR3_EL1 = 0x801c,
		DBGBCR3_EL1 = 0x801d,
		DBGWVR3_EL1 = 0x801e,
		DBGWCR3_EL1 = 0x801f,
		DBGBVR4_EL1 = 0x8024,
		DBGBCR4_EL1 = 0x8025,
		DBGWVR4_EL1 = 0x8026,
		DBGWCR4_EL1 = 0x8027,
		DBGBVR5_EL1 = 0x802c,
		DBGBCR5_EL1 = 0x802d,
		DBGWVR5_EL1 = 0x802e,
		DBGWCR5_EL1 = 0x802f,
		DBGBVR6_EL1 = 0x8034,
		DBGBCR6_EL1 = 0x8035,
		DBGWVR6_EL1 = 0x8036,
		DBGWCR6_EL1 = 0x8037,
		DBGBVR7_EL1 = 0x803c,
		DBGBCR7_EL1 = 0x803d,
		DBGWVR7_EL1 = 0x803e,
		DBGWCR7_EL1 = 0x803f,
		DBGBVR8_EL1 = 0x8044,
		DBGBCR8_EL1 = 0x8045,
		DBGWVR8_EL1 = 0x8046,
		DBGWCR8_EL1 = 0x8047,
		DBGBVR9_EL1 = 0x804c,
		DBGBCR9_EL1 = 0x804d,
		DBGWVR9_EL1 = 0x804e,
		DBGWCR9_EL1 = 0x804f,
		DBGBVR10_EL1 = 0x8054,
		DBGBCR10_EL1 = 0x8055,
		DBGWVR10_EL1 = 0x8056,
		DBGWCR10_EL1 = 0x8057,
		DBGBVR11_EL1 = 0x805c,
		DBGBCR11_EL1 = 0x805d,
		DBGWVR11_EL1 = 0x805e,
		DBGWCR11_EL1 = 0x805f,
		DBGBVR12_EL1 = 0x8064,
		DBGBCR12_EL1 = 0x8065,
		DBGWVR12_EL1 = 0x8066,
		DBGWCR12_EL1 = 0x8067,
		DBGBVR13_EL1 = 0x806c,
		DBGBCR13_EL1 = 0x806d,
		DBGWVR13_EL1 = 0x806e,
		DBGWCR13_EL1 = 0x806f,
		DBGBVR14_EL1 = 0x8074,
		DBGBCR14_EL1 = 0x8075,
		DBGWVR14_EL1 = 0x8076,
		DBGWCR14_EL1 = 0x8077,
		DBGBVR15_EL1 = 0x807c,
		DBGBCR15_EL1 = 0x807d,
		DBGWVR15_EL1 = 0x807e,
		DBGWCR15_EL1 = 0x807f,
		MIDR_EL1 = 0xc000,
		MPIDR_EL1 = 0xc005,
		ID_AA64PFR0_EL1 = 0xc020,
		ID_AA64PFR1_EL1 = 0xc021,
		ID_AA64DFR0_EL1 = 0xc028,
		ID_AA64DFR1_EL1 = 0xc029,
		ID_AA64ISAR0_EL1 = 0xc030,
		ID_AA64ISAR1_EL1 = 0xc031,
		ID_AA64MMFR0_EL1 = 0xc038,
		ID_AA64MMFR1_EL1 = 0xc039,
		ID_AA64MMFR2_EL1 = 0xc03a,
		SCTLR_EL1 = 0xc080,
		CPACR_EL1 = 0xc082,
		TTBR0_EL1 = 0xc100,
		TTBR1_EL1 = 0xc101,
		TCR_EL1 = 0xc102,
		APIAKEYLO_EL1 = 0xc108,
		APIAKEYHI_EL1 = 0xc109,
		APIBKEYLO_EL1 = 0xc10a,
		APIBKEYHI_EL1 = 0xc10b,
		APDAKEYLO_EL1 = 0xc110,
		APDAKEYHI_EL1 = 0xc111,
		APDBKEYLO_EL1 = 0xc112,
		APDBKEYHI_EL1 = 0xc113,
		APGAKEYLO_EL1 = 0xc118,
		APGAKEYHI_EL1 = 0xc119,
		SPSR_EL1 = 0xc200,
		ELR_EL1 = 0xc201,
		SP_EL0 = 0xc208,
		AFSR0_EL1 = 0xc288,
		AFSR1_EL1 = 0xc289,
		ESR_EL1 = 0xc290,
		FAR_EL1 = 0xc300,
		PAR_EL1 = 0xc3a0,
		MAIR_EL1 = 0xc510,
		AMAIR_EL1 = 0xc518,
		VBAR_EL1 = 0xc600,
		CONTEXTIDR_EL1 = 0xc681,
		TPIDR_EL1 = 0xc684,
		CNTKCTL_EL1 = 0xc708,
		CSSELR_EL1 = 0xd000,
		TPIDR_EL0 = 0xde82,
		TPIDRRO_EL0 = 0xde83,
		CNTV_CTL_EL0 = 0xdf19,
		CNTV_CVAL_EL0 = 0xdf1a,
		SP_EL1 = 0xe208,
	}

	public enum InterruptType {
		Irq, 
		Fiq, 
	}
	
	public unsafe class Vcpu : IDisposable {
		[StructLayout(LayoutKind.Sequential)]
		public struct Exit {
			public ExitReason Reason;
			public ulong Syndrome, VirtualAddress, PhysicalAddress;
		}

		readonly Thread Creator;
		readonly ulong Id;
		readonly Exit* ExitStruct;
		internal bool Destroyed = false;

		internal Vcpu() {
			Creator = Thread.CurrentThread;
			hv_vcpu_create(out Id, out ExitStruct, IntPtr.Zero).Guard();
			X = new(
				index => index == 31 ? 0 : this[(Reg) ((uint) Reg.X0 + index)], 
				(index, value) => {
					if(index != 31)
						this[(Reg) ((uint) Reg.X0 + index)] = value;
				});
			Q = new(
				index => this[(FpReg) index], 
				(index, value) => this[(FpReg) index] = value
			);
		}

		public void Dispose() => Destroy();

		public void Destroy() {
			if(Destroyed) return;
			if(Thread.CurrentThread != Creator)
				throw new ThreadStateException("Vcpu destructor called from non-creator thread");
			hv_vcpu_destroy(Id).Guard();
			Destroyed = true;
		}

		public ref Exit Run() {
			hv_vcpu_run(Id).Guard();
			return ref *ExitStruct;
		}

		public ulong this[Reg reg] {
			get {
				hv_vcpu_get_reg(Id, reg, out var value).Guard();
				return value;
			}
			set => hv_vcpu_set_reg(Id, reg, value).Guard();
		}

		public Vector4 this[FpReg reg] {
			get {
				hv_vcpu_get_simd_fp_reg(Id, reg, out var value).Guard();
				return value;
			}
			set => hv_vcpu_set_simd_fp_reg(Id, reg, value).Guard();
		}

		public ulong this[SysReg reg] {
			get {
				hv_vcpu_get_sys_reg(Id, reg, out var value).Guard();
				return value;
			}
			set => hv_vcpu_set_sys_reg(Id, reg, value).Guard();
		}

		public ulong PC {
			get => this[Reg.PC];
			set => this[Reg.PC] = value;
		}
		public ulong SP {
			get => this[SysReg.SP_EL1];
			set => this[SysReg.SP_EL1] = value;
		}
		public ulong FPCR {
			get => this[Reg.FPCR];
			set => this[Reg.FPCR] = value;
		}
		public ulong FPSR {
			get => this[Reg.FPSR];
			set => this[Reg.FPSR] = value;
		}
		public ulong CPSR {
			get => this[Reg.CPSR];
			set => this[Reg.CPSR] = value;
		}

		public bool TrapDebugExceptions {
			get {
				hv_vcpu_get_trap_debug_exceptions(Id, out var val).Guard();
				return val;
			}
			set => hv_vcpu_set_trap_debug_exceptions(Id, value).Guard();
		}
		public bool TrapDebugRegisterAccesses {
			get {
				hv_vcpu_get_trap_debug_reg_accesses(Id, out var val).Guard();
				return val;
			}
			set => hv_vcpu_set_trap_debug_reg_accesses(Id, value).Guard();
		}

		public bool IrqPending {
			get {
				hv_vcpu_get_pending_interrupt(Id, InterruptType.Irq, out var val).Guard();
				return val;
			}
			set => hv_vcpu_set_pending_interrupt(Id, InterruptType.Irq, value).Guard();
		}
		
		public bool FiqPending {
			get {
				hv_vcpu_get_pending_interrupt(Id, InterruptType.Fiq, out var val).Guard();
				return val;
			}
			set => hv_vcpu_set_pending_interrupt(Id, InterruptType.Fiq, value).Guard();
		}

		public void ForceExit() {
			var id = Id;
			hv_vcpus_exit(ref id, 1).Guard();
		}

		public readonly Indexer<int, ulong> X;
		public readonly Indexer<int, Vector4> Q;

		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_create(out ulong vcpu, out Exit* exit, IntPtr config);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_destroy(ulong vcpu);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_get_reg(ulong vcpu, Reg reg, out ulong value);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_set_reg(ulong vcpu, Reg reg, ulong value);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_get_simd_fp_reg(ulong vcpu, FpReg reg, out Vector4 value);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_set_simd_fp_reg(ulong vcpu, FpReg reg, Vector4 value);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_get_sys_reg(ulong vcpu, SysReg reg, out ulong value);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_set_sys_reg(ulong vcpu, SysReg reg, ulong value);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_get_trap_debug_exceptions(ulong vcpu, out bool value);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_set_trap_debug_exceptions(ulong vcpu, bool value);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_get_trap_debug_reg_accesses(ulong vcpu, out bool value);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_set_trap_debug_reg_accesses(ulong vcpu, bool value);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_run(ulong vcpu);

		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_get_pending_interrupt(ulong vcpu, InterruptType type, out bool pending);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpu_set_pending_interrupt(ulong vcpu, InterruptType type, bool pending);
		
		[DllImport("Hypervisor.framework/Hypervisor")]
		static extern HvReturn hv_vcpus_exit(ref ulong vcpus, uint count);
	}
}