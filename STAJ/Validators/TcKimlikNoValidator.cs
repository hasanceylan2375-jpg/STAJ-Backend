using FluentValidation;

namespace STAJ.Validators
{
    public class TcKimlikNoValidator : AbstractValidator<string?>
    {
        public TcKimlikNoValidator()
        {
            RuleFor(x => x)
                .NotEmpty().WithMessage("T.C. Kimlik No alanı zorunludur.")
                .Matches("^[0-9]{11}$").WithMessage("T.C. Kimlik No 11 haneli ve sadece rakamlardan oluşmalıdır.")
                .Must(x => string.IsNullOrEmpty(x) || x[0] != '0').WithMessage("T.C. Kimlik No'nun ilk hanesi 0 olamaz.")
                .Must(GecerliTcKimlikNo).WithMessage("Geçerli bir T.C. Kimlik No giriniz.");
        }

        private static bool GecerliTcKimlikNo(string? tc)
        {
            if (string.IsNullOrWhiteSpace(tc) || tc.Length != 11 || !tc.All(char.IsDigit) || tc[0] == '0')
                return false;

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
