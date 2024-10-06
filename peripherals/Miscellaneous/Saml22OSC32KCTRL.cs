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
        public bool UseXOSC32K { get; set; } = false;

        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);
        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);

        public void Reset()
        {
            _byteRegisters.Reset();
            _wordRegisters.Reset();
            _doubleWordRegisters.Reset();
            _osculp32k.Reset();
            _xosc32k.Reset();
        }

        public Saml22OSC32KCTRL(Machine machine)
        {
            _machine = machine;
            _interruptsManager = new InterruptManager<Interrupts>(this);

            _osculp32k = new Crystal(this, 32768, true);
            _xosc32k = new Crystal(this, 32768);

            _byteRegisters = new ByteRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _doubleWordRegisters = new DoubleWordRegisterCollection(this);

            DefineRegisters();
        }

        private readonly Machine _machine;
        private readonly InterruptManager<Interrupts> _interruptsManager;

        private readonly ByteRegisterCollection _byteRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;

        private readonly Crystal _osculp32k;
        private readonly Crystal _xosc32k;

        private IHasFrequency RTC => (IHasFrequency)_machine.SystemBus.WhatPeripheralIsAt((ulong)Saml22MemoryMap.RTCBaseAddress);
        private IFlagRegisterField _xosc32kEnable;
        private IFlagRegisterField _enable32Koutput;
        private IFlagRegisterField _enable1KOutput;

        private void DefineRegisters()
        {
            _doubleWordRegisters.AddRegister((long)Registers.InterruptEnableClear, _interruptsManager.GetInterruptEnableClearRegister<DoubleWordRegister>());
            _doubleWordRegisters.AddRegister((long)Registers.InterruptEnableSet, _interruptsManager.GetInterruptEnableSetRegister<DoubleWordRegister>());
            _doubleWordRegisters.AddRegister((long)Registers.InterruptFlagStatusandClear, _interruptsManager.GetRegister<DoubleWordRegister>(
                writeCallback: (irq, oldValue, newValue) =>
                {
                    if (newValue) _interruptsManager.ClearInterrupt(irq);
                }, valueProviderCallback: (irq, _) => _interruptsManager.IsSet(irq)));

            _doubleWordRegisters.DefineRegister((long)Registers.Status)
                .WithFlag(0, name: "XOSC32RDY", valueProviderCallback: (_) => _xosc32k.Ready);

            _byteRegisters.DefineRegister((long)Registers.RTCClockSelectionControl)
                .WithValueField(0, 3, writeCallback: (_, value) =>
                {
                    switch (value)
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
            _byteRegisters.DefineRegister((long)Registers.SLCDClockSelectionControl);
            _wordRegisters.DefineRegister((long)Registers.XOSC32KControl, 0x80)
                .WithIgnoredBits(0, 1)
                .WithFlag(1, writeCallback: (_, value) => _xosc32k.Enabled = value && UseXOSC32K, valueProviderCallback: (_) => _xosc32k.Enabled)
                .WithTaggedFlag("XTALEN", 2)
                .WithFlag(3, out _enable32Koutput)
                .WithFlag(4, out _enable1KOutput)
                .WithIgnoredBits(5, 1)
                .WithFlag(6, name: "RUNSTDBY")
                .WithFlag(7, name: "ONDEMAND")
                .WithValueField(8, 3, name: "STARTUP")
                .WithIgnoredBits(11, 1)
                .WithFlag(12, name: "WRTLOCK")
                .WithIgnoredBits(13, 3);

            _byteRegisters.DefineRegister((long)Registers.ClockFailureDetectorControl);
            _byteRegisters.DefineRegister((long)Registers.EventControl);
            _doubleWordRegisters.DefineRegister((long)Registers.ULPInt32kControl); // Read from NVM calib

        }

        private sealed class Crystal
        {

            public bool Enabled
            {
                get => _enabled;
                set
                {
                    if (!_enabled && value)
                    {
                        _startUp.Enabled = true;
                    }
                }
            }
            public long Frequency
            {
                get
                {
                    if (_enabled && _nominalFrequency > 0)
                        return _nominalFrequency;
                    return 0;
                }
            }

            public bool Ready { get; private set; }

            public ulong StartUpTime
            {
                set
                {
                    _startUp.Limit = value;
                }
            }

            public void Reset()
            {
                _enabled = _enabledByDefault;
                Ready = false;
            }

            public Crystal(Saml22OSC32KCTRL osc32kctrl, long nominalFrequency, bool enabledByDefault = false)
            {
                _osc32kctrl = osc32kctrl;
                _nominalFrequency = nominalFrequency;
                _enabledByDefault = enabledByDefault;
                _enabled = enabledByDefault;
                _startUp = new LimitTimer(_osc32kctrl._machine.ClockSource,
                    nominalFrequency, _osc32kctrl,
                    "Oscillator Startup", 32768,
                    workMode: Time.WorkMode.OneShot, eventEnabled: true, direction: Time.Direction.Ascending);
                _startUp.LimitReached += StartUpTask;
            }

            private void StartUpTask()
            {
                Ready = true;
            }

            private readonly Saml22OSC32KCTRL _osc32kctrl;
            private readonly LimitTimer _startUp;
            private readonly long _nominalFrequency;
            private readonly bool _enabledByDefault;
            private bool _enabled;
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
