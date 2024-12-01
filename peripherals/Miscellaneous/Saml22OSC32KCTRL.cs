using System;
using System.Collections.Generic;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Time;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22OSC32KCTRL : IBytePeripheral, IWordPeripheral, IDoubleWordPeripheral, ISAML22OSC32KCTRL, IKnownSize
    {

        public Saml22OSC32KCTRL(Machine machine)
        {
            _machine = machine;
            _interruptsManager = new InterruptManager<Interrupts>(this);

            _xosc32k = new XOSC32KClass(this);
            _xosc32k.ClockReady += XOSC32KReadyHandler;

            _byteRegisters = new ByteRegisterCollection(this);
            _wordRegisters = new WordRegisterCollection(this);
            _doubleWordRegisters = new DoubleWordRegisterCollection(this);

            DefineRegisters();
        }

        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);
        public ushort ReadWord(long offset) => _wordRegisters.Read(offset);
        public void WriteWord(long offset, ushort value) => _wordRegisters.Write(offset, value);
        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);

        public void Reset()
        {
            _byteRegisters.Reset();
            _wordRegisters.Reset();
            _doubleWordRegisters.Reset();
            _xosc32k.Reset();
        }

        private void XOSC32KReadyHandler(bool state)
        {
            if (state)
                _interruptsManager.SetInterrupt(Interrupts.XOSC32Ready);
        }

        public long Size => 0x400;

        [IrqProvider]
        public GPIO IRQ { get; } = new GPIO();
        public bool UseXOSC32K { get; set; } = false;

        private readonly Machine _machine;
        private readonly InterruptManager<Interrupts> _interruptsManager;

        private readonly ByteRegisterCollection _byteRegisters;
        private readonly WordRegisterCollection _wordRegisters;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;

        private readonly XOSC32KClass _xosc32k;

        public event Action<SAML22OSC32KClock> OSC32KClockChanged;

        private IHasFrequency _rtc => (IHasFrequency)_machine.SystemBus.WhatPeripheralIsAt((ulong)Saml22MemoryMap.RTCBaseAddress);

        public long XOSC32K { get => (long)_xosc32k.Frequency; set => throw new NotImplementedException(); }

        public long OSCULP32K => throw new NotImplementedException();

        public long XOSC32K_1K => throw new NotImplementedException();

        public long OSCULP32K_1k => throw new NotImplementedException();

        private void DefineRegisters()
        {
            _doubleWordRegisters.AddRegister((long)Registers.InterruptEnableClear, _interruptsManager.GetInterruptEnableClearRegister<DoubleWordRegister>());
            _doubleWordRegisters.AddRegister((long)Registers.InterruptEnableSet, _interruptsManager.GetInterruptEnableSetRegister<DoubleWordRegister>());
            _doubleWordRegisters.AddRegister((long)Registers.InterruptFlagStatusandClear, _interruptsManager.GetRegister<DoubleWordRegister>(
                writeCallback: (irq, oldValue, newValue) =>
                {
                    if (newValue) _interruptsManager.ClearInterrupt(irq);
                }, valueProviderCallback: (irq, _) => _interruptsManager.IsSet(irq)));

            _doubleWordRegisters.DefineRegister((long)Registers.Status)
                .WithFlag(0, name: "XOSC32RDY", valueProviderCallback: (_) => _xosc32k.Ready);

            _byteRegisters.DefineRegister((long)Registers.RTCClockSelectionControl)
                .WithValueField(0, 3, writeCallback: (_, value) =>
                {
                    switch (value)
                    {
                        case 0x0:
                            // _rtc.Frequency = ULP1K
                            break;
                        case 0x01:
                            // _rtc.Frequency = ULP32K;
                            break;
                        case 0x04:
                            // _rtc.Frequency = XOSC1K
                            break;
                        case 0x05:
                            // _rtc.Frequency = XOSC32K
                            break;
                        default:
                            break;
                    }
                });
            _byteRegisters.DefineRegister((long)Registers.SLCDClockSelectionControl);
            _wordRegisters.DefineRegister((long)Registers.XOSC32KControl)
                .WithValueField(0, 16, writeCallback: (old, value) => _xosc32k.WriteConfig(value),
                    valueProviderCallback: (_) => _xosc32k.ReadConfig()
                );

            _byteRegisters.DefineRegister((long)Registers.ClockFailureDetectorControl);
            _byteRegisters.DefineRegister((long)Registers.EventControl);
            _doubleWordRegisters.DefineRegister((long)Registers.ULPInt32kControl); // Read from NVM calib

        }

        private class XOSC32KClass
        {

            public bool Ready
            {
                get => _ready;
                private set
                {
                    _ready = value;
                    ClockReady?.Invoke(value);
                }
            }
            private bool _ready;

            public ulong Frequency
            {
                get
                {
                    if (EN1K && ENABLE)
                        return 32768;
                    return 0;
                }
            }
            public ulong Frequency1K
            {
                get
                {
                    if (EN1K && ENABLE)
                        return 1024;
                    return 0;
                }
            }
            public void WriteConfig(ulong value)
            {
                WRTLOCK = BitHelper.IsBitSet(value, (byte)ControlBit.WRTLOCK);
                if (!WRTLOCK)
                {
                    STARTUP = (value & 0xF00) >> (byte)ControlBit.STARTUP;
                    ONDEMAND = BitHelper.IsBitSet(value, (byte)ControlBit.ONDEMAND);
                    RUNSTDBY = BitHelper.IsBitSet(value, (byte)ControlBit.RUNSTDBY);
                    EN1K = BitHelper.IsBitSet(value, (byte)ControlBit.EN1K);
                    EN32K = BitHelper.IsBitSet(value, (byte)ControlBit.EN32K);
                    XTALEN = BitHelper.IsBitSet(value, (byte)ControlBit.XTALEN);
                    ENABLE = BitHelper.IsBitSet(value, (byte)ControlBit.ENABLE);
                }
            }

            public ulong ReadConfig()
            {
                ulong reg = 0;
                BitHelper.SetBit(ref reg, (byte)ControlBit.ENABLE, ENABLE);
                BitHelper.SetBit(ref reg, (byte)ControlBit.XTALEN, XTALEN);
                BitHelper.SetBit(ref reg, (byte)ControlBit.EN32K, EN32K);
                BitHelper.SetBit(ref reg, (byte)ControlBit.EN1K, EN1K);
                BitHelper.SetBit(ref reg, (byte)ControlBit.RUNSTDBY, RUNSTDBY);
                BitHelper.SetBit(ref reg, (byte)ControlBit.ONDEMAND, ONDEMAND);
                reg |= STARTUP << (byte)ControlBit.STARTUP;
                BitHelper.SetBit(ref reg, (byte)ControlBit.WRTLOCK, WRTLOCK);

                return reg;
            }

            public void Reset()
            {
                ENABLE = false;
                XTALEN = false;
                EN32K = false;
                EN1K = false;
                RUNSTDBY = false;
                ONDEMAND = true;
                STARTUP = 0x0;
                WRTLOCK = false;
                Ready = false;
            }
            public XOSC32KClass(Saml22OSC32KCTRL osc32ctrl)
            {
                _osc32kctrl = osc32ctrl;
            }

            public Action<bool> ClockReady;

            private readonly Saml22OSC32KCTRL _osc32kctrl;

            private bool ENABLE
            {
                get => _enabled;
                set
                {
                    if (_enabled != value)
                    {
                        if (value)
                        {
                            _osc32kctrl._machine.ScheduleAction(
                                TimeInterval.FromMicroseconds((1000000 / 32786) * _startUpCycles[STARTUP]),
                                (_) => Ready = true
                            );
                        }
                        _enabled = value;
                    }
                }
            }

            private bool _enabled;
            private bool XTALEN { get; set; }
            private bool EN32K { get; set; }
            private bool EN1K { get; set; }
            private bool RUNSTDBY { get; set; }
            private bool ONDEMAND { get; set; } = true;
            private ulong STARTUP { get; set; }
            private bool WRTLOCK { get; set; }

            private readonly Dictionary<ulong, ulong> _startUpCycles = new Dictionary<ulong, ulong>() {
                {0x0, 2048},
                {0x1, 4096},
                {0x2, 16384},
                {0x3, 32768},
                {0x4, 65536},
                {0x5, 131072},
                {0x6, 262144}
            };

            private enum ControlBit
            {
                ENABLE = 1,
                XTALEN = 2,
                EN32K = 3,
                EN1K = 4,
                RUNSTDBY = 6,
                ONDEMAND = 7,
                STARTUP = 8,
                WRTLOCK = 12
            }
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
