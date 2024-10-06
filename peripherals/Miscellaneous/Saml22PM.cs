


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
            _byteRegisters.Reset();
            _wordRegisters.Reset();
        }

        public Saml22PM(Machine machine)
        {
            this.WarningLog("PM not implemented at all. It's just a stub.");

            _machine = machine;

            _intManager = new InterruptManager<Interrupts>(this);

            _byteRegisters = new ByteRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);

            DefineRegisters();

        }

        private readonly Machine _machine;
        private readonly InterruptManager<Interrupts> _intManager;
        private readonly ByteRegisterCollection _byteRegisters;
        private readonly WordRegisterCollection _wordRegisters;

        // Registers Fields
        private IEnumRegisterField<SleepMode> _sleepMode;

        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);
        public void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);

        private void DefineRegisters()
        {
            _byteRegisters.DefineRegister((long)Registers.ControlA);
            _byteRegisters.DefineRegister((long)Registers.SleepConfiguration, 0x02)
                .WithEnumField(0, 3, out _sleepMode)
                .WithIgnoredBits(3, 5);

            _byteRegisters.DefineRegister((long)Registers.PerformanceLevelConfiguration)
                .WithValueField(0, 2, writeCallback: (_, value) =>
                {
                    _intManager.SetInterrupt(Interrupts.PerformanceLevelInterruptEnable);
                });

            _byteRegisters.AddRegister((long)Registers.InterruptEnableClear, _intManager.GetInterruptEnableClearRegister<ByteRegister>());
            _byteRegisters.AddRegister((long)Registers.InterruptEnableSet, _intManager.GetInterruptEnableSetRegister<ByteRegister>());
            _byteRegisters.AddRegister((long)Registers.InterruptFlagStatusandClear, _intManager.GetRegister<ByteRegister>(writeCallback: (irq, oldValue, newValue) =>
            {
                if (newValue)
                    _intManager.ClearInterrupt(irq);
            }, valueProviderCallback: (irq, _) =>
            {
                return _intManager.IsSet(irq);
            }).WithIgnoredBits(1, 7));

            _wordRegisters.DefineRegister((long)Registers.StandbyConfiguration, 0x0400);
        }

        private enum SleepMode
        {
            IDLE = 0x2,
            STANDBY = 0x4,
            BACKUP = 0x5,
            OFF = 0x6
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
