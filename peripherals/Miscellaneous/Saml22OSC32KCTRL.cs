using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22OSC32KCTRL : IBytePeripheral, IWordPeripheral, IDoubleWordPeripheral, IKnownSize
    {
        public long Size => 0x400;

        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => wordRegisters.Write(offset, value);
        public uint ReadDoubleWord(long offset) => doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => doubleWordRegisters.Write(offset, value);

        public void Reset()
        {
            byteRegisters.Reset();
            wordRegisters.Reset();
            doubleWordRegisters.Reset();
        }

        public Saml22OSC32KCTRL(Machine machine)
        {
            this.machine = machine;
            IRQManager = new InterruptManager<Interrupts>(this);

            byteRegisters = new ByteRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            doubleWordRegisters = new DoubleWordRegisterCollection(this);

            DefineRegisters();
        }

        private readonly Machine machine;
        private readonly InterruptManager<Interrupts> IRQManager;

        private readonly ByteRegisterCollection byteRegisters;
        private readonly WordRegisterCollection wordRegisters;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;

        private void DefineRegisters()
        {
            doubleWordRegisters.DefineRegister((long)Registers.Status)
                .WithFlag(0, valueProviderCallback: (_) => true);
        }

        private enum Registers : long
        {
            InterruptEnableClear = 0x0,
            InterruptEnableSet = 0x04,
            InterruptFlagStatusandClear = 0x08,
            Status = 0x0C,
            RTCClockSelectionControl = 0x10,
            SLCDClockSelectionControl = 0x11,
            ExternalCrystalOscillatorXOSC32KControl = 0x14,
            ClockFailureDetectorControl = 0x16,
            EventControl = 0x17,
            UltraLowPowerInternalOscillatorOSCULP32KControl = 0x1C,

        }

        private enum Interrupts
        {
            XOSC32Ready = 0,
            ClockFail = 2
        }
    }
}