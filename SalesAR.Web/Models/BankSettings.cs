namespace SalesAR.Web.Models;

using System.Collections.Generic;

public class BankInfo
{
    public string BankId { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Bin { get; set; } = string.Empty;
}

public class BankSettings
{
    public string BankId { get; set; } = "MB";
    public string BankName { get; set; } = "MB Bank (Ngân hàng TMCP Quân Đội)";
    public string AccountNumber { get; set; } = "0987654321";
    public string AccountName { get; set; } = "NGUYEN VAN A";
    public string Template { get; set; } = "compact2";

    public static readonly List<BankInfo> SupportedBanks = new()
    {
        new BankInfo { BankId = "MB", ShortName = "MB Bank", Name = "Ngân hàng TMCP Quân Đội (MBBank)", Bin = "970422" },
        new BankInfo { BankId = "VCB", ShortName = "Vietcombank", Name = "Ngân hàng TMCP Ngoại Thương Việt Nam (Vietcombank)", Bin = "970436" },
        new BankInfo { BankId = "TCB", ShortName = "Techcombank", Name = "Ngân hàng TMCP Kỹ Thương Việt Nam (Techcombank)", Bin = "970407" },
        new BankInfo { BankId = "BIDV", ShortName = "BIDV", Name = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)", Bin = "970418" },
        new BankInfo { BankId = "CTG", ShortName = "VietinBank", Name = "Ngân hàng TMCP Công Thương Việt Nam (VietinBank)", Bin = "970415" },
        new BankInfo { BankId = "ACB", ShortName = "ACB", Name = "Ngân hàng TMCP Á Châu (ACB)", Bin = "970416" },
        new BankInfo { BankId = "VPB", ShortName = "VPBank", Name = "Ngân hàng TMCP Việt Nam Thịnh Vượng (VPBank)", Bin = "970432" },
        new BankInfo { BankId = "TPB", ShortName = "TPBank", Name = "Ngân hàng TMCP Tiên Phong (TPBank)", Bin = "970423" },
        new BankInfo { BankId = "VBA", ShortName = "Agribank", Name = "Ngân hàng Nông nghiệp & PT Nông thôn VN (Agribank)", Bin = "970405" },
        new BankInfo { BankId = "STB", ShortName = "Sacombank", Name = "Ngân hàng TMCP Sài Gòn Thương Tín (Sacombank)", Bin = "970403" },
        new BankInfo { BankId = "HDB", ShortName = "HDBank", Name = "Ngân hàng TMCP Phát triển TP.HCM (HDBank)", Bin = "970437" },
        new BankInfo { BankId = "VIB", ShortName = "VIB", Name = "Ngân hàng TMCP Quốc tế Việt Nam (VIB)", Bin = "970441" },
        new BankInfo { BankId = "SHB", ShortName = "SHB", Name = "Ngân hàng TMCP Sài Gòn - Hà Nội (SHB)", Bin = "970443" },
        new BankInfo { BankId = "MSB", ShortName = "MSB", Name = "Ngân hàng TMCP Hàng Hải Việt Nam (MSB)", Bin = "970426" },
        new BankInfo { BankId = "OCB", ShortName = "OCB", Name = "Ngân hàng TMCP Phương Đông (OCB)", Bin = "970448" }
    };
}
