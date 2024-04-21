using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.Timers;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22OSCCTRL : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x400;

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

            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);




            doubleWordRegisters.DefineRegister((long)Registers.Status, 0x111); // TODO: temporary solution
        }

        private readonly Machine machine;
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

            public Crystal(Saml22OSCCTRL saml22oscctrl, long nominalFrequency, bool enabledByDefault = false)
            {
                this.saml22oscctrl = saml22oscctrl;
                this.nominalFrequency = nominalFrequency;
                this.enabledByDefault = enabledByDefault;
                enabled = enabledByDefault;
                startUp = new LimitTimer(this.saml22oscctrl.machine.ClockSource,
                    nominalFrequency, this.saml22oscctrl,
                    "Oscillator Startup", 32768,
                    workMode: Time.WorkMode.OneShot, eventEnabled: true, direction:Time.Direction.Ascending);
                startUp.LimitReached += StartUpTask;
            }

            private void StartUpTask()
            {
                ready = true;
            }

            private readonly Saml22OSCCTRL saml22oscctrl;
            private readonly LimitTimer startUp;
            private readonly long nominalFrequency;
            private readonly bool enabledByDefault;
            private bool ready;
            private bool enabled;
        }

        private enum Registers : long
        {
            InterruptEnableClear = 0x00,
            InterruptEnableSet = 0x04,
            InterruptFlagStatusandClear = 0x08,
            Status = 0x0C,
            ClockFailureDetectorPrescaler = 0x12,
            EventControl = 0x13,
            InternalOscillatorOSC16MControl = 0x14,
            // ExternalMultipurposeCrystalOscillatorXOSCControl =
            DFLL48MControl = 0x18,
            DFLL48Value = 0x1C,
            DFLL48MMultiplier = 0x20,
            DFLL48MSynchronization = 0x24,
            DPLLControlA = 0x28,
            DPLLRatioControl = 0x2C,
            DPLLControlB = 0x30,
            DPLLPrescaler = 0x34,
            DPLLSynchronizationBusy = 0x38,
            DPLLStatus = 0x3C
        }
    }
}
