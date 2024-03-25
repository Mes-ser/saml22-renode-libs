using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.Analog
{
    public class Saml22ADC : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x800;

        public void Reset()
        {
            doubleWordRegisters.Reset();
            wordRegisters.Reset();
            byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => doubleWordRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => wordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);

        public Saml22ADC(Machine machine)
        {
            this.WarningLog("ADC is a stub. Does nothing.");
            this.machine = machine;

            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);

            byteRegisters.DefineRegister((long)Registers.InterruptFlagStatusandClear, 0x1);

        }

        private readonly Machine machine;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly WordRegisterCollection wordRegisters;
        private readonly ByteRegisterCollection byteRegisters;


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
