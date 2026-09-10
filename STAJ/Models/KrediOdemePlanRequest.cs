namespace STAJ.Models;

public class KrediOdemePlanRequest
{
    public decimal KrediTutari { get; set; }
    public int Vade { get; set; }
    public decimal FaizOrani { get; set; }
    public decimal BsmvOrani { get; set; }
    public decimal KkdfOrani { get; set; }
    public int Periyot { get; set; } = 1;
    public string Tip { get; set; } = "EsitTaksitli";
}

public class KrediOdemeSatiri
{
    public int TaksitNo { get; set; }
    public decimal Anapara { get; set; }
    public decimal Faiz { get; set; }
    public decimal Bsmv { get; set; }
    public decimal Kkdf { get; set; }
    public decimal Taksit { get; set; }
    public decimal KalanAnapara { get; set; }
}

public class KrediOdemePlanResponse
{
    public string Tip { get; set; } = string.Empty;
    public decimal KrediTutari { get; set; }
    public int Vade { get; set; }
    public decimal FaizOrani { get; set; }
    public decimal BsmvOrani { get; set; }
    public decimal KkdfOrani { get; set; }
    public int Periyot { get; set; }
    public decimal ToplamAnapara { get; set; }
    public decimal ToplamFaiz { get; set; }
    public decimal ToplamBsmv { get; set; }
    public decimal ToplamKkdf { get; set; }
    public decimal ToplamOdeme { get; set; }
    public List<KrediOdemeSatiri> Plan { get; set; } = [];
}
