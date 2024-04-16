using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.IRQControllers
{
    public class Saml22EIC : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x400;

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

        public Saml22EIC(Machine machine)
        {
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
            NonMaskableInterruptControl = 0x01,
            NonMaskableInterruptFlagStatusandClear = 0x02,
            SynchronizationBusy = 0x04,
            EventControl = 0x08,
            InterruptEnableClear = 0x0C,
            InterruptEnableSet = 0x10,
            InterruptFlagStatusandClear = 0x14,
            ExternalInterruptAsynchronousMode = 0x18,
            ExternalInterruptSenseConfiguration0 = 0x1C,
            ExternalInterruptSenseConfiguration1 = 0x20,
        }
    }
}
