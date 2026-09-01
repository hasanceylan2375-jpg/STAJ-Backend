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
                .SetValidator(new TcKimlikNoValidator());
        }
    }
}
