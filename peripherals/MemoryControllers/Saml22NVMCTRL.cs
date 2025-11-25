using System;
using System.Collections.Generic;

using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.Memory;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.MemoryControllers {
    public class Saml22NVMCTRL : IBytePeripheral, IWordPeripheral, IDoubleWordPeripheral,
        IKnownSize,
        IPeripheralRegister<Saml22NVM, NumberRegistrationPoint<ulong>> {

        public Saml22NVMCTRL(Machine machine) {
            _machine = machine;

            _interruptsManager = new InterruptManager<Interrupts>(this);

            _byteRegisters = new ByteRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _doubleWordRegisters = new DoubleWordRegisterCollection(this);

            _memoryRegions = new Dictionary<long, Saml22NVM>();

            _pageBuffer = new MappedMemory(_machine, MEMORY_PAGE_SIZE_BYTES);
            _pageBuffer.ResetByte = 0xFF;
            _pageBuffer.ZeroAll();

            DefineRegisters();

            _interruptsManager.SetInterrupt(Interrupts.Ready);
        }

        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);
        public void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);


        private void NVMWriteByteHandler(long address, byte value) {
            _ = value;
            this.ErrorLog($"Invalid operatio. Byte Write at 0x{address:x} value 0x{value:x}.");
        }
        private void NVMWriteWordHandler(long address, ushort value) {
            SetAddress(address);
            _pageBuffer.WriteWord(address & (MEMORY_PAGE_SIZE_BYTES - 1), value);
        }
        private void NVMWriteDoubleWordHandler(long address, uint value) {
            SetAddress(address);
            _pageBuffer.WriteDoubleWord(address & (MEMORY_PAGE_SIZE_BYTES - 1), value);
        }

        public void SetAddress(long address) {
            _addr = (ulong)(address & 0x1FFFFF) >> 1;
        }

        public void Reset() {
            _pageBuffer.ZeroAll();
            _byteRegisters.Reset();
            _wordRegisters.Reset();
            _doubleWordRegisters.Reset();
        }

        public void Register(Saml22NVM peripheral, NumberRegistrationPoint<ulong> registrationPoint) {
            ArgumentNullException.ThrowIfNull(peripheral);
            ArgumentNullException.ThrowIfNull(registrationPoint);

            peripheral.ByteWrite += NVMWriteByteHandler;
            peripheral.WordWrite += NVMWriteWordHandler;
            peripheral.DoubleWordWrite += NVMWriteDoubleWordHandler;
            peripheral.Fill(0xFF);

            _machine.RegisterAsAChildOf(this, peripheral, registrationPoint);
            _machine.SystemBus.Register(peripheral, new Bus.BusRangeRegistration((ulong)registrationPoint.Address, (ulong)peripheral.Size));

            _memoryRegions.Add((long)registrationPoint.Address, peripheral);
        }
        public void Unregister(Saml22NVM peripheral) {
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
                .WithValueField(0, 21, writeCallback: (_, value) => _addr = value, valueProviderCallback: (_) => _addr, name: "ADDR")
                .WithIgnoredBits(21, 11);

            _wordRegisters.DefineRegister((long)Registers.LOCK); // Reset value determined by NV memory user row
        }


        private void Erase(MemoryRegionBase regionBase) => _memoryRegions[(long)regionBase].Fill(0xFF);
        private void EraseRow(MemoryRegionBase regionBase) {
            this.InfoLog($"Erase Row [{_rowNumber}] ");
            _memoryRegions[(long)regionBase].FillRegion(0xFF,
                                                        (int)_rowNumber * (MEMORY_PAGE_SIZE_BYTES * 4),
                                                        MEMORY_PAGE_SIZE_BYTES * 4);
        }

        private void WriteToMemory(MemoryRegionBase regionBase) {
            long offset = (long)((_rowNumber * (MEMORY_PAGE_SIZE_BYTES * 4)) + (_page * MEMORY_PAGE_SIZE_BYTES));
            for(int index = 0; index < _pageBuffer.Size / 8; index++) {
                ulong currentData = _memoryRegions[(long)regionBase].ReadQuadWord(offset + (index * sizeof(ulong)));
                _memoryRegions[(long)regionBase].WriteQuadWord(offset + (index * sizeof(ulong)),
                                                                currentData & _pageBuffer.ReadQuadWord(sizeof(ulong) * index));
            }
            _pageBuffer.ZeroAll();
        }

        private void CommandExecution(long offset, ushort value) {
            Command cmd = (Command)(value & 0x7F);
            int CommandExecution = value >> 8;
            if(CommandExecution == 0xA5) {
                switch(cmd) {
                case Command.ER:
                    EraseRow(MemoryRegionBase.InternalFlash);
                    break;
                case Command.WP:
                    WriteToMemory(MemoryRegionBase.InternalFlash);
                    break;
                case Command.EAR:
                    EraseRow(MemoryRegionBase.AUX);
                    break;
                case Command.WAR:
                    WriteToMemory(MemoryRegionBase.AUX);
                    break;
                case Command.RWWEEER:
                    EraseRow(MemoryRegionBase.RWWEE);
                    break;
                case Command.RWWEEWP:
                    WriteToMemory(MemoryRegionBase.RWWEE);
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

        public long Size => 0x200;
        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

        private readonly Machine _machine;
        private readonly InterruptManager<Interrupts> _interruptsManager;
        private readonly ByteRegisterCollection _byteRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly MappedMemory _pageBuffer;

        private readonly Dictionary<long, Saml22NVM> _memoryRegions;

        private ulong _addr;

        // Registers fields
        private IFlagRegisterField _manualWrite;


        private const int MEMORY_PAGE_SIZE_BYTES = 64;

        private ulong _writeOffset => _addr << 1;
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
        private enum MemoryRegionBase {
            InternalFlash = 0x0,
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
