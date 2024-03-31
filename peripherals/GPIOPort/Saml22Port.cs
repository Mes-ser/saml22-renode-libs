

using System.Collections.Generic;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.GPIOPort
{
    public class Saml22Port : IDoubleWordPeripheral, IBytePeripheral, IKnownSize, IPeripheralRegister<Saml22PortGroup, NumberRegistrationPoint<int>>
    {
        public long Size => 0x2000;

        public void Reset()
        {
            foreach (Saml22PortGroup group in groups)
                group.Reset();
        }

        public uint ReadDoubleWord(long offset) => groups[(int)(offset / GROUP_OFFSET)].doubleWordRegisters.Read(offset % GROUP_OFFSET);
        public void WriteDoubleWord(long offset, uint value) => groups[(int)(offset / GROUP_OFFSET)].doubleWordRegisters.Write(offset % GROUP_OFFSET, value);
        public byte ReadByte(long offset) => groups[(int)(offset / GROUP_OFFSET)].byteRegisters.Read(offset % GROUP_OFFSET);
        public void WriteByte(long offset, byte value) => groups[(int)(offset / GROUP_OFFSET)].byteRegisters.Write(offset % GROUP_OFFSET, value);

        public void Register(Saml22PortGroup peripheral, NumberRegistrationPoint<int> registrationPoint)
        {
            machine.RegisterAsAChildOf(this, peripheral, registrationPoint);
            groups.Add(peripheral);
            this.InfoLog($"Registered IOPins group [{(char)('A' + registrationPoint.Address)}]");
        }

        public void Unregister(Saml22PortGroup peripheral)
        {
            machine.UnregisterAsAChildOf(this, peripheral);
        }

        public Saml22Port(Machine machine)
        {
            this.InfoLog("PORT is a collection of PortGroup objects.");
            this.machine = machine;
        }

        private const int GROUP_OFFSET = 0x80;

        private readonly Machine machine;
        private readonly List<Saml22PortGroup> groups = new List<Saml22PortGroup>();

        private enum Registers : long
        {
            groupA = 0x0,
            groupB = GROUP_OFFSET
        }

    }
}
