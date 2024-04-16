using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22SLCD : IDoubleWordPeripheral, IWordPeripheral, IBytePeripheral, IKnownSize
    {
        public long Size => 0x400;
        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

        public void Reset()
        {
            doubleWordRegisters.Reset();
            wordRegisters.Reset();
            byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => doubleWordRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => wordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => byteRegisters.Write(offset, value);

        public Saml22SLCD(Machine machine)
        {
            this.machine = machine;

            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            wordRegisters = new WordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);
        }

        private readonly Machine machine;
        private readonly InterruptManager<Interrupts> IRQManager;
        private readonly DoubleWordRegisterCollection doubleWordRegisters;
        private readonly WordRegisterCollection wordRegisters;
        private readonly ByteRegisterCollection byteRegisters;

        private enum Interrupts
        {
            FrameCounter0Overflow,
            FrameCounter1Overflow,
            FrameCounter2Overflow,
            VLCDReadytoggle,
            VLCDStatusToggle,
            PumpRunStatustoggle
        }

        private enum Registers : long
        {
            ControlA = 0x00,
            ControlB = 0x04,
            ControlC = 0x06,
            ControlD = 0x08,
            EventControl = 0x0C,
            InterruptEnableClear = 0x0D,
            InterruptEnableSet = 0x0E,
            InterrptFlag = 0x0F,
            Status = 0x10,
            SynchronizatinBusy = 0x14,
            FrameCounter0 = 0x18,
            FrameCounter1 = 0x19,
            FrameCounter2 = 0x1A,
            LCDPinEnableLow = 0x1C,
            LCDPinEnableHigh = 0x20,
            SegmentDataLow0 = 0x24,
            SegmentDataHigh0 = 0x28,
            SegmentDataLow1 = 0x2C,
            SegmentDataHigh = 0x30,
            SegmentDataLow2 = 0x34,
            SegmentDataHigh2 = 0x38,
            SegmentDataLow3 = 0x3C,
            SegmentDataHigh3 = 0x40,
            SegmentDataLow4 = 0x44,
            SegmentDataHigh4 = 0x48,
            SegmentDataLow5 = 0x4C,
            SegmentDataHigh5 = 0x50,
            SegmentDataLow6 = 0x54,
            SegmentDataHigh6 = 0x58,
            SegmentDataLow7 = 0x5C,
            SegmentDataHigh7 = 0x60,
            IndirectSegmentData = 0x64,
            BlinkConfiguration = 0x68,
            CircularShiftRegisterConfiguration = 0x6C,
            CharacterMappingConfiguration = 0x70,
            AutomatedCharacterMappingConfiguration = 0x74,
            AutomatedBitMappingConfiguration = 0x78,
            CharacterMappingSegemntsData = 0x7C,
            CharacterMappingDataMask = 0x80,
            CharacterMappingIndex = 0x84
        }

    }
}
