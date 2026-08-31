using FluentValidation;
using STAJ.Entities;

namespace STAJ.Validators
{
    public class MusteriValidator : AbstractValidator<Musteri>
    {
        public MusteriValidator()
        {
            RuleFor(x => x.Ad).NotEmpty().WithMessage("Ad alanı boş bırakılamaz.").MinimumLength(2).MaximumLength(50);
            RuleFor(x => x.Soyad).NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.").MinimumLength(2).MaximumLength(50);
            RuleFor(x => x.Email).NotEmpty().WithMessage("E-posta alanı boş bırakılamaz.").EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.").MaximumLength(100);
            RuleFor(x => x.Telefon).NotEmpty().WithMessage("Telefon alanı boş bırakılamaz.").Matches("^[0-9]{10,11}$").WithMessage("Telefon 10 veya 11 rakamdan oluşmalıdır.");
            RuleFor(x => x.DogumTarihi).Must(t => !t.HasValue || t.Value.Date.AddYears(18) <= DateTime.UtcNow.Date).WithMessage("Müşteri en az 18 yaşında olmalıdır.");

            RuleFor(x => x.TcKimlikNo)
                .NotEmpty().WithMessage("T.C. Kimlik No alanı zorunludur.")
                .Matches("^[0-9]{11}$").WithMessage("T.C. Kimlik No 11 haneli ve sadece rakamlardan oluşmalıdır.")
                .Must(x => string.IsNullOrEmpty(x) || x[0] != '0').WithMessage("T.C. Kimlik No'nun ilk hanesi 0 olamaz.")
                .Must(GecerliTcKimlikNo).WithMessage("Geçerli bir T.C. Kimlik No giriniz.");
        }

        private static bool GecerliTcKimlikNo(string? tc)
        {
            if (string.IsNullOrWhiteSpace(tc) || tc.Length != 11 || !tc.All(char.IsDigit) || tc[0] == '0') return false;
            var rakamlar = tc.Select(c => c - '0').ToArray();
            var tekler = rakamlar[0] + rakamlar[2] + rakamlar[4] + rakamlar[6] + rakamlar[8];
            var ciftler = rakamlar[1] + rakamlar[3] + rakamlar[5] + rakamlar[7];
            var onuncu = ((tekler * 7) - ciftler) % 10;
            if (onuncu < 0) onuncu += 10;
            var onBirinci = rakamlar.Take(10).Sum() % 10;
            return rakamlar[9] == onuncu && rakamlar[10] == onBirinci;
        }
    }
}
