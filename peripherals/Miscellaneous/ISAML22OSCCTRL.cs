using System;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public interface ISAML22OSCCTRL
    {
        long XOSC { get; set; }
        long OSC16M { get; }
        long DFLL48M { get; }
        long FDPLL96M { get; }

        event Action<SAML22OSCClock> OSCClockChanged;
    }

    public enum SAML22OSCClock
    {
        XOSC,
        OSC16M,
        DFLL48M,
        FDPLL96M
    }
}
