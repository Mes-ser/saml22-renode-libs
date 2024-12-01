using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Time;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22OSCCTRL : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize, ISAML22OSCCTRL
    {

        public Saml22OSCCTRL(Machine machine)
        {
            _machine = machine;

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);

            _interruptManager = new InterruptManager<Interrupt>(this);

            _osc16m = new Crystal(this, 4_000_000, SAML22OSCClock.OSC16M, true);
            _osc16m.OSCReadyState += OSCReadyChange;

            _xosc = new Crystal(this, 0, SAML22OSCClock.XOSC);
            _xosc.OSCReadyState += OSCReadyChange;

            _doubleWordRegisters.DefineRegister((long)Registers.STATUS, 0x100)
                .WithFlag(0, FieldMode.Read, valueProviderCallback: (_) => _xosc.Ready)
                .WithFlag(4, FieldMode.Read, valueProviderCallback: (_) => _osc16m.Ready);

            _wordRegisters.DefineRegister((long)Registers.XOSCCTRL, 0x80)
                .WithFlag(1, writeCallback: (oldValue, newValue) => _xosc.Enabled = newValue,
                    valueProviderCallback: (_) => _xosc.Enabled)
                .WithValueField(12, 4, writeCallback: (_, value) => _xosc.StartUpCycles = (ulong)Math.Pow(2, value),
                valueProviderCallback: (_) => (ulong)Math.Sqrt(_xosc.StartUpCycles));

            _byteRegisters.DefineRegister((long)Registers.OSC16MCTRL, 0x81)
                .WithFlag(1, writeCallback: (oldValue, newValue) => _osc16m.Enabled = newValue,
                    valueProviderCallback: (_) => _osc16m.Enabled)
                .WithValueField(2, 2, writeCallback: (oldValue, newValue) =>
                {
                    switch (newValue)
                    {
                        case 0:
                            if (_osc16m.Frequency != 4_000_000)
                                _osc16m.Frequency = 4_000_000;
                            break;
                        case 1:
                            if (_osc16m.Frequency != 8_000_000)
                                _osc16m.Frequency = 8_000_000;
                            break;
                        case 2:
                            if (_osc16m.Frequency != 12_000_000)
                                _osc16m.Frequency = 12_000_000;
                            break;
                        case 3:
                            if (_osc16m.Frequency != 16_000_000)
                                _osc16m.Frequency = 16_000_000;
                            break;
                    }
                });
        }

        // This allow to simulate the clock propagation at the power up stage.
        public void StartOscillators()
        {
            _osc16m.Frequency = 4_000_000;
        }

        // Treat this as Power Reset
        public void Reset()
        {
            _doubleWordRegisters.Reset();
            _wordRegisters.Reset();
            _byteRegisters.Reset();
            StartOscillators();
        }

        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);

        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);
        public void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);

        private void OSCReadyChange(SAML22OSCClock clock, bool value)
        {
            switch (clock)
            {
                case SAML22OSCClock.OSC16M:
                    if (value)
                        OSCClockChanged?.Invoke(clock);
                    break;
            }
        }

        public long Size => 0x400;
        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

        public long XOSC
        {
            get
            {
                if (_xosc != null)
                    return _xosc.Frequency;
                return 0;
            }
            set
            {
                if (_xosc != null && value > 0)
                    _xosc.Frequency = value;
            }
        }

        public long OSC16M => _osc16m.Frequency;

        public long DFLL48M => throw new NotImplementedException();

        public long FDPLL96M => throw new NotImplementedException();

        private readonly Machine _machine;
        private readonly ByteRegisterCollection _byteRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;

        private readonly InterruptManager<Interrupt> _interruptManager;
        private Crystal _xosc;
        private readonly Crystal _dfll48m;
        private readonly Crystal _osc16m;
        private readonly Crystal _dpll96m;

        public event Action<SAML22OSCClock> OSCClockChanged;

        [Flags]
        private enum Interrupt
        {
            XOSCRDY = 0,
            XOSCFAIL = 1,
            OSC16MRDY = 4,
            DFLLRDY = 8,
            DFLLOOB = 9,
            DFLLLCKF = 10,
            DFLLLCKC = 11,
            DFLLRCS = 12,
            DPLLLCKR = 16,
            DPLLLCKF = 17,
            DPLLLTO = 18,
            DPLLLDRTO = 19
        }

        private enum Registers : long
        {
            INTENCLR = 0x00,
            INTENSET = 0x04,
            INTFLAG = 0x08,
            STATUS = 0x0C,
            XOSCCTRL = 0x10,
            CFDPRESC = 0x12,
            EVCTRL = 0x13,
            OSC16MCTRL = 0x14,
            DFLLCRL = 0x18,
            DFLLVAL = 0x1C,
            DFLLMUL = 0x20,
            DFLLSYNC = 0x24,
            DPLLCTRLA = 0x28,
            DPLLRATIO = 0x2C,
            DPLLCTRLB = 0x30,
            DPLLPRESC = 0x34,
            DPLLSYNCBUSY = 0x38,
            DPLLSTATUS = 0x3C
        }

        // Trzeba to uprościć, by był to zupełny basic potrzebny do funkcjonowania
        // Np. tylko parametry wspólne oscylatorów itd.
        // startupy i inne robić już w samej klasie SAML22OSCCTRL

        private sealed class Crystal
        {

            public Crystal(Saml22OSCCTRL oscctrl, long startFrequency, SAML22OSCClock osc, bool enabledByDefault = false)
            {
                _oscctrl = oscctrl;
                _osc = osc;
                _frequency = startFrequency;
                _enabledByDefault = enabledByDefault;
                _enabled = enabledByDefault;
            }

            public void Reset()
            {
                _enabled = _enabledByDefault;
                if (_enabledByDefault)
                    Ready = true;
            }

            private void StartUpTask()
            {
                Ready = true;
            }

            public bool Enabled
            {
                get => _enabled;
                set
                {
                    if (!_enabled && value)
                    {
                        _oscctrl._machine.ScheduleAction(TimeInterval.FromMicroseconds((1000000 / 32786) * _startUpCycles),
                        (_) => StartUpTask());
                    }
                    _enabled = value;
                }
            }

            public long Frequency
            {
                get
                {
                    if (_enabled && _frequency > 0)
                        return _frequency;
                    return 0;
                }
                set
                {
                    Ready = false;
                    if (_frequency == 0 && value > 0)
                    {
                        StartUpTask();
                    }
                    else
                    {
                        Ready = true;
                    }
                    _frequency = value;
                }
            }

            public bool Ready
            {
                get => _ready;
                set
                {
                    if (_ready != value)
                    {
                        OSCReadyState?.Invoke(_osc, value);
                    }
                    _ready = value;
                }
            }
            private bool _ready;

            public ulong StartUpCycles
            {
                get => _startUpCycles;
                set
                {
                    _startUpCycles = value;
                }
            }

            private readonly Saml22OSCCTRL _oscctrl;
            private readonly SAML22OSCClock _osc;
            private ulong _startUpCycles;
            private readonly bool _enabledByDefault;
            private long _frequency;
            private bool _enabled;

            public event Action<SAML22OSCClock, bool> OSCReadyState;
        }
    }
}
