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
            _doubleWordRegisters.Reset();
            _byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);

        public Saml22GCLK(Machine machine)
        {
            this.WarningLog("GCLK is a stub. Does nothing.");
            _machine = machine;

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);
        }

        private readonly Machine _machine;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;


        private enum Registers
        {
            CTRLA = 0x00,
            SYNCBUSY = 0x04,
            GENCTRL0 = 0x20,
            GENCTRL1 = 0x24,
            GENCTRL2 = 0x28,
            GENCTRL3 = 0x2C,
            GENCTRL4 = 0x30,
            PCHCTRL0 = 0x80,
            PCHCTRL1 = 0x84,
            PCHCTRL2 = 0x88,
            PCHCTRL3 = 0x8C,
            PCHCTRL4 = 0x90,
            PCHCTRL5 = 0x94,
            PCHCTRL6 = 0x98,
            PCHCTRL7 = 0x9C,
            PCHCTRL8 = 0xA0,
            PCHCTRL9 = 0xA4,
            PCHCTRL10 = 0xA8,
            PCHCTRL11 = 0xAC,
            PCHCTRL12 = 0xB0,
            PCHCTRL13 = 0xB4,
            PCHCTRL14 = 0xB8,
            PCHCTRL15 = 0xBC,
            PCHCTRL16 = 0xC0,
            PCHCTRL17 = 0xC4,
            PCHCTRL18 = 0xC8,
            PCHCTRL19 = 0xCC,
            PCHCTRL20 = 0xD0,
            PCHCTRL21 = 0xD4,
            PCHCTRL22 = 0xD8,
            PCHCTRL23 = 0xDC,
            PCHCTRL24 = 0xE0,
            PCHCTRL25 = 0xE4,
            PCHCTRL26 = 0xE8,
            PCHCTRL27 = 0xEC,
            PCHCTRL28 = 0xF0,
        }
    }
}
