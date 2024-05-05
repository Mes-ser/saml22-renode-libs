using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22RTC : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x400;

        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

        public void Reset()
        {
            doubleWordRegisters.Reset();
            wordRegisters.Reset();
            byteRegisters.Reset();
            operatingMode.Value = OperatingMode.COUNT32;
            enabled.Value = false;
        }

        public uint ReadDoubleWord(long offset) => doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => doubleWordRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => wordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);

        public Saml22RTC(Machine machine)
        {
            this.WarningLog("RTC is a stub. Does nothing.");
            this.machine = machine;

            interruptsManager = new InterruptManager<Interrupts>(this);

            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);


            DefineRegisters();
        }

        private readonly Machine machine;
        private readonly InterruptManager<Interrupts> interruptsManager;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly WordRegisterCollection wordRegisters;
        private readonly ByteRegisterCollection byteRegisters;

        private IEnumRegisterField<OperatingMode> operatingMode;
        private IFlagRegisterField enabled;
        private IFlagRegisterField csSync;
        private IValueRegisterField prescaler;

        private void DefineRegisters()
        {

            wordRegisters.DefineRegister((long)Registers.ControlB)
                .WithTaggedFlag("GP0EN", 0)
                .WithIgnoredBits(1, 3)
                .WithTaggedFlag("DEBMAJ", 4)
                .WithTaggedFlag("DEBSYNC", 5)
                .WithTaggedFlag("RTCOUT", 6)
                .WithTaggedFlag("DMAEN", 7)
                .WithTag("DEBF", 8, 3)
                .WithIgnoredBits(11, 1)
                .WithTag("ACTF", 12, 3)
                .WithIgnoredBits(15, 1);

            byteRegisters.DefineRegister((long)Registers.DebugControl)
                .WithTaggedFlag("DBGRUN", 0);

            byteRegisters.DefineRegister((long)Registers.FrequencyCorrelation);

            doubleWordRegisters.DefineRegister((long)Registers.GeneralPurpose0)
                .WithTag("GP", 0, 32);
            doubleWordRegisters.DefineRegister((long)Registers.GeneralPurpose1)
                .WithTag("GPx", 0, 32);

            WordRegister CommonControlA = new WordRegister(this)
                .WithFlag(0, writeCallback: (oldValue, newValue) => {
                    if(newValue)
                        Reset();
                })
                .WithFlag(1, out enabled)
                .WithEnumField(2, 2, out operatingMode)
                .WithValueField(8, 4, out prescaler)
                .WithIgnoredBits(12, 1)
                .WithTaggedFlag("BKTRST", 13)
                .WithTaggedFlag("GPTRST", 14)
                .WithFlag(15, out csSync); // This bit is named in Count mode as 'COUNTSYNC' and in Clock as 'CLOCKSYNC'

            // DoubleWordRegister CommonSynchronizationBusy = new DoubleWordRegister(this);

            // COUNT32 - Default
            // WordRegister count32ControlA = CommonControlA;
            // count32ControlA.WithTaggedFlag("MATCHCLR", 7);
            // wordRegisters.AddConditionalRegister((long)Registers.ControlA, count32ControlA, () => operatingMode.Value == OperatingMode.COUNT32);

            // doubleWordRegisters.DefineConditionalRegister((long)Registers.EventControl, () => operatingMode.Value == OperatingMode.COUNT32);
            // // wordRegisters.DefineConditionalRegister((long)Registers.,() => operatingMode.Value == OperatingMode.);
            // wordRegisters.AddConditionalRegister((long)Registers.InterruptEnableClear,
            //     interruptsManager.GetInterruptEnableClearRegister<WordRegister>(),
            //     () => operatingMode.Value == OperatingMode.COUNT32);
            // wordRegisters.AddConditionalRegister((long)Registers.InterruptEnableSet,
            //     interruptsManager.GetInterruptEnableSetRegister<WordRegister>(),
            //     () => operatingMode.Value == OperatingMode.COUNT32);
            // wordRegisters.AddConditionalRegister((long)Registers.InterruptFlagStatusandClear,
            //     interruptsManager.GetRegister<WordRegister>(writeCallback: (irq, oldValue, newValue) =>
            //     {
            //         if (newValue)
            //             interruptsManager.ClearInterrupt(irq);
            //     }, valueProviderCallback: (irq, _) => interruptsManager.IsSet(irq)),
            //     () => operatingMode.Value == OperatingMode.COUNT32);

            // doubleWordRegisters.AddConditionalRegister((long)Registers.SynchronizationBusy,
            //     CommonSynchronizationBusy,
            //     () => operatingMode.Value == OperatingMode.COUNT32);

            // Clock
            WordRegister clockControlA = CommonControlA;
            clockControlA.WithTaggedFlag("MATCHCLR", 7);
            clockControlA.WithTaggedFlag("CLKREP", 6);
            wordRegisters.AddConditionalRegister((long)Registers.ControlA, clockControlA, () => operatingMode.Value == OperatingMode.CLOCK);

            doubleWordRegisters.DefineConditionalRegister((long)Registers.EventControl, () => operatingMode.Value == OperatingMode.CLOCK);

            doubleWordRegisters.DefineConditionalRegister((long)Registers.SynchronizationBusy, () => operatingMode.Value == OperatingMode.CLOCK);

            wordRegisters.AddConditionalRegister((long)Registers.InterruptEnableClear,
                interruptsManager.GetInterruptEnableClearRegister<WordRegister>(),
                () => operatingMode.Value == OperatingMode.CLOCK);
            wordRegisters.AddConditionalRegister((long)Registers.InterruptEnableSet,
                interruptsManager.GetInterruptEnableSetRegister<WordRegister>(),
                () => operatingMode.Value == OperatingMode.CLOCK);
            wordRegisters.AddConditionalRegister((long)Registers.InterruptFlagStatusandClear,
                interruptsManager.GetRegister<WordRegister>(writeCallback: (irq, oldValue, newValue) =>
                {
                    if (newValue)
                        interruptsManager.ClearInterrupt(irq);
                }, valueProviderCallback: (irq, _) => interruptsManager.IsSet(irq)),
                () => operatingMode.Value == OperatingMode.CLOCK);

            doubleWordRegisters.DefineConditionalRegister((long)Registers.Counter_ClockValue, () => operatingMode.Value == OperatingMode.CLOCK)
                .WithTag("SECOND", 0, 6)
                .WithTag("MINUTE", 6, 6)
                .WithTag("HOUR", 12, 5)
                .WithTag("DAY", 17, 5)
                .WithTag("MONTH", 22, 4)
                .WithTag("YEAR", 26, 6);

            doubleWordRegisters.DefineConditionalRegister((long)Registers.Compare_AlarmValue, () => operatingMode.Value == OperatingMode.CLOCK)
                .WithTag("SECOND", 0, 6)
                .WithTag("MINUTE", 6, 6)
                .WithTag("HOUR", 12, 5)
                .WithTag("DAY", 17, 5)
                .WithTag("MONTH", 22, 4)
                .WithTag("YEAR", 26, 6);

            byteRegisters.DefineConditionalRegister((long)Registers.AlarmMask, () => operatingMode.Value == OperatingMode.CLOCK)
                .WithTag("SEL", 0, 3);

        }

        private enum OperatingMode
        {
            COUNT32 = 0x0,
            COUNT16 = 0x1,
            CLOCK = 0x2
        }

        private enum Interrupts
        {
            PeriodicInterval0 = 0,
            PeriodicInterval1,
            PeriodicInterval2,
            PeriodicInterval3,
            PeriodicInterval4,
            PeriodicInterval5,
            PeriodicInterval6,
            PeriodicInterval7,
            Compare0_Alarm0,
            Compare1,
            Tamper = 14,
            Overflow,
        }

        private enum Registers : long
        {
            ControlA = 0x00,
            ControlB = 0x02,
            EventControl = 0x04,
            InterruptEnableClear = 0x08,
            InterruptEnableSet = 0x0A,
            InterruptFlagStatusandClear = 0x0C,
            DebugControl = 0x0E,
            SynchronizationBusy = 0x10,
            FrequencyCorrelation = 0x14,
            Counter_ClockValue = 0x18,
            CounterPeriod = 0x1C,
            Compare_AlarmValue = 0x20,
            Compare1 = 0x22,
            AlarmMask = 0x24,
            GeneralPurpose0 = 0x40,
            GeneralPurpose1 = 0x44,
            TamperControl = 0x60,
            Timestamp = 0x64,
            TamperID = 0x68,
            Backup = 0x80,

        }
    }
}
