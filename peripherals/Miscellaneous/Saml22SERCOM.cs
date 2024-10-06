


using System;
using System.Collections.Generic;
using System.Linq;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Peripherals.SPI;
using Antmicro.Renode.Peripherals.UART;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22SERCOM :
        IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize,
        IPeripheralContainer<IUART, NullRegistrationPoint>
    // IPeripheralContainer<ISPIPeripheral, NullRegistrationPoint>,
    // IPeripheralContainer<II2CPeripheral, NumberRegistrationPoint<int>>
    {

        public byte ReadByte(long offset)
        {
            throw new NotImplementedException();
        }

        public uint ReadDoubleWord(long offset)
        {
            throw new NotImplementedException();
        }

        public ushort ReadWord(long offset)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            // throw new NotImplementedException();
        }

        public void WriteByte(long offset, byte value)
        {
            throw new NotImplementedException();
        }

        public void WriteDoubleWord(long offset, uint value)
        {
            throw new NotImplementedException();
        }

        public void WriteWord(long offset, ushort value)
        {
            throw new NotImplementedException();
        }

        public Saml22SERCOM(Machine machine)
        {
            // machine.RegisterAsAChildOf(this, );
            this._machine = machine;

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);


        }
        private readonly Machine _machine;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;

        public long Size => 0x400;

        #region UART
        public IEnumerable<IRegistered<IUART, NullRegistrationPoint>> Children => throw new NotImplementedException();

        public IEnumerable<NullRegistrationPoint> GetRegistrationPoints(IUART peripheral)
        {
            throw new NotImplementedException();
        }

        public void Register(IUART peripheral, NullRegistrationPoint registrationPoint)
        {
            throw new NotImplementedException();
        }

        public void Unregister(IUART peripheral)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region SPI
        #endregion

        #region IIC
        #endregion
    }
}
