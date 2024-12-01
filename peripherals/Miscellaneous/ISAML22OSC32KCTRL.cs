using System;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public interface ISAML22OSC32KCTRL
    {
        long XOSC32K { get; set; }
        long OSCULP32K { get; }
        long XOSC32K_1K { get; }
        long OSCULP32K_1k { get; }

        event Action<SAML22OSC32KClock> OSC32KClockChanged;
    }

    public enum SAML22OSC32KClock
    {
        XOSC32K,
        OSCULP32K,
        XOSC32K_1K,
        OSCULP32K_1k
    }
}
