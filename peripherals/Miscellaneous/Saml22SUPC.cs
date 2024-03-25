using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22SUPC : IProvidesRegisterCollection<DoubleWordRegisterCollection>, IDoubleWordPeripheral, IKnownSize
    {
        public long Size => 0x400;

        public DoubleWordRegisterCollection RegistersCollection => registers;

        public void Reset()
        {
            registers.Reset();
        }

        public uint ReadDoubleWord(long offset) => registers.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => registers.Write(offset, value);

        public Saml22SUPC(Machine machine)
        {
            this.WarningLog("SUPC is a stub. Does nothing.");
            this.machine = machine;

            registers = new DoubleWordRegisterCollection(this);

            registers.DefineRegister((long)Registers.Status, 0x701); // Temporary solution
        }

        private readonly Machine machine;
        private readonly DoubleWordRegisterCollection registers;

        private enum Registers : long
        {
            InterruptEnableClear = 0x00,
            InterruptEnableSet = 0x04,
            InterruptFlagStatusandClear = 0x08,
            Status = 0x0C,
            BorwnOutDetectorBOD33Control = 0x10,
            VoltageRegulatorSystemControl = 0x18,
            VoltageReferencesSystemControl = 0x1C,
            BatteryBackupPowerSwitchControl = 0x20,
            BackupOutputControl = 0x24,
            BackupInputValue = 0x28
        }
    }
}
