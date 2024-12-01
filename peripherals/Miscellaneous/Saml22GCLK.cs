using System;
using System.Collections.Generic;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class Saml22GCLK : IDoubleWordPeripheral, IBytePeripheral, IKnownSize, ISAML22GCLK
    {

        public Saml22GCLK(Machine machine, ISAML22OSCCTRL oscctrl, ISAML22OSC32KCTRL osc32kctrl)
        {
            _machine = machine;
            _oscctrl = oscctrl;
            _osc32kctrl = osc32kctrl;

            _oscctrl.OSCClockChanged += OSCClockChanged;

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);

            _generators = new Dictionary<int, Generator>
            {
                { 0, new Generator(this, 0, Generator.ClockSource.OSC16M, true) }
            };
            _generators[0].FrequencyChanged += (freq) => GCLKClockChanged?.Invoke(SAML22GCLKClock.GCLK_MAIN);

            for (int i = 1; i < 5; i++)
            {
                _generators.Add(i, new Generator(this, i));
            }

            _peripheralChannelsControl = new Dictionary<int, PeripheralChannelControl>();

            for (int i = 0; i < 29; i++)
            {
                _peripheralChannelsControl.Add(i, new PeripheralChannelControl(this, i));
            }

            DefineRegisters();
        }

        // Assume this is POWER Reset
        public void Reset()
        {
            _doubleWordRegisters.Reset();
            _byteRegisters.Reset();

            // TODO: Add PCHCTRL.Reset
            // TODO: Add Generator.Reset 
        }
        private void SoftwareReset()
        {
            // TODO: Add PCHCTRL.SoftwareReset
            // TODO: Add Generator.SoftwareReset
        }

        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);

        private void OSCClockChanged(SAML22OSCClock clock)
        {
            switch (clock)
            {
                case SAML22OSCClock.OSC16M:
                    OSC16MFreqChanged?.Invoke(_oscctrl.OSC16M);
                    break;
                default:
                    this.WarningLog($"Unhandled Clock source [{clock}]");
                    break;
            }
        }

        private void RegisterClockChangeHandler(Generator.ClockSource clkSrc, Action<long> handler)
        {
            switch (clkSrc)
            {
                case Generator.ClockSource.XOSC:
                    XOSC32KFreqChanged += handler;
                    break;
                case Generator.ClockSource.OSC16M:
                    OSC16MFreqChanged += handler;
                    break;
                default:
                    this.WarningLog($"Cannot Register Unknown Generator Clock Source: [{clkSrc}]");
                    break;
            }
        }

        private void UnregisterClockChangeHandler(Generator.ClockSource clkSrc, Action<long> handler)
        {
            switch (clkSrc)
            {
                case Generator.ClockSource.XOSC:
                    XOSC32KFreqChanged -= handler;
                    break;
                case Generator.ClockSource.OSC16M:
                    OSC16MFreqChanged -= handler;
                    break;
                default:
                    this.WarningLog($"Cannot Unregister Unknown Generator Clock Source: [{clkSrc}]");
                    break;
            }
        }

        private long GetFrequencyFromClock(Generator.ClockSource clk)
        {
            switch (clk)
            {
                case Generator.ClockSource.XOSC32K:
                    return _osc32kctrl.XOSC32K;
                case Generator.ClockSource.OSC16M:
                    return _oscctrl.OSC16M;
                default:
                    this.WarningLog($"Can't get frequency from unknown clock: [{clk}]");
                    return 0;
            }
        }
        public void RegisterPeripheralChannelFrequencyChange(ulong id, Action<long> handler)
        {
            _peripheralChannelsControl[(int)id].RegisterFreqChangeHandler(handler);
        }

        private void DefineRegisters()
        {
            _byteRegisters.DefineRegister((long)Registers.CTRLA)
                .WithWriteCallback((old, value) =>
                {
                    if (BitHelper.IsBitSet(value, 0))
                        // Change to SWRESET()
                        Reset();
                });

            _doubleWordRegisters.DefineRegister((long)Registers.GENCTRL0)
                .WithValueField(0, 4, writeCallback: (old, value) => _generators[0].SRC = value,
                    valueProviderCallback: (_) => _generators[0].SRC)
                .WithFlag(8, writeCallback: (old, value) => _generators[0].GENEN = value,
                    valueProviderCallback: (_) => _generators[0].GENEN)
                .WithFlag(9, writeCallback: (old, value) => _generators[0].IDC = value,
                    valueProviderCallback: (_) => _generators[0].IDC)
                .WithFlag(10, writeCallback: (old, value) => _generators[0].OOV = value,
                    valueProviderCallback: (_) => _generators[0].OOV)
                .WithFlag(11, writeCallback: (old, value) => _generators[0].OE = value,
                    valueProviderCallback: (_) => _generators[0].OE)
                .WithFlag(12, writeCallback: (old, value) => _generators[0].DIVSEL = value,
                    valueProviderCallback: (_) => _generators[0].DIVSEL)
                .WithFlag(13, writeCallback: (old, value) => _generators[0].RUNSTDBY = value,
                    valueProviderCallback: (_) => _generators[0].RUNSTDBY)
                .WithValueField(16, 8, writeCallback: (old, value) => _generators[0].DIV = (long)value,
                    valueProviderCallback: (_) => (ulong)_generators[0].DIV);

            _doubleWordRegisters.DefineRegister((long)Registers.GENCTRL3)
                .WithValueField(0, 4, writeCallback: (old, value) => _generators[3].SRC = value,
                    valueProviderCallback: (_) => _generators[3].SRC)
                .WithFlag(8, writeCallback: (old, value) => _generators[3].GENEN = value,
                    valueProviderCallback: (_) => _generators[3].GENEN)
                .WithFlag(9, writeCallback: (old, value) => _generators[3].IDC = value,
                    valueProviderCallback: (_) => _generators[3].IDC)
                .WithFlag(10, writeCallback: (old, value) => _generators[3].OOV = value,
                    valueProviderCallback: (_) => _generators[3].OOV)
                .WithFlag(11, writeCallback: (old, value) => _generators[3].OE = value,
                    valueProviderCallback: (_) => _generators[3].OE)
                .WithFlag(12, writeCallback: (old, value) => _generators[3].DIVSEL = value,
                    valueProviderCallback: (_) => _generators[3].DIVSEL)
                .WithFlag(13, writeCallback: (old, value) => _generators[3].RUNSTDBY = value,
                    valueProviderCallback: (_) => _generators[3].RUNSTDBY)
                .WithValueField(16, 8, writeCallback: (old, value) => _generators[3].DIV = (long)value,
                    valueProviderCallback: (_) => (ulong)_generators[3].DIV);

            _doubleWordRegisters.DefineRegister((long)Registers.PCHCTRL3)
                .WithValueField(0, 32, writeCallback: _peripheralChannelsControl[3].WriteConfig,
                    valueProviderCallback: _peripheralChannelsControl[3].ReadConfig
                );

            _doubleWordRegisters.DefineRegister((long)Registers.PCHCTRL15)
                .WithValueField(0, 32, writeCallback: _peripheralChannelsControl[15].WriteConfig,
                    valueProviderCallback: _peripheralChannelsControl[15].ReadConfig
                );
            _doubleWordRegisters.DefineRegister((long)Registers.PCHCTRL20)
                .WithValueField(0, 32, writeCallback: _peripheralChannelsControl[20].WriteConfig,
                    valueProviderCallback: _peripheralChannelsControl[20].ReadConfig
                );

            _doubleWordRegisters.DefineRegister((long)Registers.PCHCTRL22)
                .WithValueField(0, 32, writeCallback: _peripheralChannelsControl[22].WriteConfig,
                    valueProviderCallback: _peripheralChannelsControl[22].ReadConfig
                );

            _doubleWordRegisters.DefineRegister((long)Registers.PCHCTRL25)
                .WithValueField(0, 32, writeCallback: _peripheralChannelsControl[25].WriteConfig,
                    valueProviderCallback: _peripheralChannelsControl[25].ReadConfig
                );
        }

        public event Action<SAML22GCLKClock> GCLKClockChanged;
        private readonly Machine _machine;
        private readonly Dictionary<int, Generator> _generators;
        private readonly Dictionary<int, PeripheralChannelControl> _peripheralChannelsControl;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;
        private readonly ISAML22OSCCTRL _oscctrl;
        private readonly ISAML22OSC32KCTRL _osc32kctrl;

        public long Size => 0x400;

        public long GCLK_MAIN => _generators[0].Frequency;
        public long GCLK_DFLL46M_REF => _peripheralChannelsControl[0].Frequency;
        public long GCLK_FDPLL => _peripheralChannelsControl[1].Frequency;
        public long GCLK_FDPLL_32K => _peripheralChannelsControl[2].Frequency;

        private Action<long> XOSCFreqChanged;
        private Action<long> GCLK_INFreqChanged;
        private Action<long> GCLK_GEN1FreqChanged;
        private Action<long> OSCULP32KFreqChanged;
        private Action<long> XOSC32KFreqChanged;
        private Action<long> OSC16MFreqChanged;
        private Action<long> DFLL48MFreqChanged;
        private Action<long> DFLL96MFreqChanged;


        private enum Registers
        {
            CTRLA = 0x00,
            SYNCBUSY = 0x04,
            GENCTRL0 = 0x20,
            GENCTRL1 = 0x24,
            GENCTRL2 = 0x28,
            GENCTRL3 = 0x2C,
            GENCTRL4 = 0x30,
            PCHCTRL0 = 0x80,
            PCHCTRL1 = 0x84,
            PCHCTRL2 = 0x88,
            PCHCTRL3 = 0x8C,
            PCHCTRL4 = 0x90,
            PCHCTRL5 = 0x94,
            PCHCTRL6 = 0x98,
            PCHCTRL7 = 0x9C,
            PCHCTRL8 = 0xA0,
            PCHCTRL9 = 0xA4,
            PCHCTRL10 = 0xA8,
            PCHCTRL11 = 0xAC,
            PCHCTRL12 = 0xB0,
            PCHCTRL13 = 0xB4,
            PCHCTRL14 = 0xB8,
            PCHCTRL15 = 0xBC,
            PCHCTRL16 = 0xC0,
            PCHCTRL17 = 0xC4,
            PCHCTRL18 = 0xC8,
            PCHCTRL19 = 0xCC,
            PCHCTRL20 = 0xD0,
            PCHCTRL21 = 0xD4,
            PCHCTRL22 = 0xD8,
            PCHCTRL23 = 0xDC,
            PCHCTRL24 = 0xE0,
            PCHCTRL25 = 0xE4,
            PCHCTRL26 = 0xE8,
            PCHCTRL27 = 0xEC,
            PCHCTRL28 = 0xF0,
        }

        private sealed class Generator
        {
            public Generator(Saml22GCLK gclk, int id, ClockSource source = ClockSource.XOSC, bool enabledByDefault = false)
            {
                _gclk = gclk;
                ID = id;
                _src = source;
                _gclk.RegisterClockChangeHandler(_src, ClockFreqChange);
                _enable = enabledByDefault;
                _defaultSource = source;
                _enabledByDefault = enabledByDefault;
            }

            private void ClockFreqChange(long frequency)
            {
                SourceFrequency = frequency;
            }

            public void Reset(bool soft = true)
            {
                _enable = _enabledByDefault;
                _src = _defaultSource;
                _improveDutyCycle = false;
                _outputOFFValue = false;
                _outputEnable = false;
                _divSelection = false;
                _runStandBy = false;
                _divisionFactor = 1;
            }

            private void UpdateFrequency()
            {
                if (_enable)
                {
                    if (_divSelection)
                    {
                        Frequency = (long)(SourceFrequency / Math.Pow(_divisionFactor, 2));
                    }
                    else
                    {
                        Frequency = SourceFrequency / _divisionFactor;
                    }
                }
                else
                {
                    Frequency = 0;
                }
            }

            public int ID { get; private set; }

            public long SourceFrequency
            {
                get => _gclk.GetFrequencyFromClock(_src);
                set
                {
                    _sourceFrequency = value;
                    UpdateFrequency();
                }
            }
            public long Frequency
            {
                get => _frequency;
                private set
                {
                    if (_frequency != value)
                    {
                        _frequency = value;
                        FrequencyChanged?.Invoke(_frequency);
                    }
                }
            }

            public ulong SRC
            {
                get => (ulong)_src;
                set
                {
                    if (value != (ulong)_src)
                    {
                        _gclk.UnregisterClockChangeHandler(_src, ClockFreqChange);
                        _gclk.RegisterClockChangeHandler(_src, ClockFreqChange);
                        _src = (ClockSource)value;
                        UpdateFrequency();
                    }

                }
            }

            public bool GENEN
            {
                get => _enable;
                set
                {
                    if (_enable != value)
                    {
                        _enable = value;
                        UpdateFrequency();
                    }
                }
            }

            public bool IDC
            {
                get => _improveDutyCycle;
                set => _improveDutyCycle = value;
            }


            // TODO: Will be implemented when PORT pads will have abillity to assign frequency and state by peripherals.
            public bool OOV
            {
                get => _outputOFFValue;
                set => _outputOFFValue = value;
            }
            // TODO: needs feature from PORT wich allow to assign frequency value to PAD.
            public bool OE
            {
                get => _outputEnable;
                set => _outputEnable = value;
            }

            public bool DIVSEL
            {
                get => _divSelection;
                set
                {
                    if (_divSelection != value)
                    {
                        _divSelection = value;
                        UpdateFrequency();
                    }
                }
            }

            public bool RUNSTDBY
            {
                get => _runStandBy;
                set => _runStandBy = value;
            }

            public long DIV
            {
                get => _divisionFactor;
                set
                {
                    if (_divisionFactor != value)
                    {
                        _divisionFactor = value;
                        UpdateFrequency();
                    }
                }
            }

            public event Action<long> FrequencyChanged;

            private readonly Saml22GCLK _gclk;
            private readonly ClockSource _defaultSource;
            private readonly bool _enabledByDefault;
            private ClockSource _src;
            private bool _enable;
            private bool _improveDutyCycle;
            private bool _outputOFFValue;
            private bool _outputEnable;
            private bool _divSelection;
            private bool _runStandBy;
            private long _divisionFactor = 1;
            private long _frequency;
            private long _sourceFrequency;

            public enum ClockSource
            {
                XOSC,
                GCLK_IN,
                GCLK_GEN1,
                OSCULP32K,
                XOSC32K,
                OSC16M,
                DFLL48M,
                DFLL96M
            }
        }

        private class PeripheralChannelControl
        {

            public int ID { get; private set; }
            public PeripheralChannelControl(Saml22GCLK gclk, int id)
            {
                _gclk = gclk;
                ID = id;
                _gclk._generators[0].FrequencyChanged += GenFrequencyChangeHandler;
            }

            public void WriteConfig(ulong old, ulong value)
            {
                WRTLOCK = BitHelper.IsBitSet(value, (byte)ControlBit.WRTLOCK);
                if (!WRTLOCK)
                {
                    GEN = (byte)((value & 0xF) >> (byte)ControlBit.GEN);
                    CHEN = BitHelper.IsBitSet(value, (byte)ControlBit.CHEN);
                }
            }

            public ulong ReadConfig(ulong _)
            {
                ulong reg = 0;
                BitHelper.SetBit(ref reg, (byte)ControlBit.WRTLOCK, WRTLOCK);
                reg |= (ulong)GEN << (byte)ControlBit.GEN;
                BitHelper.SetBit(ref reg, (byte)ControlBit.CHEN, CHEN);
                return reg;
            }

            public void Reset()
            {
                WRTLOCK = false;
                CHEN = false;
                GEN = 0x0;
            }
            public void SoftwareReset()
            {
                if (WRTLOCK)
                    return;
                Reset();
            }

            public void RegisterFreqChangeHandler(Action<long> handler)
            {
                UnregisterFreqChangeHandler(handler);
                _frequencyChanged += handler;
            }

            public void UnregisterFreqChangeHandler(Action<long> handler)
            {
                _frequencyChanged -= handler;
            }

            private void GenFrequencyChangeHandler(long frequency)
            {
                if (CHEN)
                    Frequency = frequency;
            }

            private byte GEN
            {
                get => _gen;
                set
                {
                    if (value != _gen)
                    {
                        _gen = value;
                        _gclk._generators[_gen].FrequencyChanged -= GenFrequencyChangeHandler;
                        if (CHEN)
                            Frequency = SourceFrequency;
                    }
                }
            }

            private byte _gen;
            private bool CHEN
            {
                get => _chen;
                set
                {
                    if (value != _chen)
                    {
                        _chen = value;
                        if (_chen)
                        {
                            Frequency = SourceFrequency;
                        }
                        else
                        {
                            Frequency = 0;
                        }
                    }
                }
            }
            private bool _chen;
            private bool WRTLOCK { get; set; }

            public long Frequency
            {
                get => _frequency;
                private set
                {
                    if (value != _frequency)
                    {
                        _frequency = value;
                        _frequencyChanged?.Invoke(_frequency);
                    }
                }
            }

            private long SourceFrequency => _gclk._generators[_gen].Frequency;

            private long _frequency;

            private Action<long> _frequencyChanged;

            private readonly Saml22GCLK _gclk;

            private enum ControlBit
            {
                GEN = 0,
                CHEN = 6,
                WRTLOCK = 7
            }
        }
    }
}
