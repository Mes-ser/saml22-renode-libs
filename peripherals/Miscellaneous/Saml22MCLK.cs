using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22MCLK : IDoubleWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x400;

        public void Reset()
        {
            _doubleWordRegisters.Reset();
            _byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);

        public Saml22MCLK(Machine machine)
        {
            this.WarningLog("MCLK is a stub. Does nothing.");
            _machine = machine;

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);
        }

        private readonly Machine _machine;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;


        private enum Registers : long
        {
            InterruptEnableClear = 0x01,
            InterruptEnableSet = 0x02,
            InterruptFlagStatusandClear = 0x03,
            CPUClockDivision = 0x04,
            BackupClockDivision = 0x06,
            AHBMask = 0x10,
            APBAMask = 0x14,
            APBBMask = 0x18,
            APBCMask = 0x1C
        }
    }
}
