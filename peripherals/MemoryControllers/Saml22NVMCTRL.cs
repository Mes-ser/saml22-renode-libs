

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
            if(accessRegion == AccessRegion.Control)
                return byteRegisters.Read(offset);

            // Read from memory selected in "SetAbsoluteAddress()"
            return 0x0;
        }
        public ushort ReadWord(long offset)
        {
            if(accessRegion == AccessRegion.Control)
                return wordRegisters.Read(offset);

            // Read from memory selected in "SetAbsoluteAddress()"
            return 0x0;
        }
        public uint ReadDoubleWord(long offset)
        {

            if(accessRegion == AccessRegion.Control)
                return doubleWordRegisters.Read(offset);

            // Read from memory selected in "SetAbsoluteAddress()"
            return 0x0;
        }
        public void WriteByte(long offset, byte value)
        {
            if(accessRegion == AccessRegion.Control){
                wordRegisters.Write(offset, value);
                return;
            }
            // Byte write to NVM is prohibited
            interruptsManager.SetInterrupt(Interrupts.Error);
        }
        public void WriteWord(long offset, ushort value)
        {
            if (accessRegion == AccessRegion.Control)
            {
                wordRegisters.Write(offset, value);
                return;
            }
            // Write to pageBuffer
        }
        public void WriteDoubleWord(long offset, uint value)
        {
            if (accessRegion == AccessRegion.Control)
            {
                doubleWordRegisters.Write(offset, value);
                return;
            }
            // Write to pageBuffer
        }

        public Saml22NVMCTRL(Machine machine)
        {
            this.WarningLog("NVMCTRL not implemented at all. It's just a stub.");
            this.machine = machine;

            interruptsManager = new InterruptManager<Interrupts>(this);

            byteRegisters = new ByteRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            doubleWordRegisters = new DoubleWordRegisterCollection(this);

            rwweeMemory = new ArrayMemory(0x2000);
            auxiliaryMemory = new ArrayMemory(0xA100);

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
        private readonly ArrayMemory rwweeMemory;
        private readonly ArrayMemory auxiliaryMemory;
        private IValueRegisterField ADDR;
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

            doubleWordRegisters.DefineRegister((long)Registers.Address)
                .WithValueField(0, 16, out ADDR, name: "ADDR")
                .WithIgnoredBits(22, 10);

            wordRegisters.DefineRegister((long)Registers.LockSection); // Reset value determined by NV memory user row
        }

        public void SetAbsoluteAddress(ulong address)
        {
            accessRegion = AccessRegion.Memory;
            if ((address & APB_BRIDGE_PBASE_ADDR) == APB_BRIDGE_PBASE_ADDR)
                accessRegion = AccessRegion.Control;

            ADDR.Value = (address & 0x1FFFF) >> 1;
        }

        private enum AccessRegion
        {
            Memory = 0,
            Control = 1
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
