using System;
using System.Linq;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.Memory;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.MemoryControllers
{
    public class Saml22NVMCTRL : IBytePeripheral, IWordPeripheral, IDoubleWordPeripheral, IKnownSize, IAbsoluteAddressAware
    {
        public Saml22NVMCTRL(Machine machine)
        {
            this.WarningLog("NVMCTRL works on RWWEE and AUX space.");
            _machine = machine;

            _interruptsManager = new InterruptManager<Interrupts>(this);

            _byteRegisters = new ByteRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _doubleWordRegisters = new DoubleWordRegisterCollection(this);

            _rwweeMemory = new ArrayMemory(0x2000);
            Erase(_rwweeMemory);
            _auxMemory = new ArrayMemory(0xA100);
            Erase(_auxMemory);

            _pageBuffer = new PageBuffer(this);

            DefineRegisters();

            _interruptsManager.SetInterrupt(Interrupts.Ready);
        }

        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);
        public void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);

        [ConnectionRegion("RWWEE")]
        public byte ReadByteRWWEE(long offset) => _rwweeMemory.ReadByte(offset);
        [ConnectionRegion("RWWEE")]
        public ushort ReadWordRWWEE(long offset) => _rwweeMemory.ReadWord(offset);
        [ConnectionRegion("RWWEE")]
        public uint ReadDoubleWordRWWEE(long offset) => _rwweeMemory.ReadDoubleWord(offset);
        [ConnectionRegion("RWWEE")]
        public void WriteByteRWWEE(long offset, byte value) => _interruptsManager.SetInterrupt(Interrupts.Error);
        [ConnectionRegion("RWWEE")]
        public void WriteWordRWWEE(long offset, ushort value) => _pageBuffer.Load(value);
        [ConnectionRegion("RWWEE")]
        public void WriteDoubleWordRWWEE(long offset, uint value) => this.WarningLog("32-bit write to pageBuffer not implemented.");

        [ConnectionRegion("AUX")]
        public byte ReadByteAUX(long offset) => _auxMemory.ReadByte(offset);
        [ConnectionRegion("AUX")]
        public ushort ReadWordAUX(long offset) => _auxMemory.ReadWord(offset);
        [ConnectionRegion("AUX")]
        public uint ReadDoubleWordAUX(long offset) => _auxMemory.ReadDoubleWord(offset);
        [ConnectionRegion("AUX")]
        public void WriteByteAUX(long offset, byte value) => _interruptsManager.SetInterrupt(Interrupts.Error);
        [ConnectionRegion("AUX")]
        public void WriteWordAUX(long offset, ushort value) => _pageBuffer.Load(value);
        [ConnectionRegion("AUX")]
        public void WriteDoubleWordAUX(long offset, uint value) => this.WarningLog("32-bit write to pageBuffer not implemented.");


        public void SetAbsoluteAddress(ulong address)
        {
            _sector = (Sector)(address & 0xFFFF0000);

            if (_sector != Sector.Control)
            {
                _memorySector = _sector;
                _addr.Value = (address & 0x1FFFF) >> 1;
            }
        }

        public void Reset()
        {
            _byteRegisters.Reset();
            _wordRegisters.Reset();
            _doubleWordRegisters.Reset();
            _pageBuffer.Clear();
        }

        private void DefineRegisters()
        {
            _wordRegisters.DefineRegister((long)Registers.CTRLA);
            _wordRegisters.AddAfterWriteHook((long)Registers.CTRLA, CommandExecution);

            _doubleWordRegisters.DefineRegister((long)Registers.CTRLB, 0x80)
            .WithFlag(7, out _manualWrite);


            _doubleWordRegisters.DefineRegister((long)Registers.PARAM); // Reset value depend on NVM User row

            _byteRegisters.AddRegister((long)Registers.INTENCLR, _interruptsManager.GetInterruptEnableClearRegister<ByteRegister>());
            _byteRegisters.AddRegister((long)Registers.INTENSET, _interruptsManager.GetInterruptEnableSetRegister<ByteRegister>());
            _byteRegisters.AddRegister((long)Registers.INTFLAG, _interruptsManager.GetRegister<ByteRegister>(writeCallback: (irq, oldValue, newValue) =>
            {
                if (newValue && irq != Interrupts.Ready)
                    _interruptsManager.ClearInterrupt(irq);
            }, valueProviderCallback: (irq, _) =>
            {
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
                .WithValueField(0, 17, out _addr, name: "ADDR")
                .WithIgnoredBits(17, 15);

            _wordRegisters.DefineRegister((long)Registers.LOCK); // Reset value determined by NV memory user row
        }

        private static void Erase(ArrayMemory accessedMemory)
        {
            for (long i = 0; i < accessedMemory.Size / 0x8; i++)
            {
                accessedMemory.WriteQuadWord(0x8 * i, ulong.MaxValue);
            }
        }
        private void EraseRow(ArrayMemory memory)
        {
            this.InfoLog($"Erase Row [{Row}] ");
            memory.WriteBytes((long)Row * (MEMORY_PAGE_SIZE_BYTES * 4),
                Enumerable.Repeat<byte>(0xFF, MEMORY_PAGE_SIZE_BYTES * 4).ToArray(),
                0,
                MEMORY_PAGE_SIZE_BYTES * 4
            );
        }

        private void WriteToMemory(ArrayMemory memory)
        {
            ArgumentNullException.ThrowIfNull(memory);

            long offset = (long)((Row * (MEMORY_PAGE_SIZE_BYTES * 4)) + (Page * MEMORY_PAGE_SIZE_BYTES));
            for (int index = 0; index < _pageBuffer.Buffer.Length; index++)
            {
                ulong currentData = memory.ReadQuadWord(offset + (index * sizeof(ulong)));
                memory.WriteQuadWord(offset + (index * sizeof(ulong)), currentData & _pageBuffer.Buffer[index]);
            }
            _pageBuffer.Clear();
        }

        private void CommandExecution(long offset, ushort value)
        {
            Command cmd = (Command)(value & 0x7F);
            int CommandExecution = value >> 8;
            if (CommandExecution == 0xA5)
            {
                switch (cmd)
                {
                    case Command.ER:
                    case Command.WP:
                        // TODO: How to get offset at which Main Array was accessed?
                        this.WarningLog("NVMCTRL can't operate on Main Array.");
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
                        _pageBuffer.Clear();
                        break;
                    default:
                        this.WarningLog($"Command {cmd} is not supported.");
                        break;
                }
            }
            else
            {
                this.ErrorLog("An invalid Keyword was writtern in the NVM Command register.");
            }
        }

        private readonly Machine _machine;
        private readonly InterruptManager<Interrupts> _interruptsManager;
        private readonly ByteRegisterCollection _byteRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly PageBuffer _pageBuffer;
        private readonly ArrayMemory _rwweeMemory;
        private readonly ArrayMemory _auxMemory;

        private Sector _sector;
        private Sector _memorySector;

        // Registers fields
        private IFlagRegisterField _manualWrite;
        private IValueRegisterField _addr;


        private const int MEMORY_PAGE_SIZE_BYTES = 64;
        private const long BASE_ADDR_OFFSET = 0x40000;
        private const long RWW_SECTOR_BASE_ADDR = 0x400000;
        private const long CALIB_AUX_BASE_ADDR = 0x800000;
        private const long APB_BRIDGE_PBASE_ADDR = 0x41000000;


        public long Size => 0x2000;

        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

        private ulong WriteOffset => _addr.Value << 1;
        private ulong Row => WriteOffset / (MEMORY_PAGE_SIZE_BYTES * 4);
        private ulong Page => (WriteOffset - (Row * MEMORY_PAGE_SIZE_BYTES * 4)) / MEMORY_PAGE_SIZE_BYTES;

        private enum Command
        {
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

        private enum Sector
        {
            MainArray = 0x0,
            RWWEE = 0x00400000,
            AUX = 0x00800000,
            Control = 0x41000000
        }

        private enum Registers : long
        {
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

        private enum Interrupts
        {
            Ready = 0,
            Error = 1
        }

        private sealed class PageBuffer
        {

            public PageBuffer(Saml22NVMCTRL parent)
            {
                _parent = parent;
                Clear();
            }

            public void Load(ushort data)
            {
                // Random access writes to 32-bit words within the page buffer will overwrite the opposite word within the same 64-bit section with ones.
                if (IsBoundaryCrossed)
                    Buffer[DoubleWordDataIndex] = 0xFFFF_FFFF;

                if (WordDataIndex % 2 == 0)
                {
                    Buffer[DoubleWordDataIndex] &= 0xFFFF_0000;
                    Buffer[DoubleWordDataIndex] |= data | 0xFFFF_0000;
                }
                else
                {
                    Buffer[DoubleWordDataIndex] &= 0x0000_FFFF;
                    Buffer[DoubleWordDataIndex] |= ((uint)data) << 16;
                }
                // parent.WarningLog($"pageBuffer[{Page32BitDataIndex}] [0x{page[Page32BitDataIndex]:X8}]");
                _previousPage32BitDataIndex = DoubleWordDataIndex;
                if (!_parent._manualWrite.Value && WordDataIndex >= 15)
                {
                    switch (_parent._memorySector)
                    {
                        case Sector.RWWEE:
                            _parent.WriteToMemory(_parent._rwweeMemory);
                            break;
                        case Sector.AUX:
                            _parent.WriteToMemory(_parent._auxMemory);
                            break;
                    }
                }
            }

            public void Clear()
            {
                for (int index = 0; index < Buffer.Length; index++)
                    Buffer[index] = uint.MaxValue;
                _previousPage32BitDataIndex = 0;
            }

            private readonly Saml22NVMCTRL _parent;
            private long _previousPage32BitDataIndex;

            public uint[] Buffer { get; } = new uint[16];
            private long DoubleWordDataIndex => (long)(_parent.WriteOffset & (MEMORY_PAGE_SIZE_BYTES - 1)) / sizeof(uint);
            private long WordDataIndex => (long)(_parent.WriteOffset & (MEMORY_PAGE_SIZE_BYTES - 1)) / sizeof(ushort);
            private bool IsBoundaryCrossed => DoubleWordDataIndex != _previousPage32BitDataIndex;
        }
    }
}
