using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.IRQControllers;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22MCLK : IDoubleWordPeripheral, IBytePeripheral, IKnownSize
    {
        public Saml22MCLK(Machine machine, ISAML22GCLK gclk, NVIC nvic)
        {
            _machine = machine;
            _nvic = nvic;
            _gclk = gclk;
            _gclk.GCLKClockChanged += MainClockChanged;

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);

            _doubleWordRegisters.DefineRegister((long)Registers.APBCMask)
                .WithFlags(0, 19, writeCallback: (flag, old, value) =>
                {
                    IPeripheral perip = _machine.SystemBus.WhatPeripheralIsAt((ulong)(0x42000000 + (0x400 * flag)));
                    if (perip != null)
                        _machine.SystemBus.SetPeripheralEnabled(perip, value);
                },
                valueProviderCallback: (flag, _) =>
                {
                    IPeripheral perip = _machine.SystemBus.WhatPeripheralIsAt((ulong)(0x42000000 + (0x400 * flag)));
                    if (perip != null)
                    {
                        return _machine.SystemBus.IsPeripheralEnabled(perip);
                    }
                    return false;
                }
            );
        }

        // Power Reset
        public void Reset()
        {
            _doubleWordRegisters.Reset();
            _byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);

        private void MainClockChanged(SAML22GCLKClock clock)
        {
            if (clock == SAML22GCLKClock.GCLK_MAIN)
            {
                CLK_CPU = _gclk.GCLK_MAIN;
            }
        }

        public long Size => 0x400;

        public long CLK_CPU
        {
            get => _nvic.Frequency;
            set => _nvic.Frequency = value;
        }

        private readonly Machine _machine;
        private readonly NVIC _nvic;
        private readonly ISAML22GCLK _gclk;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;


        private enum Registers : long
        {
            InterruptEnableClear = 0x01,
            InterruptEnableSet = 0x02,
            InterruptFlagStatusandClear = 0x03,
            CPUClockDivision = 0x04,
            BackupClockDivision = 0x06,
            AHBMask = 0x10,
            APBAMask = 0x14,
            APBBMask = 0x18,
            APBCMask = 0x1C
        }
    }
}
