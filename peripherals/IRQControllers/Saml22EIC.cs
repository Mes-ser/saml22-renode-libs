using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.Miscellaneous;

namespace Antmicro.Renode.Peripherals.IRQControllers
{
    public class Saml22EIC : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize, ILocalGPIOReceiver
    {
        public long Size => 0x400;

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

        public IGPIOReceiver GetLocalReceiver(int index)
        {
            throw new System.NotImplementedException();
        }

        public Saml22EIC(Machine machine, ISAML22GCLK gclk, ulong pchctrl)
        {
            this.WarningLog("EIC is a stub. Does nothing.");
            _machine = machine;

            gclk?.RegisterPeripheralChannelFrequencyChange(pchctrl, FreqChanged);

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);
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
