﻿

using System.Collections.Generic;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.GPIOPort
{
    public class Saml22PortGroup : IGPIOReceiver, INumberedGPIOOutput, IPeripheralRegister<IGPIOReceiver, NullRegistrationPoint>
    {

        public IReadOnlyDictionary<int, IGPIO> Connections => _connections;

        public void OnGPIO(int number, bool value)
        {
            this.DebugLog($"Signal on [{number}] value [{value}]");
            if (!_pads[number].Direction)
            {
                this.WarningLog($"Received signal on output pin [{number}].");
                return;
            }
            _pads[number].HandleInput(value);
        }

        public void Toggle(int number)
        {
            _pads[number].HandleInput(!_pads[number].Input);
        }

        public void Reset()
        {
            foreach (Pad pad in _pads)
            {
                pad.Reset();
            }
        }

        public Saml22PortGroup(Machine machine)
        {
            _machine = machine;
            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);

            // Connections = Enumerable.Range(0, NUMBER_OF_PINS).ToDictionary(i => i, _ => (IGPIO)new GPIO());

            _pads = new Pad[NUMBER_OF_PINS];
            for (int padID = 0; padID < NUMBER_OF_PINS; padID++)
            {
                _pads[padID] = new Pad();
                _connections.Add(padID, _pads[padID].PAD);
            }

            DefineRegisters();
        }


        private const int NUMBER_OF_PINS = 32;
        private readonly Machine _machine;
        public readonly DoubleWordRegisterCollection doubleWordRegisters;
        public readonly ByteRegisterCollection byteRegisters;
        private readonly Dictionary<int, IGPIO> _connections = new Dictionary<int, IGPIO>();
        private readonly Pad[] _pads;
        private readonly IValueRegisterField _pinMask;

        private void DefineRegisters()
        {
            doubleWordRegisters.DefineRegister((long)Registers.DataDirection)
                .WithFlags(0, 32,
                    writeCallback: (padID, oldValue, newValue) => _pads[padID].DirectionSet(newValue),
                    valueProviderCallback: (padID, _) => _pads[padID].Direction
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataDirectionClear)
               .WithFlags(0, 32,
                   writeCallback: (padID, oldValue, newValue) => { if (newValue) _pads[padID].DirectionClear(); },
                   valueProviderCallback: (padID, _) => _pads[padID].Direction
               );
            doubleWordRegisters.DefineRegister((long)Registers.DataDirectionSet)
                .WithFlags(0, 32,
                    writeCallback: (padID, oldValue, newValue) => { if (newValue) _pads[padID].DirectionSet(); },
                    valueProviderCallback: (padID, _) => _pads[padID].Direction
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataDirectiontoggle)
                .WithFlags(0, 32,
                    writeCallback: (padID, oldValue, newValue) => { if (newValue) _pads[padID].DirectionToggle(); },
                    valueProviderCallback: (padID, _) => _pads[padID].Direction
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataOutputValue)
                .WithFlags(0, 32,
                    writeCallback: (padID, oldValue, newValue) => _pads[padID].OutputSet(newValue),
                    valueProviderCallback: (padID, _) => _pads[padID].Output
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataOutputValueClear)
                .WithFlags(0, 32,
                    writeCallback: (padID, oldValue, newValue) => { if (newValue) _pads[padID].OutputClear(); },
                    valueProviderCallback: (padID, _) => _pads[padID].Output
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataOutputValueSet)
                .WithFlags(0, 32,
                    writeCallback: (padID, oldValue, newValue) => { if (newValue) _pads[padID].OutputSet(); },
                    valueProviderCallback: (padID, _) => _pads[padID].Output
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataOutputValueToggle)
                .WithFlags(0, 32,
                    writeCallback: (padID, oldValue, newValue) =>
                    {
                        if (newValue) _pads[padID].OutputToggle();
                        if (newValue) this.DebugLog($"Pad [{padID}] toggled.");
                    },
                    valueProviderCallback: (padID, _) => _pads[padID].Output
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataInputValue)
                .WithFlags(0, 32, FieldMode.Read, valueProviderCallback: (padID, _) => _pads[padID].Input);
            doubleWordRegisters.DefineRegister((long)Registers.Control);

            doubleWordRegisters.DefineRegister((long)Registers.WriteConfiguration)
                .WithValueField(0, 16, FieldMode.Write, name: "PINMASK")
                .WithFlag(16, FieldMode.Write, name: "PMUXEN")
                .WithFlag(17, FieldMode.Write, name: "INEN")
                .WithFlag(18, FieldMode.Write, name: "PULLEN")
                .WithFlag(22, FieldMode.Write, name: "DRVSTR")
                .WithValueField(24, 4, FieldMode.Write, name: "PMUX")
                .WithFlag(28, FieldMode.Write, name: "WRPMUX")
                .WithFlag(30, FieldMode.Write, name: "WRPINCFG")
                .WithFlag(31, FieldMode.Write, name: "HWSEL");
            doubleWordRegisters.AddAfterWriteHook((long)Registers.WriteConfiguration, (offset, value) =>
            {
                uint pinMask = BitHelper.GetValue(value, 0, 16);
                bool pmuxen = BitHelper.IsBitSet(value, 16);
                bool inen = BitHelper.IsBitSet(value, 17);
                bool pullen = BitHelper.IsBitSet(value, 18);
                bool drvstr = BitHelper.IsBitSet(value, 2);
                uint pmux = BitHelper.GetValue(value, 24, 4);
                bool wrpmux = BitHelper.IsBitSet(value, 28);
                bool wrpinconf = BitHelper.IsBitSet(value, 30);
                uint hwsel = BitHelper.GetValue(value, 31, 1);

                if (wrpinconf)
                {
                    BitHelper.ForeachActiveBit(pinMask, pinID =>
                    {
                        _pads[pinID + 16 * hwsel].PeripheralMultiplexerEnable = pmuxen;
                        _pads[pinID + 16 * hwsel].InputEnable = inen;
                        _pads[pinID + 16 * hwsel].OutputDriverStrength = drvstr;
                    });
                }
                if (wrpmux)
                {
                    BitHelper.ForeachActiveBit(pinMask, pinID =>
                    {
                        _pads[pinID + 16 * hwsel].PeripheralMultiplexer = pmux;
                    });
                }
            });

            doubleWordRegisters.DefineRegister((long)Registers.EventInputControl);

            for (int index = 0; index < NUMBER_OF_PINS / 2; index++)
            {
                Pad padEven = _pads[2 * index];
                Pad padOdd = _pads[2 * index + 1];
                byteRegisters.DefineRegister((long)Registers.PeripheralMultiplexingX + index)
                    .WithValueField(0, 4,
                        writeCallback: (oldValue, newValue) => padEven.PeripheralMultiplexer = newValue,
                        valueProviderCallback: (_) => padEven.PeripheralMultiplexer
                    )
                    .WithValueField(4, 4,
                        writeCallback: (oldValue, newValue) => padOdd.PeripheralMultiplexer = newValue,
                        valueProviderCallback: (_) => padOdd.PeripheralMultiplexer
                    );
                // byteRegisters.AddAfterWriteHook((long)Registers.PeripheralMultiplexingX + index, (_, value) => {
                //     this.InfoLog($"E - 0x{padEven.PeripheralMultiplexer:x}| O - 0x{padOdd.PeripheralMultiplexer:x}");
                // });
            }

            for (int index = 0; index < NUMBER_OF_PINS; index++)
            {
                Pad pad = _pads[index];
                byteRegisters.DefineRegister((long)Registers.PinConfigurationN + index)
                    .WithFlag(0, name: "PMUXEN",
                        writeCallback: (oldValue, newValue) => pad.PeripheralMultiplexerEnable = newValue,
                        valueProviderCallback: (_) => pad.PeripheralMultiplexerEnable
                    )
                    .WithFlag(1, name: "INEN",
                        writeCallback: (oldValue, newValue) => pad.InputEnable = newValue,
                        valueProviderCallback: (_) => pad.InputEnable
                    )
                    .WithFlag(2, name: "PULLEN",
                        writeCallback: (oldValue, newValue) => pad.PullEnable = newValue,
                        valueProviderCallback: (_) => pad.PullEnable
                    )
                    .WithFlag(6, name: "DRVSTR",
                        writeCallback: (oldValue, newValue) => pad.OutputDriverStrength = newValue,
                        valueProviderCallback: (_) => pad.OutputDriverStrength
                    );
                // byteRegisters.AddAfterWriteHook((long)Registers.PinConfigurationN + index, (_, value) => {
                //     this.InfoLog($"{pad.PeripheralMultiplexerEnable}|{pad.InputEnable}|{pad.PullEnable}|{pad.OutputDriverStrength}|");
                // });
            }
        }

        public void Register(IGPIOReceiver peripheral, NullRegistrationPoint registrationPoint)
        {
            _machine.RegisterAsAChildOf(this, peripheral, registrationPoint);
        }

        public void Unregister(IGPIOReceiver peripheral)
        {
            _machine.UnregisterAsAChildOf(this, peripheral);
        }


        private class Pad
        {
            public bool Direction { get; private set; }
            public bool Output { get => PAD.IsSet; }
            public bool Input { get => PAD.IsSet; }

            public GPIO PAD { get; }

            public bool PeripheralMultiplexerEnable;
            public bool InputEnable;
            public bool PullEnable;
            public bool OutputDriverStrength;
            public ulong PeripheralMultiplexer;

            public void DirectionClear() => Direction = false;
            public void DirectionSet(bool dir = true) => Direction = dir;
            public void DirectionToggle() => Direction = !Direction;

            public void OutputClear() => PAD.Set(false);
            public void OutputSet(bool output = true) => PAD.Set(output);
            public void OutputToggle() => PAD.Set(!PAD.IsSet);

            public void HandleInput(bool level) => PAD.Set(level);

            public void Reset()
            {
                Direction = false;
                PAD.Set(false);
                PeripheralMultiplexerEnable = false;
                InputEnable = false;
                PullEnable = false;
                OutputDriverStrength = false;
            }

            public Pad()
            {
                PAD = new GPIO();
            }
        }

        private enum Registers : long
        {
            DataDirection = 0x00,
            DataDirectionClear = 0x04,
            DataDirectionSet = 0x08,
            DataDirectiontoggle = 0x0C,
            DataOutputValue = 0x10,
            DataOutputValueClear = 0x14,
            DataOutputValueSet = 0x18,
            DataOutputValueToggle = 0x1C,
            DataInputValue = 0x20,
            Control = 0x24,
            WriteConfiguration = 0x28,
            EventInputControl = 0x2C,
            PeripheralMultiplexingX = 0x30,
            PinConfigurationN = 0x40
        }

    }
}
