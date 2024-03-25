using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22GCLK : IDoubleWordPeripheral, IBytePeripheral, IKnownSize
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

        public Saml22GCLK(Machine machine)
        {
            this.WarningLog("GCLK is a stub. Does nothing.");
            this.machine = machine;

            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);
        }

        private readonly Machine machine;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly ByteRegisterCollection byteRegisters;


        private enum Registers : long
        {
            ControlA = 0x00,
            SynchronizationBusy = 0x04,
            GeneratorControl = 0x20,
            PeripheralChannelControl = 0x80
        }
    }
}
