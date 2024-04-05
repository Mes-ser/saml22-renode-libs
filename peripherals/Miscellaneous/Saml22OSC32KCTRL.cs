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
        public int XOSC32Frequency
        { 
            get => xosc32kFreq;
            set
            { 
                xosc32kReady = value == 32768;
                IRQManager.SetInterrupt(Interrupts.XOSC32Ready, xosc32kReady);
                xosc32kFreq = value;
            } 
        }

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
        private bool xosc32kReady;
        private int xosc32kFreq;
        private IFlagRegisterField xosc32kEnable;
        private IFlagRegisterField enable32Koutput;
        private IFlagRegisterField enable1KOutput;

        private void DefineRegisters()
        {

            doubleWordRegisters.AddRegister((long)Registers.InterruptEnableClear, IRQManager.GetInterruptEnableClearRegister<DoubleWordRegister>());
            doubleWordRegisters.AddRegister((long)Registers.InterruptEnableSet, IRQManager.GetInterruptEnableSetRegister<DoubleWordRegister>());
            doubleWordRegisters.AddRegister((long)Registers.InterruptFlagStatusandClear, IRQManager.GetRegister<DoubleWordRegister>(
                writeCallback: (irq, oldValue, newValue) => {
                    if(newValue) IRQManager.ClearInterrupt(irq);
            }, valueProviderCallback: (irq, _) => IRQManager.IsSet(irq)));

            doubleWordRegisters.DefineRegister((long)Registers.Status)
                .WithFlag(0, name: "XOSC32RDY", valueProviderCallback: (_) => xosc32kReady && xosc32kEnable.Value);

            byteRegisters.DefineRegister((long)Registers.RTCClockSelectionControl);
            byteRegisters.DefineRegister((long)Registers.SLCDClockSelectionControl);
            wordRegisters.DefineRegister((long)Registers.XOSC32KControl, 0x80)
                .WithIgnoredBits(0, 1)
                .WithFlag(1, out xosc32kEnable)
                .WithTaggedFlag("XTALEN", 2)
                .WithFlag(3, out enable32Koutput)
                .WithFlag(4, out enable1KOutput)
                .WithIgnoredBits(5, 1)
                .WithFlag(6, name:"RUNSTDBY")
                .WithFlag(7, name:"ONDEMAND")
                .WithValueField(8, 3, name: "STARTUP")
                .WithIgnoredBits(11, 1)
                .WithFlag(12, name: "WRTLOCK")
                .WithIgnoredBits(13, 3);
            
            byteRegisters.DefineRegister((long)Registers.ClockFailureDetectorControl);
            byteRegisters.DefineRegister((long)Registers.EventControl);
            doubleWordRegisters.DefineRegister((long)Registers.ULPInt32kControl); // Read from NVM calib

        }

        private enum Registers : long
        {
            InterruptEnableClear = 0x0,
            InterruptEnableSet = 0x04,
            InterruptFlagStatusandClear = 0x08,
            Status = 0x0C,
            RTCClockSelectionControl = 0x10,
            SLCDClockSelectionControl = 0x11,
            XOSC32KControl = 0x14,
            ClockFailureDetectorControl = 0x16,
            EventControl = 0x17,
            ULPInt32kControl = 0x1C,

        }

        private enum Interrupts
        {
            XOSC32Ready = 0,
            ClockFail = 2
        }
    }
}