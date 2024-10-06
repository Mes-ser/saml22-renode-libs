using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Timers
{
    public class Saml22TCC : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x400;
        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

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

        public Saml22TCC(Machine machine)
        {
            _machine = machine;
            _interruptsManager = new InterruptManager<Interrupts>(this);

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);
        }

        private readonly Machine _machine;
        private readonly InterruptManager<Interrupts> _interruptsManager;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;

        private enum Interrupts
        {
            Overflow,
            Retrigger,
            Counter,
            Error,
            NonRecoverableUpdateFault = 10,
            NonRecoverableDebugFaultState,
            RecoverableFaultA,
            RecoverableFaultB,
            NonRecoverableFault0,
            NonRecoverableFault1,
            MatchOrCaptureChannel0,
            MatchOrCaptureChannel1,
            MatchOrCaptureChannel2,
            MatchOrCaptureChannel3
        }

        private enum Registers : long
        {
            ControlA = 0x00,
            ControlBClear = 0x04,
            ControlBSet = 0x05,
            SynchronizatinBusy = 0x08,
            FaultControlA = 0x0C,
            FaultControlB = 0x10,
            WaveformExtensionControl = 0x14,
            DriverControl = 0x18,
            DebugControl = 0x1E,
            EventControl = 0x20,
            InterruptEnableClear = 0x24,
            InterruptEnableSet = 0x28,
            InterrptFlagStatusandClear = 0x2C,
            Status = 0x30,
            CounterValue = 0x34,
            Pattern = 0x38,
            Waveform = 0x3C,
            PeriodValue = 0x40,
            CompareCaptureChannel0 = 0x44,
            CompareCaptureChannel1 = 0x48,
            CompareCaptureChannel2 = 0x4C,
            CompareCaptureChannel3 = 0x50,
            PatternBuffer = 0x64,
            PeriodBufferValue = 0x6C,
            Channel0CompareCaptureBufferValue = 0x70,
            Channel1CompareCaptureBufferValue = 0x74,
            Channel2CompareCaptureBufferValue = 0x78,
            Channel3CompareCaptureBufferValue = 0x7C,
        }
    }
}
