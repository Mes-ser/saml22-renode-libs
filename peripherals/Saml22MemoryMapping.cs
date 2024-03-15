

namespace Antmicro.Renode.Peripherals
{
    public enum Saml22MemoryMappin
    {
        FlashBaseAddress = 0x0,
        SRAMBaseAddress = 0x2000000,
        // Bridge A
        PACBaseAddress = 0x4000000,
        PMBaseAddress = 0x40000400,
        MCLKBaseAddress = 0x40000800,
        RSTCBaseAddress = 0x40000C00,
        OSCCTRLBaseAddress = 0x40001000,
        OSC32KCTRLBaseAddress = 0x40001400,
        SUPCBaseAddress = 0x40001800,
        GCLKBaseAddress = 0x40001C00,
        WDTBaseAddress = 0x40002000,
        RTCBaseAddress = 0x40002400,
        EICBaseAddress = 0x40002800,
        FREQMBaseAddress = 0x40002C00,
        // Bridge B TODO: Check correct address mapping, as manual states weird addresses
        USBBaseAddress = 0x41000000,
        DSUBaseAddress = 0x41002000,
        NVMCTRLBaseAddress = 0x41004000,
        PORTBaseAddress = 0x41006000,
        DMACBaseAddress = 0x41008000,
        MTBBaseAddress = 0x4100A000,
        HMATRIXHSBaseAddress = 0x4100C000,
        // Bridge C
        EVSYSBaseAddress = 0x42000000,
        SERCOM0BaseAddress = 0x42000400,
        SERCOM1BaseAddress = 0x42000800,
        SERCOM2BaseAddress = 0x42000C00,
        SERCOM3BaseAddress = 0x42001000,
        SERCOM4BaseAddress = 0x42001400,
        SERCOM5BaseAddress = 0x42001800,
        TCC0BaseAddress = 0x42001C00,
        TC0BaseAddress = 0x42002000,
        TC1BaseAddress = 0x42002400,
        TC2BaseAddress = 0x42002800,
        TC3BaseAddress = 0x42002C00,
        ADCBaseAddress = 0x42003000,
        PTCBaseAddress = 0x42003800,
        SLCDBaseAddress = 0x42004000,
        AESBaseAddress = 0x42004000,
        TRNGBaseAddress = 0x42004400,
        CCLBaseAddress = 0x42004400
    }
}