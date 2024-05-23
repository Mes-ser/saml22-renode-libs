

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

        public string ImageFile
        {
            get => ImageFile;
            set
            {

            }
        }

        public void Reset()
        {
            byteRegisters.Reset();
            wordRegisters.Reset();
            doubleWordRegisters.Reset();
        }

        public byte ReadByte(long offset)
        {
            switch (accessRegion)
            {
                case AccessRegion.CommandAndControl:
                    return byteRegisters.Read(offset);
                case AccessRegion.CalibAndAux:
                    offset += BASE_ADDR_OFFSET - CALIB_AUX_BASE_ADDR;
                    this.DebugLog($"CalibAux [Byte][Read] at [0x{offset:x}]");
                    return 0x0;
                case AccessRegion.RWWMemory:
                    offset += BASE_ADDR_OFFSET - RWW_SECTOR_BASE_ADDR;
                    this.DebugLog($"RWW [Byte][Read] at [0x{offset:x}]");
                    return rwwMemory.ReadByte(offset);
                default:
                    return 0;
            }
        }
        public ushort ReadWord(long offset)
        {
            switch (accessRegion)
            {
                case AccessRegion.CommandAndControl:
                    return wordRegisters.Read(offset);
                case AccessRegion.CalibAndAux:
                    offset += BASE_ADDR_OFFSET - CALIB_AUX_BASE_ADDR;
                    this.DebugLog($"CalibAux [Word][Read] at [0x{offset:x}]");
                    return 0x0;
                case AccessRegion.RWWMemory:
                    offset += BASE_ADDR_OFFSET - RWW_SECTOR_BASE_ADDR;
                    this.DebugLog($"RWW [Word][Read] at [0x{offset:x}]");
                    return rwwMemory.ReadWord(offset);
                default:
                    return 0;
            }
        }
        public uint ReadDoubleWord(long offset)
        {
            switch (accessRegion)
            {
                case AccessRegion.CommandAndControl:
                    return doubleWordRegisters.Read(offset);
                case AccessRegion.CalibAndAux:
                    offset += BASE_ADDR_OFFSET - CALIB_AUX_BASE_ADDR;
                    this.DebugLog($"CalibAux [DWord][Read] at [0x{offset:x}]");
                    return 0x0;
                case AccessRegion.RWWMemory:
                    offset += BASE_ADDR_OFFSET - RWW_SECTOR_BASE_ADDR;
                    this.DebugLog($"RWW [DWord][Read] at [0x{offset:x}]");
                    return rwwMemory.ReadDoubleWord(offset);
                default:
                    return 0;
            }
        }
        public void WriteByte(long offset, byte value)
        {
            switch (accessRegion)
            {
                case AccessRegion.CommandAndControl:
                    byteRegisters.Write(offset, value);
                    break;
                case AccessRegion.CalibAndAux:
                    offset += BASE_ADDR_OFFSET - CALIB_AUX_BASE_ADDR;
                    this.DebugLog($"CalibAux [Byte][Write] at [0x{offset:x}] - [0x{value:x}]");
                    break;
                case AccessRegion.RWWMemory:
                    offset += BASE_ADDR_OFFSET - RWW_SECTOR_BASE_ADDR;
                    rwwMemory.WriteByte(offset, value);
                    this.DebugLog($"RWW [Byte][Write] at [0x{offset:x}] - [0x{value:x}]");
                    break;
                default:
                    break;
            }
        }
        public void WriteWord(long offset, ushort value)
        {
            switch (accessRegion)
            {
                case AccessRegion.CommandAndControl:
                    wordRegisters.Write(offset, value);
                    break;
                case AccessRegion.CalibAndAux:
                    offset += BASE_ADDR_OFFSET - CALIB_AUX_BASE_ADDR;
                    this.DebugLog($"CalibAux [Word][Write] at [0x{offset:x}] - [0x{value:x}]");
                    break;
                case AccessRegion.RWWMemory:
                    offset += BASE_ADDR_OFFSET - RWW_SECTOR_BASE_ADDR;
                    rwwMemory.WriteWord(offset, value);
                    this.DebugLog($"RWW [Word][Write] at [0x{offset:x}] - [0x{value:x}]");
                    break;
                default:
                    break;
            }
        }
        public void WriteDoubleWord(long offset, uint value)
        {
            switch (accessRegion)
            {
                case AccessRegion.CommandAndControl:
                    doubleWordRegisters.Write(offset, value);
                    break;
                case AccessRegion.CalibAndAux:
                    offset += BASE_ADDR_OFFSET - CALIB_AUX_BASE_ADDR;
                    this.DebugLog($"CalibAux [DWord][Write] at [0x{offset:x}] - [0x{value:x}]");
                    break;
                case AccessRegion.RWWMemory:
                    rwwMemory.WriteDoubleWord(offset, value);
                    this.DebugLog($"RWW [DWord][Write] at [0x{offset:x}]:[0x{value:x}]");
                    break;
                default:
                    break;
            }

        }

        public Saml22NVMCTRL(Machine machine)
        {
            this.WarningLog("NVMCTRL not implemented at all. It's just a stub.");
            this.machine = machine;

            interruptsManager = new InterruptManager<Interrupts>(this);

            byteRegisters = new ByteRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            doubleWordRegisters = new DoubleWordRegisterCollection(this);

            rwwMemory = new ArrayMemory(0x2000);

            DefineRegisters();
        }

        private const long BASE_ADDR_OFFSET = 0x40000;
        private const long RWW_SECTOR_BASE_ADDR = 0x400000;
        private const long CALIB_AUX_BASE_ADDR = 0x800000;
        private const long APB_BRIDGE_PBASE_ADDR = 0x41000000;

        private readonly Machine machine;
        private readonly InterruptManager<Interrupts> interruptsManager;
        private readonly ByteRegisterCollection byteRegisters;
        private readonly WordRegisterCollection wordRegisters;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly ArrayMemory rwwMemory;
        private IValueRegisterField address;
        private bool isControlAccess;
        private AccessRegion accessRegion;

        private void DefineRegisters()
        {
            wordRegisters.DefineRegister((long)Registers.ControlA);
            doubleWordRegisters.DefineRegister((long)Registers.ControlB, 0x80);
            doubleWordRegisters.DefineRegister((long)Registers.NVMParameter); // Reset value depend on NVM User row

            byteRegisters.AddRegister((long)Registers.InterruptEnableClear, interruptsManager.GetInterruptEnableClearRegister<ByteRegister>());
            byteRegisters.AddRegister((long)Registers.InterruptEnableSet, interruptsManager.GetInterruptEnableSetRegister<ByteRegister>());
            byteRegisters.AddRegister((long)Registers.InterruptFlagStatusandClear, interruptsManager.GetRegister<ByteRegister>(writeCallback: (irq, oldValue, newValue) =>
            {
                if (newValue)
                    interruptsManager.ClearInterrupt(irq);
            }, valueProviderCallback: (irq, _) =>
            {
                if (irq == Interrupts.Ready)
                    return true;
                return interruptsManager.IsSet(irq);
            }));

            wordRegisters.DefineRegister((long)Registers.Status); // SB determined by value in NV Memory
                // .WithFlag(0, FieldMode.Read, name: "PRM")
                // .WithFlag(1, FieldMode.Read | FieldMode.WriteOneToClear, name: "LOAD")
                // .WithFlag(2, FieldMode.Read | FieldMode.WriteOneToClear, name: "PROGE")
                // .WithFlag(3, FieldMode.Read | FieldMode.WriteOneToClear, name: "LOCKE")
                // .WithFlag(4, FieldMode.Read | FieldMode.WriteOneToClear, name: "NVME")
                // .WithIgnoredBits(5, 3)
                // .WithFlag(8, FieldMode.Read, name: "SB")
                // .WithIgnoredBits(9, 7);

            doubleWordRegisters.DefineRegister((long)Registers.Address);
                // .WithValueField(0, 22, out address, name: "ADDR")
                // .WithIgnoredBits(22, 10);

            wordRegisters.DefineRegister((long)Registers.LockSection); // Reset value determined by NV memory user row
        }

        public void SetAbsoluteAddress(ulong address)
        {

            if ((address & APB_BRIDGE_PBASE_ADDR) == APB_BRIDGE_PBASE_ADDR)
            {
                accessRegion = AccessRegion.CommandAndControl;
                return;
            }
            if ((address & CALIB_AUX_BASE_ADDR) == CALIB_AUX_BASE_ADDR)
            {
                accessRegion = AccessRegion.CalibAndAux;
                return;
            }
            if ((address & RWW_SECTOR_BASE_ADDR) == RWW_SECTOR_BASE_ADDR)
            {
                accessRegion = AccessRegion.RWWMemory;
                return;
            }
        }

        private enum AccessRegion
        {
            CommandAndControl = 0,
            CalibAndAux = 1,
            RWWMemory = 2,
        }

        private enum Registers : long
        {
            ControlA = 0x0,
            ControlB = 0x04,
            NVMParameter = 0x08,
            InterruptEnableClear = 0x0C,
            InterruptEnableSet = 0x10,
            InterruptFlagStatusandClear = 0x14,
            Status = 0x18,
            Address = 0x1C,
            LockSection = 0x20
        }

        private enum Interrupts
        {
            Ready = 0,
            Error = 1
        }

    }
}
