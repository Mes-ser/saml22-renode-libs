

using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.GPIOPort
{
    public class Saml22Port : IDoubleWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x2000;

        public void Reset()
        {
            doubleWordRegisters.Reset();
            byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => doubleWordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);

        public Saml22Port(Machine machine)
        {
            this.WarningLog("PORT is a stub. Does nothing.");
            this.machine = machine;

            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);
        }

        private readonly Machine machine;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly ByteRegisterCollection byteRegisters;


        private enum Registers : long
        {
            DataDirection = 0x00,
            DataDirectionClear = 0x04,
            DataDirectionSet = 0x08,
            DataDirectiontoggle = 0x0C,
            DataOutputValue = 0x10,
            DataOutputValueClear = 0x14,
            DataOutputValueSet = 0x18,
            DataOutputValueToggle = 0x1C,
            DataInputValue = 0x20,
            Control = 0x24,
            WriteConfiguration = 0x28,
            EventInputControl = 0x2C,
            PeripheralMultiplexingX = 0x30,
            PinConfigurationN = 0x40
        }
    }
}
