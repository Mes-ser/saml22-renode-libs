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
            doubleWordRegisters.Reset();
            byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => doubleWordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);

        public Saml22MCLK(Machine machine)
        {
            this.WarningLog("MCLK is a stub. Does nothing.");
            this.machine = machine;

            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);
        }

        private readonly Machine machine;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly ByteRegisterCollection byteRegisters;


        private enum Registers : long
        {
            InterruptEnableClear = 0x01,
            InterruptEnableSet = 0x02,
            InterruptFlagStatusandClear = 0x03,
            CPUClockDivision = 0x04,
            BackupClockDivision = 0x06,
            AHBMask = 0x10,
            APBAMask =0x14,
            APBBMask = 0x18,
            APBCMask = 0x1C
        }
    }
}
