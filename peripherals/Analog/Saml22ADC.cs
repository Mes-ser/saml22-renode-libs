using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.Miscellaneous;

namespace Antmicro.Renode.Peripherals.Analog
{
    public class Saml22ADC : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x800;

        public void Reset()
        {
            _doubleWordRegisters.Reset();
            _wordRegisters.Reset();
            _byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);

        public Saml22ADC(Machine machine, ISAML22GCLK gclk, ulong pchctrl)
        {
            this.WarningLog("ADC is a stub. Does nothing.");
            _machine = machine;

            gclk?.RegisterPeripheralChannelFrequencyChange(pchctrl, FreqChanged);

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);

            _byteRegisters.DefineRegister((long)Registers.InterruptFlagStatusandClear, 0x1);

        }

        private void FreqChanged(long frequency)
        {
            this.WarningLog("Clock isn't handled.");
            this.DebugLog($"Frequency: [{frequency}]");
        }

        private readonly Machine _machine;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;


        private enum Registers : long
        {
            ControlA = 0x00,
            ControlB = 0x01,
            ReferenceControl = 0x02,
            EventControl = 0x03,
            InterruptEnableClear = 0x04,
            InterruptEnableSet = 0x05,
            InterruptFlagStatusandClear = 0x06,
            SequenceStatus = 0x07,
            InputControl = 0x08,
            ControlC = 0x0A,
            AverageControl = 0x0C,
            SamplingTimeControl = 0x0D,
            WindowMonitorLowerThreshold = 0x0E,
            WindowMonitorUpperThreshold = 0x10,
            GainControl = 0x12,
            OffsetCorrection = 0x14,
            SoftwareTrigger = 0x18,
            DebugControl = 0x1C,
            SynchronizationBusy = 0x20,
            Result = 0x24,
            SequenceControl = 0x28,
            Calibration = 0x2C
        }
    }
}
