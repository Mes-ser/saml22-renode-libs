using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22RTC : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize
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

        public Saml22RTC(Machine machine)
        {
            this.WarningLog("RTC is a stub. Does nothing.");
            this.machine = machine;

            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);

        }

        private readonly Machine machine;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly WordRegisterCollection wordRegisters;
        private readonly ByteRegisterCollection byteRegisters;


        private enum Registers : long
        {
            ControlA = 0x00,
            ControlB = 0x02,
            EventControl = 0x04,
            InterruptEnableClear = 0x08,
            InterruptEnableSet = 0x0A,
            InterruptFlagStatusandClear = 0x0C,
            DebugControl = 0x0E,
            SynchronizationBusy = 0x10,
            FrequencyCorrelation = 0x14,
            CounterValue = 0x18,
            ClockValue = 0x18,
            CounterPeriod = 0x1C,
            AlarmValue = 0x20,
            Compare0 = 0x20,
            Compare1 = 0x22,
            AlarmMask = 0x24,
            GeneralPurpose0 = 0x40,
            GeneralPurpose1 = 0x44,
            TamperControl = 0x60,
            Timestamp = 0x64,
            TamperID = 0x68,
            Backup = 0x80,

        }
    }
}
