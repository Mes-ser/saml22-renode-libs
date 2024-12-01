using System;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public interface ISAML22GCLK
    {
        long GCLK_MAIN { get; }
        long GCLK_DFLL46M_REF { get; }
        long GCLK_FDPLL { get; }
        long GCLK_FDPLL_32K { get; }

        event Action<SAML22GCLKClock> GCLKClockChanged;

        void RegisterPeripheralChannelFrequencyChange(ulong id, Action<long> handler);
    }

    public enum SAML22GCLKClock
    {
        GCLK_MAIN,
        GCLK_DFLL46M_REF,
        GCLK_FDPLL,
        GCLK_FDPLL_32K
    }
}
