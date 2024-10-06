using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.Timers;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22OSCCTRL : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize
    {
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

        public void Reset()
        {
            _doubleWordRegisters.Reset();
            _byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);

        public Saml22OSCCTRL(Machine machine)
        {
            this.WarningLog("OSCCTRL is a stub. Does nothing.");
            _machine = machine;

            _interruptManager = new InterruptManager<Interrupts>(this);

            _osc16m = new Crystal(this, 16_000_000);

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);

            _doubleWordRegisters.DefineRegister((long)Registers.STATUS, 0x111); // TODO: temporary solution
        }

        private readonly Machine _machine;
        private readonly InterruptManager<Interrupts> _interruptManager;
        private Crystal _xosc;
        private readonly Crystal _dfll48m;
        private readonly Crystal _osc16m;
        private readonly Crystal _dpll96m;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;

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
                    if (_enabled && _actualFrequency > 0)
                        return _actualFrequency;
                    return 0;
                }
                set => _actualFrequency = value;
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

            public Crystal(Saml22OSCCTRL saml22oscctrl, long nominalFrequency, bool enabledByDefault = false)
            {
                _saml22oscctrl = saml22oscctrl;
                _nominalFrequency = nominalFrequency;
                _enabledByDefault = enabledByDefault;
                _enabled = enabledByDefault;
                _startUp = new LimitTimer(_saml22oscctrl._machine.ClockSource,
                    nominalFrequency, _saml22oscctrl,
                    "Oscillator Startup", 32768,
                    workMode: Time.WorkMode.OneShot, eventEnabled: true, direction: Time.Direction.Ascending);
                _startUp.LimitReached += StartUpTask;
            }

            private void StartUpTask()
            {
                Ready = true;
            }

            private readonly Saml22OSCCTRL _saml22oscctrl;
            private readonly LimitTimer _startUp;
            private readonly long _nominalFrequency;
            private long _actualFrequency;
            private readonly bool _enabledByDefault;
            private bool _enabled;
        }

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
    }
}
