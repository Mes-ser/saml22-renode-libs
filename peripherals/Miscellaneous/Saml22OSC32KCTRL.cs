using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.Timers;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22OSC32KCTRL : IBytePeripheral, IWordPeripheral, IDoubleWordPeripheral, IKnownSize
    {
        public long Size => 0x400;

        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();
        public bool UseXOSC32K
        {
            get => xosc32kConnected;
            set => xosc32kConnected = value;
        }

        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => wordRegisters.Write(offset, value);
        public uint ReadDoubleWord(long offset) => doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => doubleWordRegisters.Write(offset, value);

        public void Reset()
        {
            byteRegisters.Reset();
            wordRegisters.Reset();
            doubleWordRegisters.Reset();
            OSCULP32K.Reset();
            XOSC32K.Reset();
        }

        public Saml22OSC32KCTRL(Machine machine)
        {
            this.machine = machine;
            IRQManager = new InterruptManager<Interrupts>(this);

            OSCULP32K = new Crystal(this, 32768, true);
            XOSC32K = new Crystal(this, 32768);

            byteRegisters = new ByteRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            doubleWordRegisters = new DoubleWordRegisterCollection(this);

            DefineRegisters();
        }

        private readonly Machine machine;
        private readonly InterruptManager<Interrupts> IRQManager;

        private readonly ByteRegisterCollection byteRegisters;
        private readonly WordRegisterCollection wordRegisters;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;

        private readonly Crystal OSCULP32K;
        private readonly Crystal XOSC32K;
        private bool xosc32kConnected = false;
        private IHasFrequency rtc => (IHasFrequency)machine.SystemBus.WhatPeripheralIsAt((ulong)Saml22MemoryMap.RTCBaseAddress);
        private IFlagRegisterField xosc32kEnable;
        private IFlagRegisterField enable32Koutput;
        private IFlagRegisterField enable1KOutput;

        private void DefineRegisters()
        {
            doubleWordRegisters.AddRegister((long)Registers.InterruptEnableClear, IRQManager.GetInterruptEnableClearRegister<DoubleWordRegister>());
            doubleWordRegisters.AddRegister((long)Registers.InterruptEnableSet, IRQManager.GetInterruptEnableSetRegister<DoubleWordRegister>());
            doubleWordRegisters.AddRegister((long)Registers.InterruptFlagStatusandClear, IRQManager.GetRegister<DoubleWordRegister>(
                writeCallback: (irq, oldValue, newValue) =>
                {
                    if (newValue) IRQManager.ClearInterrupt(irq);
                }, valueProviderCallback: (irq, _) => IRQManager.IsSet(irq)));

            doubleWordRegisters.DefineRegister((long)Registers.Status)
                .WithFlag(0, name: "XOSC32RDY", valueProviderCallback: (_) => XOSC32K.Ready);

            byteRegisters.DefineRegister((long)Registers.RTCClockSelectionControl)
                .WithValueField(0, 3, writeCallback:(_, value) => {
                    switch(value)
                    {
                        case 0x0:
                            // rtc.Frequency = ULP1K
                            break;
                        case 0x01:
                            // rtc.Frequency = ULP32K;
                            break;
                        case 0x04:
                            // rtc.Frequency = XOSC1K
                            break;
                        case 0x05:
                            // rtc.Frequency = XOSC32K
                            break;
                        default:
                            break;
                    }
                });
            byteRegisters.DefineRegister((long)Registers.SLCDClockSelectionControl);
            wordRegisters.DefineRegister((long)Registers.XOSC32KControl, 0x80)
                .WithIgnoredBits(0, 1)
                .WithFlag(1, writeCallback: (_, value) => XOSC32K.Enabled = value && xosc32kConnected, valueProviderCallback: (_) => XOSC32K.Enabled)
                .WithTaggedFlag("XTALEN", 2)
                .WithFlag(3, out enable32Koutput)
                .WithFlag(4, out enable1KOutput)
                .WithIgnoredBits(5, 1)
                .WithFlag(6, name: "RUNSTDBY")
                .WithFlag(7, name: "ONDEMAND")
                .WithValueField(8, 3, name: "STARTUP")
                .WithIgnoredBits(11, 1)
                .WithFlag(12, name: "WRTLOCK")
                .WithIgnoredBits(13, 3);

            byteRegisters.DefineRegister((long)Registers.ClockFailureDetectorControl);
            byteRegisters.DefineRegister((long)Registers.EventControl);
            doubleWordRegisters.DefineRegister((long)Registers.ULPInt32kControl); // Read from NVM calib

        }

        private sealed class Crystal
        {

            public bool Enabled
            {
                get => enabled;
                set
                {
                    if (!enabled && value)
                    {
                        startUp.Enabled = true;
                    }
                }
            }
            public long Frequency
            {
                get
                {
                    if (enabled && nominalFrequency > 0)
                        return nominalFrequency;
                    return 0;
                }
            }

            public bool Ready => ready;

            public ulong StartUpTime
            {
                set
                {
                    startUp.Limit = value;
                }
            }

            public void Reset()
            {
                enabled = enabledByDefault;
                ready = false;
            }

            public Crystal(Saml22OSC32KCTRL osc32kctrl, long nominalFrequency, bool enabledByDefault = false)
            {
                this.osc32kctrl = osc32kctrl;
                this.nominalFrequency = nominalFrequency;
                this.enabledByDefault = enabledByDefault;
                enabled = enabledByDefault;
                startUp = new LimitTimer(this.osc32kctrl.machine.ClockSource,
                    nominalFrequency, this.osc32kctrl,
                    "Oscillator Startup", 32768,
                    workMode: Time.WorkMode.OneShot, eventEnabled: true, direction:Time.Direction.Ascending);
                startUp.LimitReached += StartUpTask;
            }

            private void StartUpTask()
            {
                ready = true;
            }

            private readonly Saml22OSC32KCTRL osc32kctrl;
            private readonly LimitTimer startUp;
            private readonly long nominalFrequency;
            private readonly bool enabledByDefault;
            private bool ready;
            private bool enabled;
        }

        private enum Registers : long
        {
            InterruptEnableClear = 0x0,
            InterruptEnableSet = 0x04,
            InterruptFlagStatusandClear = 0x08,
            Status = 0x0C,
            RTCClockSelectionControl = 0x10,
            SLCDClockSelectionControl = 0x11,
            XOSC32KControl = 0x14,
            ClockFailureDetectorControl = 0x16,
            EventControl = 0x17,
            ULPInt32kControl = 0x1C,

        }

        private enum Interrupts
        {
            XOSC32Ready = 0,
            ClockFail = 2
        }
    }
}
