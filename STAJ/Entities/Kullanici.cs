namespace STAJ.Entities
{
    public class Kullanici
    {
        public int Id { get; set; }

        public string KullaniciAdi { get; set; } = string.Empty;

        public string Sifre { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
