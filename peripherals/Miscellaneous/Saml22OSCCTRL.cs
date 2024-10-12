using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.Timers;
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

            _interruptManager = new InterruptManager<Interrupts>(this);

            _osc16m = new Crystal(this, 4_000_000, true);
            _osc16m.OSCReady += OSCReadyChange;

            _doubleWordRegisters.DefineRegister((long)Registers.STATUS, 0x111); // TODO: temporary solution

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
        public long XOSCFrequency
        {
            get => _xosc.Frequency;
            set
            {
                if (_xosc == null && value > 0)
                    _xosc = new Crystal(this, value);
                if (_xosc != null && value > 0)
                    _xosc.Frequency = value;
            }
        }

        public long XOSC { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public long OSC16M => _osc16m.Frequency;

        public long DFLL48M => throw new NotImplementedException();

        public long FDPLL96M => throw new NotImplementedException();

        private readonly Machine _machine;
        private readonly ByteRegisterCollection _byteRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;

        private readonly InterruptManager<Interrupts> _interruptManager;
        private Crystal _xosc;
        private readonly Crystal _dfll48m;
        private readonly Crystal _osc16m;
        private readonly Crystal _dpll96m;

        public event Action<SAML22OSCClock> OSCClockChanged;

        [Flags]
        private enum Interrupts
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

            public Crystal(Saml22OSCCTRL saml22oscctrl, long startFrequency, bool enabledByDefault = false)
            {
                _saml22oscctrl = saml22oscctrl;
                _frequency = startFrequency;
                _enabledByDefault = enabledByDefault;
                _enabled = enabledByDefault;
                _startUp = new LimitTimer(_saml22oscctrl._machine.ClockSource,
                    startFrequency, _saml22oscctrl,
                    "Oscillator Startup", 32768,
                    workMode: WorkMode.OneShot, eventEnabled: true, direction: Direction.Ascending);
                if (!_enabledByDefault)
                    _startUp.LimitReached += StartUpTask;

            }

            public void Reset()
            {
                _enabled = _enabledByDefault;
                if (_enabledByDefault)
                    OSCReady?.Invoke(SAML22OSCClock.OSC16M, true);
            }

            private void StartUpTask()
            {
                Ready = true;
                OSCReady?.Invoke(SAML22OSCClock.OSC16M, true);
            }

            public bool Enabled
            {
                get => _enabled;
                set
                {
                    if (!_enabled && value)
                    {
                        _startUp.Enabled = true;
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
                    _frequency = value;
                    OSCReady?.Invoke(SAML22OSCClock.OSC16M, true);
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

            private readonly Saml22OSCCTRL _saml22oscctrl;
            private readonly LimitTimer _startUp;
            private readonly bool _enabledByDefault;
            private long _frequency;
            private bool _enabled;

            public event Action<SAML22OSCClock, bool> OSCReady;
        }
    }
}
