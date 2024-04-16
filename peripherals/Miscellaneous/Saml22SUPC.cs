using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22SUPC : IProvidesRegisterCollection<DoubleWordRegisterCollection>, IDoubleWordPeripheral, IKnownSize
    {
        public long Size => 0x400;

        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();

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

            IRQManager = new InterruptManager<Interrupts>(this);

            registers = new DoubleWordRegisterCollection(this);

            registers.AddRegister((long)Registers.InterruptEnableClear, IRQManager.GetInterruptEnableClearRegister<DoubleWordRegister>());
            registers.AddRegister((long)Registers.InterruptEnableSet, IRQManager.GetInterruptEnableSetRegister<DoubleWordRegister>());
            registers.AddRegister((long)Registers.InterruptFlagStatusandClear, IRQManager.GetRegister<DoubleWordRegister>(
                writeCallback: (irq, oldValue, newValue) => {
                    if(newValue)
                        IRQManager.ClearInterrupt(irq);
            }));
            registers.DefineRegister((long)Registers.Status, 0x705); // Temporary solution
        }

        private readonly Machine machine;

        private InterruptManager<Interrupts> IRQManager;

        private readonly DoubleWordRegisterCollection registers;

        private enum Interrupts
        {
            BOD33Ready = 0,
            BOD33Detection,
            BOD33SynchronizationReady,
            VoltageRegulatorReady = 8,
            AutomaticPowerSwitchReady,
            VDDCOREVoltageReady
        }

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
