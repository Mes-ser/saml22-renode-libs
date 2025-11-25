using System;
using Antmicro.Renode.Logging;

namespace Antmicro.Renode.Peripherals.Memory {

    public class Saml22NVM : ArrayMemory, IAbsoluteAddressAware {
        // Base class ArrayMemory is a dirty hack to allow code execution.
        public Saml22NVM(ulong size, byte initialValue = 0xFF, bool useCache = false) : base(size, initialValue) {
            _cached = useCache; // TODO: Handle cache
        }

        public override ulong ReadQuadWord(long offset) {
            if(IsSysbusAccess) {
                this.WarningLog("64bit reads not supported.");
                return 0x0;
            }
            return base.ReadQuadWord(offset);
        }

        // invalid operation
        // byte write is forbidden.
        // Calling ByteWrite to inform NVMCTRL.
        public override void WriteByte(long offset, byte value) => ByteWrite?.Invoke(offset, value);
        public override void WriteWord(long offset, ushort value) {
            if(IsSysbusAccess) {
                this.InfoLog($"Sysbus W write. [{offset:x}] [{value:x}]");
                WordWrite?.Invoke(offset, value);
            }
            else {
                this.InfoLog($"NVMCTRL W write. [{offset:x}] [{value:x}]");
                base.WriteWord(offset, value);
            }
        }
        public override void WriteDoubleWord(long offset, uint value) {
            if(IsSysbusAccess) {
                this.InfoLog($"Sysbus DW write. [{offset:x}] [{value:x}]");
                DoubleWordWrite?.Invoke(offset, value);
            }
            else {
                this.InfoLog($"NVMCTRL DW write. [{offset:x}] [{value:x}]");
                base.WriteDoubleWord(offset, value);
            }
        }
        public override void WriteQuadWord(long offset, ulong value) {
            if(IsSysbusAccess) {
                this.WarningLog("64bit writes not supported.");
                return;
            }
            this.InfoLog($"NVMCTRL QW write. [{offset:x}] [{value:x}]");
            base.WriteQuadWord(offset, value);
        }

        public void SetAbsoluteAddress(ulong address) {
            // Used to determine if access is from sysbus or NVMCTRL.
            // access from sysbus will call this function before performing R/W operation.
            _sysbusAccess = true;
        }

        // public event Func<long, byte> ByteRead;
        // public event Func<long, ushort> WordRead;
        // public event Func<long, uint> DoubleWordRead;

        public event Action<long, byte> ByteWrite;
        public event Action<long, ushort> WordWrite;
        public event Action<long, uint> DoubleWordWrite;

        private bool _cached;
        private bool IsSysbusAccess {
            get {
                bool ret = _sysbusAccess;
                if(_sysbusAccess)
                    _sysbusAccess = false;
                return ret;
            }
            set => _sysbusAccess = value;
        }
        private bool _sysbusAccess;
    }
}