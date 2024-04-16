

using System.Collections.Generic;
using System.Linq;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.PlatformDescription.Syntax;
using Antmicro.Renode.Utilities;
using Dynamitey.DynamicObjects;

namespace Antmicro.Renode.Peripherals.GPIOPort
{
    public class Saml22PortGroup : IGPIOReceiver, INumberedGPIOOutput
    {

        public IReadOnlyDictionary<int, IGPIO> Connections { get; } // Pads

        public void OnGPIO(int number, bool value)
        {
            this.DebugLog($"Signal on [{number}] value [{value}]");
            if(!pads[number].Direction)
            {
                this.WarningLog($"Received signal on output pin [{number}].");
                return;
            }
            pads[number].HandleInput(value);
        }

        public void Toggle(int number)
        {
            pads[number].HandleInput(!pads[number].Input);
        }

        public void Reset()
        {
            foreach (Pad pad in pads)
            {
                pad.Reset();
            }
        }

        public Saml22PortGroup(Machine machine)
        {
            doubleWordRegisters = new DoubleWordRegisterCollection(this);
            byteRegisters = new ByteRegisterCollection(this);

            Connections = Enumerable.Range(0, NUMBER_OF_PINS).ToDictionary(i => i, _ => (IGPIO)new GPIO());

            pads = new Pad[NUMBER_OF_PINS];
            for(int padID = 0; padID < NUMBER_OF_PINS; padID++)
                pads[padID] = new Pad(Connections[padID]);

            DefineRegisters();
        }

        private const int NUMBER_OF_PINS = 32;

        public readonly DoubleWordRegisterCollection doubleWordRegisters;
        public readonly ByteRegisterCollection byteRegisters;
        private readonly Dictionary<int, IGPIO> connections;
        private readonly Pad[] pads;
        private IValueRegisterField pinMask;

        private void DefineRegisters()
        {
            doubleWordRegisters.DefineRegister((long)Registers.DataDirection)
                .WithFlags(0, 32,
                    writeCallback:(padID, oldValue, newValue) => pads[padID].DirectionSet(newValue),
                    valueProviderCallback: (padID, _) => pads[padID].Direction
                );
             doubleWordRegisters.DefineRegister((long)Registers.DataDirectionClear)
                .WithFlags(0, 32,
                    writeCallback:(padID, oldValue, newValue) =>{ if(newValue) pads[padID].DirectionClear();},
                    valueProviderCallback: (padID, _) => pads[padID].Direction
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataDirectionSet)
                .WithFlags(0, 32,
                    writeCallback:(padID, oldValue, newValue) =>{ if(newValue) pads[padID].DirectionSet();},
                    valueProviderCallback: (padID, _) => pads[padID].Direction
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataDirectiontoggle)
                .WithFlags(0, 32,
                    writeCallback:(padID, oldValue, newValue) =>{ if(newValue) pads[padID].DirectionToggle();},
                    valueProviderCallback: (padID, _) => pads[padID].Direction
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataOutputValue)
                .WithFlags(0, 32,
                    writeCallback:(padID, oldValue, newValue) => pads[padID].OutputSet(newValue),
                    valueProviderCallback: (padID, _) => pads[padID].Output
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataOutputValueClear)
                .WithFlags(0, 32,
                    writeCallback:(padID, oldValue, newValue) =>{ if(newValue) pads[padID].OutputClear();},
                    valueProviderCallback: (padID, _) => pads[padID].Output
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataOutputValueSet)
                .WithFlags(0, 32,
                    writeCallback:(padID, oldValue, newValue) =>{ if(newValue) pads[padID].OutputSet();},
                    valueProviderCallback: (padID, _) => pads[padID].Output
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataOutputValueToggle)
                .WithFlags(0, 32,
                    writeCallback:(padID, oldValue, newValue) =>{ if(newValue) pads[padID].OutputToggle();},
                    valueProviderCallback: (padID, _) => pads[padID].Output
                );
            doubleWordRegisters.DefineRegister((long)Registers.DataInputValue)
                .WithFlags(0, 32, FieldMode.Read, valueProviderCallback: (padID, _) => pads[padID].Input);
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
            doubleWordRegisters.AddAfterWriteHook((long)Registers.WriteConfiguration, (offset, value) => {
                uint pinMask = BitHelper.GetValue(value, 0, 16);
                bool pmuxen = BitHelper.IsBitSet(value, 16);
                bool inen = BitHelper.IsBitSet(value, 17);
                bool pullen = BitHelper.IsBitSet(value, 18);
                bool drvstr = BitHelper.IsBitSet(value, 2);
                uint pmux = BitHelper.GetValue(value, 24, 4);
                bool wrpmux = BitHelper.IsBitSet(value, 28);
                bool wrpinconf = BitHelper.IsBitSet(value, 30);
                uint hwsel = BitHelper.GetValue(value, 31, 1);

                if(wrpinconf){
                    BitHelper.ForeachActiveBit(pinMask, pinID => {
                        pads[pinID + 16 * hwsel].PeripheralMultiplexerEnable = pmuxen;
                        pads[pinID + 16 * hwsel].InputEnable = inen;
                        pads[pinID + 16 * hwsel].OutputDriverStrength = drvstr;
                    });
                }
                if(wrpmux){
                    BitHelper.ForeachActiveBit(pinMask, pinID => {
                        pads[pinID + 16 * hwsel].PeripheralMultiplexer = pmux;
                    });
                }
            });

            doubleWordRegisters.DefineRegister((long)Registers.EventInputControl);

            for(int index = 0; index < NUMBER_OF_PINS / 2; index++)
            {
                Pad padEven = pads[2 * index];
                Pad padOdd = pads[2 * index + 1];
                byteRegisters.DefineRegister((long)Registers.PeripheralMultiplexingX + index)
                    .WithValueField(0, 4,
                        writeCallback: (oldValue, newValue) => padEven.PeripheralMultiplexer = newValue,
                        valueProviderCallback: (_) => padEven.PeripheralMultiplexer
                    )
                    .WithValueField(4, 4,
                        writeCallback: (oldValue, newValue) => padOdd.PeripheralMultiplexer = newValue,
                        valueProviderCallback: (_) => padOdd.PeripheralMultiplexer
                    );
            }

            for(int index = 0; index < NUMBER_OF_PINS; index++)
            {
                Pad pad = pads[index];
                byteRegisters.DefineRegister((long)Registers.PinConfigurationN + index)
                    .WithFlag(0, name: "PMUXEN",
                        writeCallback: (oldValue, newValue) => pad.PeripheralMultiplexerEnable = newValue,
                        valueProviderCallback: (_) => pad.PeripheralMultiplexerEnable
                    )
                    .WithFlag(1, name: "PMUXEN",
                        writeCallback: (oldValue, newValue) => pad.InputEnable = newValue,
                        valueProviderCallback: (_) => pad.InputEnable
                    )
                    .WithFlag(2, name: "PMUXEN",
                        writeCallback: (oldValue, newValue) => pad.PullEnable = newValue,
                        valueProviderCallback: (_) => pad.PullEnable
                    )
                    .WithFlag(6, name: "PMUXEN",
                        writeCallback: (oldValue, newValue) => pad.OutputDriverStrength = newValue,
                        valueProviderCallback: (_) => pad.OutputDriverStrength
                    );
            }
        }


        private class Pad
        {
            public bool Direction { get; private set; }
            public bool Output { get; private set; }
            public bool Input { get => pad.IsSet; }

            public bool PeripheralMultiplexerEnable;
            public bool InputEnable;
            public bool PullEnable;
            public bool OutputDriverStrength;
            public ulong PeripheralMultiplexer;

            public void DirectionClear() => Direction = false;
            public void DirectionSet(bool dir = true) =>  Direction = dir;
            public void DirectionToggle() => Direction = !Direction;

            public void OutputClear() => Output = false;
            public void OutputSet(bool output = true) =>  Output = output;
            public void OutputToggle() => Output = !Output;

            public void HandleInput(bool level) => pad.Set(level);

            public void Reset()
            {
                Direction = false;
                Output = false;
                pad.Set(false);
                PeripheralMultiplexerEnable = false;
                InputEnable = false;
                PullEnable = false;
                OutputDriverStrength = false;
            }

            public Pad(IGPIO pad)
            {
                this.pad = pad;
            }

            private readonly IGPIO pad;
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
