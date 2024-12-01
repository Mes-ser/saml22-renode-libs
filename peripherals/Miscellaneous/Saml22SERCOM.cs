using System;
using System.Collections.Generic;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.UART;
using Antmicro.Renode.Utilities;
using static Antmicro.Renode.Peripherals.Bus.Wrappers.RegisterMapper;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22SERCOM :
        IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize,
        IPeripheralContainer<IUART, NullRegistrationPoint>
    // IPeripheralContainer<ISPIPeripheral, NullRegistrationPoint>,
    // IPeripheralContainer<II2CPeripheral, NumberRegistrationPoint<int>>
    {
        public Saml22SERCOM(IMachine machine, ISAML22GCLK gclk, ulong pchctrl)
        {
            _machine = machine;

            if (gclk != null)
            {
                gclk.RegisterPeripheralChannelFrequencyChange(pchctrl, CoreFreqChanged);
                gclk.RegisterPeripheralChannelFrequencyChange(SLOW_FREQ_CHANNEL_ID, SlowFreqChanged);
            }

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);
        }

        public void Reset()
        {
            _byteRegisters.Reset();
            _wordRegisters.Reset();
            _doubleWordRegisters.Reset();
        }

        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);

        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);
        public void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);

        private void CoreFreqChanged(long frequency)
        {
            this.WarningLog("Clock isn't handled.");
            this.DebugLog($"Core frequency: [{frequency}]");
        }
        private void SlowFreqChanged(long frequency)
        {
            this.WarningLog("Slow Clock isn't handled.");
            this.DebugLog($"Slow frequency: [{frequency}]");
        }

        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();
        public GPIO TXRequest { get; } = new GPIO();
        public GPIO RXRequest { get; } = new GPIO();
        public long Size => 0x400;

        private const ulong SLOW_FREQ_CHANNEL_ID = 15;

        private readonly IMachine _machine;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;

        // Is that bad to use regions?
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

        [RegistersDescriptionAttribute]
        private enum Registers
        {
            ControlA = 0x0,
            ControlB = 0x4,
            BaudRate = 0xC,
            ReceivePulseLength = 0xE,
            InterruptEnableClear = 0x14,
            InterruptEnableSet = 0x16,
            InterruptFlagStatusAndClear = 0x18,
            Status = 0x1A,
            SynchronizationBusy = 0x1C,
            ReceiveErrorCount = 0x20,
            Address = 0x24,
            Data = 0x28,
            DebugControl = 0x30
        }

        [Flags]
        private enum Interrupts
        {
            DRE = 0,
            PREC = 0,
            MB = 0,
            TXC = 1,
            AMATCH = 1,
            SB = 1,
            RXC = 2,
            DRDY = 2,
            RXS = 3,
            SSL = 3,
            CTSIC = 4,
            RXBRK = 5,
            ERROR = 7
        }
    }
}
