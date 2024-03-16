


using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22PM : IBytePeripheral, IWordPeripheral, IKnownSize
    { 
        public long Size => 0x400;

        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

        public void Reset()
        {
            byteRegisters.Reset();
            wordRegisters.Reset();
        }

        public Saml22PM(Machine machine)
        {
            this.WarningLog("PM not implemented at all. It's just a stub.");

            this.machine = machine;

            IRQManager = new InterruptManager<Interrupts>(this);

            byteRegisters = new ByteRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);

            DefineRegisters();
            
        }

        private readonly Machine machine;
        private readonly InterruptManager<Interrupts> IRQManager;
        private ByteRegisterCollection byteRegisters;
        private WordRegisterCollection wordRegisters;

        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public ushort ReadWord(long offset) => wordRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);
        public void WriteWord(long offset, ushort value) => wordRegisters.Write(offset, value);

        private void DefineRegisters()
        {
            byteRegisters.DefineRegister((long)Registers.ControlA);
            byteRegisters.DefineRegister((long)Registers.SleepConfiguration, 0x02);
            byteRegisters.DefineRegister((long)Registers.PerformanceLevelConfiguration)
                .WithValueField(0, 2, writeCallback: (_, value) => {
                    IRQManager.SetInterrupt(Interrupts.PerformanceLevelInterruptEnable);
                });

            byteRegisters.AddRegister((long)Registers.InterruptEnableClear, IRQManager.GetInterruptEnableClearRegister<ByteRegister>());
            byteRegisters.AddRegister((long)Registers.InterruptEnableSet, IRQManager.GetInterruptEnableSetRegister<ByteRegister>());
            byteRegisters.AddRegister((long)Registers.InterruptFlagStatusandClear, IRQManager.GetRegister<ByteRegister>(writeCallback: (irq, oldValue, newValue) => {
                    if(newValue)
                        IRQManager.ClearInterrupt(irq);
                }, valueProviderCallback: (irq, _) => {
                    return IRQManager.IsSet(irq);
                }).WithIgnoredBits(1, 7));

            wordRegisters.DefineRegister((long)Registers.StandbyConfiguration, 0x0400);
        }

        private enum Registers : long
        {
            ControlA = 0x00,
            SleepConfiguration = 0x01,
            PerformanceLevelConfiguration = 0x02,
            InterruptEnableClear = 0x04,
            InterruptEnableSet = 0x05,
            InterruptFlagStatusandClear = 0x06,
            StandbyConfiguration = 0x08
        }

        private enum Interrupts
        {
            PerformanceLevelInterruptEnable = 0
        }

    }
}