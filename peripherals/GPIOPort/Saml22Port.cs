

using System.Collections.Generic;
using System.Linq;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.GPIOPort
{
    public class Saml22Port : IDoubleWordPeripheral, IBytePeripheral, IKnownSize, IPeripheralRegister<IGPIOReceiver, NumberRegistrationPoint<int>>
    {
        public long Size => 0x2000;

        public void Reset()
        {
            foreach (Saml22PortGroup port in _portsGroup.Cast<Saml22PortGroup>())
            {
                port.Reset();
            }
        }

        public uint ReadDoubleWord(long offset) => ((Saml22PortGroup)_portsGroup[(int)(offset / GROUP_OFFSET)]).doubleWordRegisters.Read(offset % GROUP_OFFSET);
        public void WriteDoubleWord(long offset, uint value) => ((Saml22PortGroup)_portsGroup[(int)(offset / GROUP_OFFSET)]).doubleWordRegisters.Write(offset % GROUP_OFFSET, value);
        public byte ReadByte(long offset) => ((Saml22PortGroup)_portsGroup[(int)(offset / GROUP_OFFSET)]).byteRegisters.Read(offset % GROUP_OFFSET);
        public void WriteByte(long offset, byte value) => ((Saml22PortGroup)_portsGroup[(int)(offset / GROUP_OFFSET)]).byteRegisters.Write(offset % GROUP_OFFSET, value);

        public void Register(IGPIOReceiver port, NumberRegistrationPoint<int> portNumber)
        {
            _machine.RegisterAsAChildOf(this, port, portNumber);
            _portsGroup.Add(port);
            this.InfoLog($"Registered IOPins group [{(char)('A' + portNumber.Address)}]");
        }

        public void Unregister(IGPIOReceiver peripheral)
        {
            _machine.UnregisterAsAChildOf(this, peripheral);
        }

        public Saml22Port(Machine machine)
        {
            this.InfoLog("PORT is a collection of PortGroup objects.");
            _machine = machine;
        }

        private const int GROUP_OFFSET = 0x80;

        private readonly Machine _machine;
        private readonly List<IGPIOReceiver> _portsGroup = new();

        private enum Registers : long
        {
            GroupAdirection = 0x0,
            GroupADirectionClear = 0x4,
            GroupADirectionSet = 0x8,
            GroupADirectiontoggle = 0xC,
            GroupAOutputValue = 0x10,
            GroupAOutputValueClear = 0x14,
            GroupAOutputValueSet = 0x18,
            GroupAOutputValueToggle = 0x1C,
            GroupAInputValue = 0x20,
            GroupAControl = 0x24,
            GroupAWriteConfiguration = 0x28,
            GroupAEventInputcontrol = 0x2C,
            GroupAPeripheralMultiplexingX = 0x30,
            GroupAPinConfiguration = 0x40,
            GroupBDirection = GROUP_OFFSET,
            GroupBDirectionClear = GroupBDirection + GroupBDirection + 0x4,
            GroupBDirectionSet = GroupBDirection + 0x8,
            GroupBDirectiontoggle = GroupBDirection + 0xC,
            GroupBOutputValue = GroupBDirection + 0x10,
            GroupBOutputValueClear = GroupBDirection + 0x14,
            GroupBOutputValueSet = GroupBDirection + 0x18,
            GroupBOutputValueToggle = GroupBDirection + 0x1C,
            GroupBInputValue = GroupBDirection + 0x20,
            GroupBControl = GroupBDirection + 0x24,
            GroupBWriteConfiguration = GroupBDirection + 0x28,
            GroupBEventInputcontrol = GroupBDirection + 0x2C,
            GroupBPeripheralMultiplexingX = GroupBDirection + 0x30,
            GroupBPinConfiguration = GroupBDirection + 0x40
        }

    }
}
