

using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.MemoryControllers
{
    public class Saml22NVMCTRL : IBytePeripheral, IWordPeripheral, IDoubleWordPeripheral, IKnownSize
    {
        public long Size => 0x400;

        [IrqProvider]
        public GPIO IRQ { get; }

        public void Reset()
        {
            byteRegisters.Reset();
            wordRegisters.Reset();
            doubleWordRegisters.Reset();
        }
        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public ushort ReadWord(long offset) => wordRegisters.Read(offset);
        public uint ReadDoubleWord(long offset) => doubleWordRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);
        public void WriteWord(long offset, ushort value) => wordRegisters.Write(offset, value);
        public void WriteDoubleWord(long offset, uint value) => doubleWordRegisters.Write(offset, value);
        
        public Saml22NVMCTRL(Machine machine)
        {
            this.WarningLog("NVMCTRL not implemented at all. It's just a stub.");
            this.machine = machine;

            IRQ = new GPIO();
            IRQManager = new InterruptManager<Interrupts>(this);

            byteRegisters = new ByteRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            doubleWordRegisters = new DoubleWordRegisterCollection(this);

            DefineRegisters();
        }

        private readonly Machine machine;
        private readonly InterruptManager<Interrupts> IRQManager; 
        private ByteRegisterCollection byteRegisters;
        private WordRegisterCollection wordRegisters;
        private DoubleWordRegisterCollection doubleWordRegisters;

        private void DefineRegisters()
        {
            wordRegisters.DefineRegister((long)Registers.ControlA);
            doubleWordRegisters.DefineRegister((long)Registers.ControlB, 0x80);
            doubleWordRegisters.DefineRegister((long)Registers.NVMParameter); // Reset value depend on NVM User row

            byteRegisters.AddRegister((long)Registers.InterruptEnableClear, IRQManager.GetInterruptEnableClearRegister<ByteRegister>());
            byteRegisters.AddRegister((long)Registers.InterruptEnableSet, IRQManager.GetInterruptEnableSetRegister<ByteRegister>());
            byteRegisters.AddRegister((long)Registers.InterruptFlagStatusandClear, IRQManager.GetRegister<ByteRegister>(writeCallback: (irq, oldValue, newValue) => {
                if(newValue)
                    IRQManager.ClearInterrupt(irq);
            }, valueProviderCallback: (irq, _) => {
                return IRQManager.IsSet(irq);
            }));

            wordRegisters.DefineRegister((long)Registers.Status); // SB determined by value in NV Memory
            doubleWordRegisters.DefineRegister((long)Registers.Address);
            wordRegisters.DefineRegister((long)Registers.LockSection); // Reset value determined by NV memory user row
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