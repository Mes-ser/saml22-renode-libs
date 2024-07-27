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
        public long Size => 0x2000;

        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

        public void Reset()
        {
            byteRegisters.Reset();
            wordRegisters.Reset();
            doubleWordRegisters.Reset();
            pageBuffer.Clear();
        }

        public byte ReadByte(long offset)
        {
            switch (sector)
            {
                case Sector.MainArray:
                    // Flash read
                    break;
                case Sector.RWWEE:
                    return rwweeMemory.ReadByte(offset);
                case Sector.AUX:
                    return auxiliaryMemory.ReadByte(offset);
                case Sector.Control:
                    return byteRegisters.Read(offset);
            }
            return 0x0;
        }
        public ushort ReadWord(long offset)
        {
            switch (sector)
            {
                case Sector.MainArray:
                    // Flash read
                    break;
                case Sector.RWWEE:
                    return rwweeMemory.ReadWord(offset);
                case Sector.AUX:
                    return auxiliaryMemory.ReadWord(offset);
                case Sector.Control:
                    return wordRegisters.Read(offset);
            }
            return 0x0;
        }
        public uint ReadDoubleWord(long offset)
        {

            switch (sector)
            {
                case Sector.MainArray:
                    // Flash read
                    break;
                case Sector.RWWEE:
                    return rwweeMemory.ReadDoubleWord(offset);
                case Sector.AUX:
                    return auxiliaryMemory.ReadDoubleWord(offset);
                case Sector.Control:
                    return doubleWordRegisters.Read(offset);
            }
            return 0x0;
        }
        public void WriteByte(long offset, byte value)
        {
            switch (sector)
            {
                case Sector.MainArray:
                case Sector.RWWEE:
                case Sector.AUX:
                    interruptsManager.SetInterrupt(Interrupts.Error);
                    break;
                case Sector.Control:
                    byteRegisters.Write(offset, value);
                    break;
            }

        }
        public void WriteWord(long offset, ushort value)
        {
            switch (sector)
            {
                case Sector.MainArray:
                case Sector.RWWEE:
                case Sector.AUX:
                    pageBuffer.Load(value);
                    break;
                case Sector.Control:
                    wordRegisters.Write(offset, value);
                    break;
            }
        }
        public void WriteDoubleWord(long offset, uint value)
        {
            switch (sector)
            {
                case Sector.MainArray:
                case Sector.RWWEE:
                case Sector.AUX:
                    this.WarningLog("32-bit write to pageBuffer not implemented.");
                    break;
                case Sector.Control:
                    doubleWordRegisters.Write(offset, value);
                    break;
            }
        }

        public Saml22NVMCTRL(Machine machine)
        {
            this.WarningLog("NVMCTRL works on RWWEE and AUX space.");
            this.machine = machine;

            interruptsManager = new InterruptManager<Interrupts>(this);

            byteRegisters = new ByteRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            doubleWordRegisters = new DoubleWordRegisterCollection(this);

            rwweeMemory = new ArrayMemory(0x2000);
            Erase(rwweeMemory);
            auxiliaryMemory = new ArrayMemory(0xA100);
            Erase(auxiliaryMemory);

            pageBuffer = new PageBuffer(this);

            DefineRegisters();

            interruptsManager.SetInterrupt(Interrupts.Ready);
        }

        private const int MEMORY_PAGE_SIZE_BYTES = 64;
        private const long BASE_ADDR_OFFSET = 0x40000;
        private const long RWW_SECTOR_BASE_ADDR = 0x400000;
        private const long CALIB_AUX_BASE_ADDR = 0x800000;
        private const long APB_BRIDGE_PBASE_ADDR = 0x41000000;

        private readonly Machine machine;
        private readonly InterruptManager<Interrupts> interruptsManager;
        private readonly ByteRegisterCollection byteRegisters;
        private readonly WordRegisterCollection wordRegisters;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly PageBuffer pageBuffer;
        private readonly ArrayMemory rwweeMemory;
        private readonly ArrayMemory auxiliaryMemory;

        private Sector sector;
        private Sector memorySector;


        // Registers fields
        private IFlagRegisterField manualWrite;
        private IValueRegisterField ADDR;


        private void DefineRegisters()
        {
            wordRegisters.DefineRegister((long)Registers.CTRLA);
            wordRegisters.AddAfterWriteHook((long)Registers.CTRLA, CommandExecution);

            doubleWordRegisters.DefineRegister((long)Registers.CTRLB, 0x80)
            .WithFlag(7, out manualWrite);


            doubleWordRegisters.DefineRegister((long)Registers.PARAM); // Reset value depend on NVM User row

            byteRegisters.AddRegister((long)Registers.INTENCLR, interruptsManager.GetInterruptEnableClearRegister<ByteRegister>());
            byteRegisters.AddRegister((long)Registers.INTENSET, interruptsManager.GetInterruptEnableSetRegister<ByteRegister>());
            byteRegisters.AddRegister((long)Registers.INTFLAG, interruptsManager.GetRegister<ByteRegister>(writeCallback: (irq, oldValue, newValue) =>
            {
                if (newValue && irq != Interrupts.Ready)
                    interruptsManager.ClearInterrupt(irq);
            }, valueProviderCallback: (irq, _) =>
            {
                return interruptsManager.IsSet(irq);
            }));

            wordRegisters.DefineRegister((long)Registers.STATUS); // SB determined by value in NV Memory
                                                                  // .WithFlag(0, FieldMode.Read, name: "PRM")
                                                                  // .WithFlag(1, FieldMode.Read | FieldMode.WriteOneToClear, name: "LOAD")
                                                                  // .WithFlag(2, FieldMode.Read | FieldMode.WriteOneToClear, name: "PROGE")
                                                                  // .WithFlag(3, FieldMode.Read | FieldMode.WriteOneToClear, name: "LOCKE")
                                                                  // .WithFlag(4, FieldMode.Read | FieldMode.WriteOneToClear, name: "NVME")
                                                                  // .WithIgnoredBits(5, 3)
                                                                  // .WithFlag(8, FieldMode.Read, name: "SB")
                                                                  // .WithIgnoredBits(9, 7);

            doubleWordRegisters.DefineRegister((long)Registers.ADDR)
                .WithValueField(0, 17, out ADDR, name: "ADDR")
                .WithIgnoredBits(17, 15);

            wordRegisters.DefineRegister((long)Registers.LOCK); // Reset value determined by NV memory user row
        }

        public void SetAbsoluteAddress(ulong address)
        {
            sector = (Sector)(address & 0xFFFF0000);

            if (sector != Sector.Control)
            {
                memorySector = sector;
                ADDR.Value = (address & 0x1FFFF) >> 1;
            }
        }

        private ulong WriteOffset => ADDR.Value << 1;
        private ulong Row => WriteOffset / (MEMORY_PAGE_SIZE_BYTES * 4);
        private ulong Page => (WriteOffset - (Row * MEMORY_PAGE_SIZE_BYTES * 4)) / MEMORY_PAGE_SIZE_BYTES;

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
                        EraseRow(auxiliaryMemory);
                        break;
                    case Command.WAR:
                        WriteToMemory(auxiliaryMemory);
                        break;
                    case Command.RWWEEER:
                        EraseRow(rwweeMemory);
                        break;
                    case Command.RWWEEWP:
                        WriteToMemory(rwweeMemory);
                        break;
                    case Command.PBC:
                        pageBuffer.Clear();
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
            if (memory == null)
                throw new ArgumentNullException(nameof(memory));

            long offset = (long)((Row * (MEMORY_PAGE_SIZE_BYTES * 4)) + (Page * MEMORY_PAGE_SIZE_BYTES));
            for (int index = 0; index < pageBuffer.Buffer.Length; index++)
            {
                ulong currentData = memory.ReadQuadWord(offset + (index * sizeof(ulong)));
                memory.WriteQuadWord(offset + (index * sizeof(ulong)), currentData & pageBuffer.Buffer[index]);
            }
            pageBuffer.Clear();
        }

        private sealed class PageBuffer
        {

            public uint[] Buffer => page;

            public void Load(ushort data)
            {
                // Random access writes to 32-bit words within the page buffer will overwrite the opposite word within the same 64-bit section with ones.
                if (IsBoundaryCrossed)
                    page[DoubleWordDataIndex] = 0xFFFF_FFFF;

                if (WordDataIndex % 2 == 0)
                {
                    page[DoubleWordDataIndex] &= 0xFFFF_0000;
                    page[DoubleWordDataIndex] |= data | 0xFFFF_0000;
                }
                else
                {
                    page[DoubleWordDataIndex] &= 0x0000_FFFF;
                    page[DoubleWordDataIndex] |= ((uint)data) << 16;
                }
                // parent.WarningLog($"pageBuffer[{Page32BitDataIndex}] [0x{page[Page32BitDataIndex]:X8}]");
                previousPage32BitDataIndex = DoubleWordDataIndex;
                if (!parent.manualWrite.Value && WordDataIndex >= 15)
                {
                    switch (parent.memorySector)
                    {
                        case Sector.RWWEE:
                            parent.WriteToMemory(parent.rwweeMemory);
                            break;
                        case Sector.AUX:
                            parent.WriteToMemory(parent.auxiliaryMemory);
                            break;
                    }
                }
            }

            public void Clear()
            {
                for (int index = 0; index < page.Length; index++)
                    page[index] = uint.MaxValue;
                previousPage32BitDataIndex = 0;
            }

            public PageBuffer(Saml22NVMCTRL parent)
            {
                this.parent = parent;
                Clear();
            }

            private readonly Saml22NVMCTRL parent;
            private readonly uint[] page = new uint[16];

            private long previousPage32BitDataIndex;
            private long DoubleWordDataIndex => (long)(parent.WriteOffset & (MEMORY_PAGE_SIZE_BYTES - 1)) / sizeof(uint);
            private long WordDataIndex => (long)(parent.WriteOffset & (MEMORY_PAGE_SIZE_BYTES - 1)) / sizeof(ushort);
            private bool IsBoundaryCrossed => DoubleWordDataIndex != previousPage32BitDataIndex;
        }

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

    }
}
