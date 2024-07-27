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
            get => xosc.Frequency;
            set
            {
                if(xosc == null && value > 0)
                    xosc = new Crystal(this, value);
                if(xosc != null && value > 0)
                    xosc.Frequency = value;
            }
        }

        public void Reset()
        {
            doubleWordRegisters.Reset();
            byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => doubleWordRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => wordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);

        public Saml22OSCCTRL(Machine machine)
        {
            this.WarningLog("OSCCTRL is a stub. Does nothing.");
            this.machine = machine;

            interruptManager = new InterruptManager<Interrupts>(this);

            osc16m = new Crystal(this, 16_000_000);

            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);

            doubleWordRegisters.DefineRegister((long)Registers.STATUS, 0x111); // TODO: temporary solution
        }

        private readonly Machine machine;
        private readonly InterruptManager<Interrupts> interruptManager;
        private Crystal xosc;
        private readonly Crystal dfll48m;
        private readonly Crystal osc16m;
        private readonly Crystal dpll96m;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly WordRegisterCollection wordRegisters;
        private readonly ByteRegisterCollection byteRegisters;

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
                    if (enabled && actualFrequency > 0)
                        return actualFrequency;
                    return 0;
                }
                set => actualFrequency = value;
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

            public Crystal(Saml22OSCCTRL saml22oscctrl, long nominalFrequency, bool enabledByDefault = false)
            {
                this.saml22oscctrl = saml22oscctrl;
                this.nominalFrequency = nominalFrequency;
                this.enabledByDefault = enabledByDefault;
                enabled = enabledByDefault;
                startUp = new LimitTimer(this.saml22oscctrl.machine.ClockSource,
                    nominalFrequency, this.saml22oscctrl,
                    "Oscillator Startup", 32768,
                    workMode: Time.WorkMode.OneShot, eventEnabled: true, direction: Time.Direction.Ascending);
                startUp.LimitReached += StartUpTask;
            }

            private void StartUpTask()
            {
                ready = true;
            }

            private readonly Saml22OSCCTRL saml22oscctrl;
            private readonly LimitTimer startUp;
            private readonly long nominalFrequency;
            private long actualFrequency;
            private readonly bool enabledByDefault;
            private bool ready;
            private bool enabled;
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
