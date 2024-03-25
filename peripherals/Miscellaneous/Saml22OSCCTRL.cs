using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22OSCCTRL : IDoubleWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x400;

        public void Reset()
        {
            doubleWordRegisters.Reset();
            byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => doubleWordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);

        public Saml22OSCCTRL(Machine machine)
        {
            this.WarningLog("OSCCTRL is a stub. Does nothing.");
            this.machine = machine;

            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);

            doubleWordRegisters.DefineRegister((long)Registers.Status, 0x111); // TODO: temporary solution
        }

        private readonly Machine machine;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly ByteRegisterCollection byteRegisters;


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
