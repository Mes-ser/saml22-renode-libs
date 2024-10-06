using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Core.USB;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.USB
{
    public class Saml22USB : IUSBDevice, IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x2000;

        public USBDeviceCore USBCore { get; private set; }

        public void Reset()
        {
            _doubleWordRegisters.Reset();
            _byteRegisters.Reset();

            USBCore = new USBDeviceCore(this);
        }

        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);

        public Saml22USB(Machine machine)
        {
            this.WarningLog("USB is a stub. Does nothing.");
            _machine = machine;

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);
        }

        private readonly Machine _machine;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;

        private enum Registers : long
        {
            ControlA = 0x00,
            SynchronizationBusy = 0x02,
            QOSControl = 0x3,
            FiniteStateMachineStatus = 0xD,
            ControlB = 0x08,
            DeviceAddres = 0x0A,
            Status = 0x0C,
            DeviceFrameNumber = 0x10,
            DeviceInterruptEnableClear = 0x14,
            DeviceInterruptEnableSet = 0x18,
            DeviceInterruptFlagStatusandClear = 0x1C,
            EndpointInterruptSummary = 0x20,
            DescriptoAddress = 0x24,
            PadCalibration = 0x28,
            DeviceEndpointConfiguration = 0x100,
            EndpointStatusClear = 0x104,
            EndpointStatusSet = 0x105,
            EndpointStatus = 0x106,
            DeviceEndpointInterruptFlag = 0x107,
            DeviceEndpointInterruptEnable = 0x108,
            DeviceInterruptEndpointSet = 0x109,
        }
    }
}
