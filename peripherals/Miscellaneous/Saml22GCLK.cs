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

        public Saml22GCLK(Machine machine, ISAML22OSCCTRL oscctrl)
        {
            _machine = machine;
            _oscctrl = oscctrl;

            _oscctrl.OSCClockChanged += ClockChanged;

            _doubleWordRegisters = new DoubleWordRegisterCollection(this);
            _byteRegisters = new ByteRegisterCollection(this);

            _generators = new Dictionary<int, Generator>
            {
                { 0, new Generator(this, 0, Generator.ClockSource.OSC16M, true) }
            };
            _generators[0].FrequencyChanged += GeneratorFrequencyChanged;
            for (int i = 1; i < 5; i++)
            {
                _generators.Add(i, new Generator(this, i));
                _generators[i].FrequencyChanged += GeneratorFrequencyChanged;
            }

            DefineRegisters();
        }

        // Assume this is POWER Reset
        public void Reset()
        {
            _doubleWordRegisters.Reset();
            _byteRegisters.Reset();
        }

        public uint ReadDoubleWord(long offset) => _doubleWordRegisters.Read(offset);
        public void WriteDoubleWord(long offset, uint value) => _doubleWordRegisters.Write(offset, value);
        public byte ReadByte(long offset) => _byteRegisters.Read(offset);
        public void WriteByte(long offset, byte value) => _byteRegisters.Write(offset, value);

        private void GeneratorFrequencyChanged(int id)
        {
            switch (id)
            {
                case 0:
                    GCLKClockChanged?.Invoke(SAML22GCLKClock.GCLK_MAIN);
                    break;
                default:
                    this.WarningLog($"Unhandled GEN[{id}] change.");
                    break;
            }
        }

        private void ClockChanged(SAML22OSCClock clock)
        {
            switch (clock)
            {
                case SAML22OSCClock.OSC16M:
                    foreach (Generator generator in _generators.Values)
                    {
                        if ((Generator.ClockSource)generator.SRC == Generator.ClockSource.OSC16M)
                        {
                            generator.SourceFrequency = _oscctrl.OSC16M;
                        }
                    }
                    break;
            }
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
        }

        public event Action<SAML22GCLKClock> GCLKClockChanged;
        private readonly Machine _machine;
        private readonly Dictionary<int, Generator> _generators;
        private readonly DoubleWordRegisterCollection _doubleWordRegisters;
        private readonly ByteRegisterCollection _byteRegisters;
        private readonly ISAML22OSCCTRL _oscctrl;

        public long Size => 0x400;

        public long GCLK_MAIN => _generators[0].Frequency;
        public long GCLK_DFLL46M_REF => throw new NotImplementedException();
        public long GCLK_FDPLL => throw new NotImplementedException();
        public long GCLK_FDPLL_32K => throw new NotImplementedException();

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
                _enable = enabledByDefault;
                _defaultSource = source;
                _enabledByDefault = enabledByDefault;
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
                        Frequency = (long)(_sourceFrequency / Math.Pow(_divisionFactor, 2));
                    }
                    else
                    {
                        Frequency = _sourceFrequency / _divisionFactor;
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
                set
                {
                    _gclk.DebugLog($"Generator [{ID}] source frequency changed [{value}Hz]");
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
                        _gclk.DebugLog($"Generator [{ID}] frequency changed [{value}Hz]");
                        _frequency = value;
                        FrequencyChanged?.Invoke(ID);
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

            public event Action<int> FrequencyChanged;

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

        private class ChannelControl
        {
            public ChannelControl()
            {

            }
        }
    }
}
