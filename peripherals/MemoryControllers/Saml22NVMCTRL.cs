using System;
using System.Linq;

using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.Memory;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.MemoryControllers {
    public class Saml22NVMCTRLFlash : ArrayMemory, IMemory,
        IBytePeripheral, IWordPeripheral, IDoubleWordPeripheral,
        IAbsoluteAddressAware,
        IPeripheralRegister<MappedMemory, NumberRegistrationPoint<ulong>> {

        // Base class ArrayMemory is a dirty hack to allow NVMCTRL to be frontend for flash memory and allow to execute code.
        public Saml22NVMCTRLFlash(Machine machine) : base(null) {
            _machine = machine;

            _interruptsManager = new InterruptManager<Interrupts>(this);

            _byteRegisters = new ByteRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _doubleWordRegisters = new DoubleWordRegisterCollection(this);

            _pageBuffer = new MappedMemory(_machine, MEMORY_PAGE_SIZE_BYTES);
            _pageBuffer.ResetByte = 0xFF;
            _pageBuffer.ZeroAll();

            DefineRegisters();

            _interruptsManager.SetInterrupt(Interrupts.Ready);
        }

        public new byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public new ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public new uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public new void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);
        public new void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);
        public new void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);

        // While using 'sysbus LoadELF' Connection Regions aren't used, so this method (ArrayMemory.WriteBytes) is called.
        public new void WriteBytes(long offset, byte[] bytes, int startingIndex, int count, IPeripheral context = null) {
            if(_absoluteAddress >= (ulong)(MemoryRegionBaseAddr.Main) &&
                    _absoluteAddress <= (ulong)(_mainMemory.Size - 1))
                MainMemoryWriteBytes(offset, bytes, startingIndex, count, context);
            else if(_absoluteAddress >= (ulong)(MemoryRegionBaseAddr.RWWEE) &&
                            _absoluteAddress <= (ulong)((ulong)MemoryRegionBaseAddr.RWWEE + (ulong)_rwweeMemory.Size) - 1)
                RWWEEMemoryWriteBytes(offset, bytes, startingIndex, count, context);
            else if(_absoluteAddress >= (ulong)(MemoryRegionBaseAddr.AUX) &&
                            _absoluteAddress <= (ulong)((ulong)MemoryRegionBaseAddr.AUX + (ulong)_auxMemory.Size) - 1)
                AUXWriteBytes(offset, bytes, startingIndex, count, context);
        }
        public new byte[] ReadBytes(long offset, int count, IPeripheral context = null) {
            if(_absoluteAddress >= (ulong)(MemoryRegionBaseAddr.Main) && _absoluteAddress <= (ulong)(_mainMemory.Size - 1))
                return MainMemoryReadBytes(offset, count, context);
            else if(_absoluteAddress >= (ulong)(MemoryRegionBaseAddr.RWWEE) && _absoluteAddress <= (ulong)((ulong)MemoryRegionBaseAddr.RWWEE + (ulong)_rwweeMemory.Size) - 1)
                return RWWEEMemoryReadBytes(offset, count, context);
            else if(_absoluteAddress >= (ulong)(MemoryRegionBaseAddr.AUX) && _absoluteAddress <= (ulong)((ulong)MemoryRegionBaseAddr.AUX + (ulong)_auxMemory.Size) - 1)
                return AUXReadBytes(offset, count, context);

            return Array.Empty<byte>();
        }

        /**** START MAIN MEMORY ****/
        [ConnectionRegion("MainMemory")]
        public void MainMemoryWriteByte(long offset, byte value) {
            _ = value;
            this.ErrorLog($"Illegal Byte write at [0x{offset:x}].");
        }
        [ConnectionRegion("MainMemory")]
        public void MainMemoryWriteWord(long offset, ushort value) {
            if(_mainMemory != null) {
                this.ErrorLog($"Page [0x{offset & (MEMORY_PAGE_SIZE_BYTES - 1):x}], Offset [0x{offset:x}]");
                _pageBuffer.WriteWord(offset & (MEMORY_PAGE_SIZE_BYTES - 1), value);
            }
            else
                this.WarningLog($"TODO: Invalid memory Word write at [0x{offset:x}]");
        }
        [ConnectionRegion("MainMemory")]
        public void MainMemoryWriteDoubleWord(long offset, uint value) {
            if(_mainMemory != null) {
                this.ErrorLog($"Page [0x{offset & (MEMORY_PAGE_SIZE_BYTES - 1):x}], Offset [0x{offset:x}]");
                _pageBuffer.WriteDoubleWord(offset & (MEMORY_PAGE_SIZE_BYTES - 1), value);
            }
            else
                this.WarningLog($"TODO: Invalid memory Double Word write at [0x{offset:x}]");
        }
        [ConnectionRegion("MainMemory")]
        public void MainMemoryWriteQuadWord(long offset, ulong value) => throw new NotImplementedException();
        [ConnectionRegion("MainMemory")]
        public void MainMemoryWriteBytes(long offset, byte[] array, int startingIndex, int count, IPeripheral context = null) {
            if(_mainMemory != null)
                _mainMemory.WriteBytes(offset & (_mainMemory.Size - 1), array, startingIndex, count, context);
            else
                this.WarningLog($"TODO: Invalid memory bytes write at [0x{offset:x}]");
        }
        [ConnectionRegion("MainMemory")]
        public byte MainMemoryReadByte(long offset) {
            if(_mainMemory != null)
                return _mainMemory.ReadByte(offset & (_mainMemory.Size - 1));
            else
                this.WarningLog($"TODO: Invalid memory Byte read at [0x{offset:x}]");
            return 0x0;
        }
        [ConnectionRegion("MainMemory")]
        public ushort MainMemoryReadWord(long offset) {
            if(_mainMemory != null)
                return _mainMemory.ReadWord(offset & (_mainMemory.Size - 1));
            else
                this.WarningLog($"TODO: Invalid memory Word read at [0x{offset:x}]");
            return 0x0;
        }
        [ConnectionRegion("MainMemory")]
        public uint MainMemoryReadDoubleWord(long offset) {
            if(_mainMemory != null)
                return _mainMemory.ReadDoubleWord(offset & (_mainMemory.Size - 1));
            else
                this.WarningLog($"TODO: Invalid memory Double Word read at [0x{offset:x}]");
            return 0x0;
        }
        [ConnectionRegion("MainMemory")]
        public byte[] MainMemoryReadBytes(long offset, int count, IPeripheral context = null) => throw new NotImplementedException();
        [ConnectionRegion("MainMemory")]
        public ulong MainMemoryReadQuadWord(long offset) => throw new NotImplementedException();
        /**** END MAIN MEMORY ****/

        /**** START RWWEE MEMORY ****/
        [ConnectionRegion("RWWEEMemory")]
        public void RWWEEMemoryWriteByte(long offset, byte value) {
            _ = value;
            this.ErrorLog($"Illegal Byte write at [0x{offset:x}].");
        }
        [ConnectionRegion("RWWEEMemory")]
        public void RWWEEMemoryWriteWord(long offset, ushort value) {
            if(_rwweeMemory != null) {
                this.ErrorLog($"Page [0x{offset & (MEMORY_PAGE_SIZE_BYTES - 1):x}], Offset [0x{offset:x}]");
                _pageBuffer.WriteWord(offset & (MEMORY_PAGE_SIZE_BYTES - 1), value);
            }
            else
                this.WarningLog($"TODO: Invalid memory Word write at [0x{offset:x}]");
        }
        [ConnectionRegion("RWWEEMemory")]
        public void RWWEEMemoryWriteDoubleWord(long offset, uint value) {
            if(_rwweeMemory != null) {
                this.ErrorLog($"Page [0x{offset & (MEMORY_PAGE_SIZE_BYTES - 1):x}], Offset [0x{offset:x}]");
                _pageBuffer.WriteDoubleWord(offset & (MEMORY_PAGE_SIZE_BYTES - 1), value);
            }
            else
                this.WarningLog($"TODO: Invalid memory Double Word write at [0x{offset:x}]");
        }
        [ConnectionRegion("RWWEEMemory")]
        public void RWWEEMemoryWriteQuadWord(long offset, ulong value) => throw new NotImplementedException();
        [ConnectionRegion("RWWEEMemory")]
        public void RWWEEMemoryWriteBytes(long offset, byte[] array, int startingIndex, int count, IPeripheral context = null) {
            if(_rwweeMemory != null)
                _rwweeMemory.WriteBytes(offset & (_rwweeMemory.Size - 1), array, startingIndex, count, context);
            else
                this.WarningLog($"TODO: Invalid memory bytes write at [0x{offset:x}]");
        }
        [ConnectionRegion("RWWEEMemory")]
        public byte RWWEEMemoryReadByte(long offset) {
            if(_rwweeMemory != null)
                return _rwweeMemory.ReadByte(offset & (_rwweeMemory.Size - 1));
            else
                this.WarningLog($"TODO: Invalid memory Byte read at [0x{offset:x}]");
            return 0x0;
        }
        [ConnectionRegion("RWWEEMemory")]
        public ushort RWWEEMemoryReadWord(long offset) {
            if(_rwweeMemory != null)
                return _rwweeMemory.ReadWord(offset & (_rwweeMemory.Size - 1));
            else
                this.WarningLog($"TODO: Invalid memory Word read at [0x{offset:x}]");
            return 0x0;
        }
        [ConnectionRegion("RWWEEMemory")]
        public uint RWWEEMemoryReadDoubleWord(long offset) {
            if(_rwweeMemory != null)
                return _rwweeMemory.ReadDoubleWord(offset & (_rwweeMemory.Size - 1));
            else
                this.WarningLog($"TODO: Invalid memory Double Word read at [0x{offset:x}]");
            return 0x0;
        }
        [ConnectionRegion("RWWEEMemory")]
        public byte[] RWWEEMemoryReadBytes(long offset, int count, IPeripheral context = null) => throw new NotImplementedException();
        [ConnectionRegion("RWWEEMemory")]
        public ulong RWWEEMemoryReadQuadWord(long offset) => throw new NotImplementedException();
        /**** END RWWEE MEMORY ****/

        /**** START AUX MEMORY ****/
        [ConnectionRegion("AUXMemory")]
        public void AUXWriteByte(long offset, byte value) {
            _ = value;
            this.ErrorLog($"Illegal Byte write at [0x{offset:x}].");
        }
        [ConnectionRegion("AUXMemory")]
        public void AUXWriteWord(long offset, ushort value) {
            if(_auxMemory != null) {
                this.ErrorLog($"Page [0x{offset & (MEMORY_PAGE_SIZE_BYTES - 1):x}], Offset [0x{offset:x}]");
                _pageBuffer.WriteWord(offset & (MEMORY_PAGE_SIZE_BYTES - 1), value);
            }
            else
                this.WarningLog($"TODO: Invalid memory Word write at [0x{offset:x}]");
        }
        [ConnectionRegion("AUXMemory")]
        public void AUXWriteDoubleWord(long offset, uint value) {
            if(_auxMemory != null) {
                this.ErrorLog($"Page [0x{offset & (MEMORY_PAGE_SIZE_BYTES - 1):x}], Offset [0x{offset:x}]");
                _pageBuffer.WriteDoubleWord(offset & (MEMORY_PAGE_SIZE_BYTES - 1), value);
            }
            else
                this.WarningLog($"TODO: Invalid memory Double Word write at [0x{offset:x}]");
        }
        [ConnectionRegion("AUXMemory")]
        public void AUXWriteQuadWord(long offset, ulong value) => throw new NotImplementedException();
        [ConnectionRegion("AUXMemory")]
        public void AUXWriteBytes(long offset, byte[] array, int startingIndex, int count, IPeripheral context = null) {
            if(_auxMemory != null)
                _auxMemory.WriteBytes(offset & (_auxMemory.Size - 1), array, startingIndex, count, context);
            else
                this.WarningLog($"TODO: Invalid memory bytes write at [0x{offset:x}]");
        }
        [ConnectionRegion("AUXMemory")]
        public byte AUXReadByte(long offset) {
            if(_auxMemory != null)
                return _auxMemory.ReadByte(offset & (_auxMemory.Size - 1));
            else
                this.WarningLog($"TODO: Invalid memory Byte read at [0x{offset:x}]");
            return 0x0;
        }
        [ConnectionRegion("AUXMemory")]
        public ushort AUXReadWord(long offset) {
            if(_auxMemory != null)
                return _auxMemory.ReadWord(offset & (_auxMemory.Size - 1));
            else
                this.WarningLog($"TODO: Invalid memory Word read at [0x{offset:x}]");
            return 0x0;
        }
        [ConnectionRegion("AUXMemory")]
        public uint AUXReadDoubleWord(long offset) {
            if(_auxMemory != null)
                return _auxMemory.ReadDoubleWord(offset & (_auxMemory.Size - 1));
            else
                this.WarningLog($"TODO: Invalid memory Double Word read at [0x{offset:x}]");
            return 0x0;
        }
        [ConnectionRegion("AUXMemory")]
        public byte[] AUXReadBytes(long offset, int count, IPeripheral context = null) => throw new NotImplementedException();
        [ConnectionRegion("AUXMemory")]
        public ulong AUXReadQuadWord(long offset) => throw new NotImplementedException();
        /**** END AUX MEMORY ****/

        public void SetAbsoluteAddress(ulong address) {
            if(address >= 0x0 && address <= (ulong)(0x20000000 - (sizeof(ushort))))
                _addr.Value = (ulong)(address >> 1) & 0x1FFFFF;
            _absoluteAddress = address;
        }

        public new void Reset() {
            _byteRegisters.Reset();
            _wordRegisters.Reset();
            _doubleWordRegisters.Reset();
            _pageBuffer.ZeroAll();
        }

        public void Register(MappedMemory peripheral, NumberRegistrationPoint<ulong> registrationPoint) {
            ArgumentNullException.ThrowIfNull(peripheral);
            ArgumentNullException.ThrowIfNull(registrationPoint);

            MemoryRegionBaseAddr region = (MemoryRegionBaseAddr)registrationPoint.Address;

            peripheral.ResetByte = 0xFF;
            peripheral.ZeroAll();

            switch(region) {
            case MemoryRegionBaseAddr.Main:
                if(_mainMemory != null)
                    throw new Exception($"{region} region already registered.");
                _mainMemory = peripheral;
                registerMemoryRegion(_mainMemory, registrationPoint, "MainMemory");
                break;
            case MemoryRegionBaseAddr.RWWEE:
                if(_rwweeMemory != null)
                    throw new Exception($"{region} region already registered.");
                _rwweeMemory = peripheral;
                registerMemoryRegion(_rwweeMemory, registrationPoint, "RWWEEMemory");
                break;
            case MemoryRegionBaseAddr.AUX:
                if(_auxMemory != null)
                    throw new Exception($"{region} region already registered.");
                _auxMemory = peripheral;
                registerMemoryRegion(_auxMemory, registrationPoint, $"AUXMemory");
                break;
            default:
                throw new ArgumentException($"Invalid Registration Point 0x{registrationPoint.Address:x}");
            }
        }

        private void registerMemoryRegion(MappedMemory memoryRegion, NumberRegistrationPoint<ulong> registrationPoint, string regionName) {
            _machine.RegisterAsAChildOf(this, memoryRegion, registrationPoint);
            _machine.SystemBus.Register(this, new Bus.BusMultiRegistration((ulong)registrationPoint.Address, (ulong)memoryRegion.Size, regionName));
        }

        public void Unregister(MappedMemory peripheral) {
            throw new NotImplementedException();
        }

        private void DefineRegisters() {
            _wordRegisters.DefineRegister((long)Registers.CTRLA);
            _wordRegisters.AddAfterWriteHook((long)Registers.CTRLA, CommandExecution);

            _doubleWordRegisters.DefineRegister((long)Registers.CTRLB, 0x80)
            .WithFlag(7, out _manualWrite);


            _doubleWordRegisters.DefineRegister((long)Registers.PARAM); // Reset value depend on NVM User row

            _byteRegisters.AddRegister((long)Registers.INTENCLR, _interruptsManager.GetInterruptEnableClearRegister<ByteRegister>());
            _byteRegisters.AddRegister((long)Registers.INTENSET, _interruptsManager.GetInterruptEnableSetRegister<ByteRegister>());
            _byteRegisters.AddRegister((long)Registers.INTFLAG, _interruptsManager.GetRegister<ByteRegister>(writeCallback: (irq, oldValue, newValue) => {
                if(newValue && irq != Interrupts.Ready)
                    _interruptsManager.ClearInterrupt(irq);
            }, valueProviderCallback: (irq, _) => {
                return _interruptsManager.IsSet(irq);
            }));

            _wordRegisters.DefineRegister((long)Registers.STATUS); // SB determined by value in NV Memory
                                                                   // .WithFlag(0, FieldMode.Read, name: "PRM")
                                                                   // .WithFlag(1, FieldMode.Read | FieldMode.WriteOneToClear, name: "LOAD")
                                                                   // .WithFlag(2, FieldMode.Read | FieldMode.WriteOneToClear, name: "PROGE")
                                                                   // .WithFlag(3, FieldMode.Read | FieldMode.WriteOneToClear, name: "LOCKE")
                                                                   // .WithFlag(4, FieldMode.Read | FieldMode.WriteOneToClear, name: "NVME")
                                                                   // .WithIgnoredBits(5, 3)
                                                                   // .WithFlag(8, FieldMode.Read, name: "SB")
                                                                   // .WithIgnoredBits(9, 7);

            _doubleWordRegisters.DefineRegister((long)Registers.ADDR)
                .WithValueField(0, 21, out _addr, name: "ADDR")
                .WithIgnoredBits(21, 11);

            _wordRegisters.DefineRegister((long)Registers.LOCK); // Reset value determined by NV memory user row
        }

        private IMemory GetMemoryByAddress(long offset) {
            try {
                if(offset >= (long)MemoryRegionBaseAddr.Main && offset < _mainMemory.Size)
                    return _mainMemory;
            }
            catch(NullReferenceException) {
                this.ErrorLog("Main Memory not set. See .repl file.");
            }

            try {
                if(offset >= (long)MemoryRegionBaseAddr.RWWEE && offset - (long)MemoryRegionBaseAddr.RWWEE < _rwweeMemory.Size)
                    return _rwweeMemory;
            }
            catch(NullReferenceException) {
                this.ErrorLog("RWWEE Memory not set. See .repl file.");
            }

            try {
                if(offset >= (long)MemoryRegionBaseAddr.AUX && offset - (long)MemoryRegionBaseAddr.AUX < _auxMemory.Size)
                    return _auxMemory;
            }
            catch(NullReferenceException) {
                this.ErrorLog("AUX Memory not set. See .repl file.");
            }
            return null;
        }

        private static void Erase(IMemory accessedMemory) {
            for(long i = 0; i < accessedMemory.Size / 0x8; i++) {
                accessedMemory.WriteQuadWord(0x8 * i, ulong.MaxValue);
            }
        }
        private void EraseRow(IMemory memory) {
            this.InfoLog($"Erase Row [{_rowNumber}] ");
            memory.WriteBytes((long)_rowNumber * (MEMORY_PAGE_SIZE_BYTES * 4),
                Enumerable.Repeat<byte>(0xFF, MEMORY_PAGE_SIZE_BYTES * 4).ToArray(),
                0,
                MEMORY_PAGE_SIZE_BYTES * 4
            );
        }

        private void WriteToMemory(IMemory memory) {
            ArgumentNullException.ThrowIfNull(memory);

            long offset = (long)((_rowNumber * (MEMORY_PAGE_SIZE_BYTES * 4)) + (_page * MEMORY_PAGE_SIZE_BYTES));
            for(int index = 0; index < _pageBuffer.Size / 8; index++) {
                ulong currentData = memory.ReadQuadWord(offset + (index * sizeof(ulong)));
                memory.WriteQuadWord(offset + (index * sizeof(ulong)), currentData & _pageBuffer.ReadQuadWord(sizeof(ulong) * index));
            }
            _pageBuffer.ZeroAll();
        }

        private void CommandExecution(long offset, ushort value) {
            Command cmd = (Command)(value & 0x7F);
            int CommandExecution = value >> 8;
            if(CommandExecution == 0xA5) {
                switch(cmd) {
                case Command.ER:
                    EraseRow(_mainMemory);
                    break;
                case Command.WP:
                    WriteToMemory(_mainMemory);
                    break;
                case Command.EAR:
                    EraseRow(_auxMemory);
                    break;
                case Command.WAR:
                    WriteToMemory(_auxMemory);
                    break;
                case Command.RWWEEER:
                    EraseRow(_rwweeMemory);
                    break;
                case Command.RWWEEWP:
                    WriteToMemory(_rwweeMemory);
                    break;
                case Command.PBC:
                    _pageBuffer.ZeroAll();
                    break;
                default:
                    this.WarningLog($"Command {cmd} is not supported.");
                    break;
                }
            }
            else {
                this.ErrorLog("An invalid Keyword was writtern in the NVM Command register.");
            }
        }

        public new long Size => 0x200;
        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

        private readonly Machine _machine;
        private readonly InterruptManager<Interrupts> _interruptsManager;
        private readonly ByteRegisterCollection _byteRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly MappedMemory _pageBuffer;

        private MappedMemory _mainMemory;
        private MappedMemory _rwweeMemory;
        private MappedMemory _auxMemory;

        private ulong _absoluteAddress;

        // Registers fields
        private IFlagRegisterField _manualWrite;
        private IValueRegisterField _addr;


        private const int MEMORY_PAGE_SIZE_BYTES = 64;

        private ulong _writeOffset => _addr.Value << 1;
        private ulong _rowNumber => _writeOffset / (MEMORY_PAGE_SIZE_BYTES * 4);
        private ulong _page => (_writeOffset - (_rowNumber * MEMORY_PAGE_SIZE_BYTES * 4)) / MEMORY_PAGE_SIZE_BYTES;

        private enum Command {
            ER = 0x02,
            WP = 0x04,
            EAR = 0x05,
            WAR = 0x06,
            RWWEEER = 0x1A,
            RWWEEWP = 0x1C,
            LR = 0x40,
            UL = 0x41,
            SPRM = 0x42,
            CPRM = 0x43,
            PBC = 0x44,
            SSB = 0x45,
            INVALL = 0x46,
        }

        // TODO: Add user row const (AUX Bse + 0x4000)
        private enum MemoryRegionBaseAddr {
            Main = 0x0,
            RWWEE = 0x400000,
            AUX = 0x800000
        }

        private enum Registers : long {
            CTRLA = 0x0,
            CTRLB = 0x04,
            PARAM = 0x08,
            INTENCLR = 0x0C,
            INTENSET = 0x10,
            INTFLAG = 0x14,
            STATUS = 0x18,
            ADDR = 0x1C,
            LOCK = 0x20
        }

        private enum Interrupts {
            Ready = 0,
            Error = 1
        }

    }
}
