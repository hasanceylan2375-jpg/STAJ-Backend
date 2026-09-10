namespace STAJ.Models;

public class OtpRequest
{
    public string Hedef { get; set; } = string.Empty;
    public string Kanal { get; set; } = "Eposta";
    public string? KullaniciId { get; set; }
}

public class OtpDogrulaRequest
{
    public string Hedef { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
}
