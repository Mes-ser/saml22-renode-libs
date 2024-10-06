

using Antmicro.Renode.Core;
using Antmicro.Renode.Peripherals.UART;

namespace Antmicro.Renode.Peripherals.Mocks
{
    public class DriverTest : UARTBase
    {
        public DriverTest(Machine machine) : base(machine)
        {

        }

        public override Bits StopBits => throw new System.NotImplementedException();

        public override Parity ParityBit => throw new System.NotImplementedException();

        public override uint BaudRate => throw new System.NotImplementedException();

        protected override void CharWritten()
        {
            throw new System.NotImplementedException();
        }

        protected override void QueueEmptied()
        {
            // throw new System.NotImplementedException();
        }
    }
}
