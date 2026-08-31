using FluentValidation;
using STAJ.Entities;

namespace STAJ.Validators
{
    public class MusteriValidator : AbstractValidator<Musteri>
    {
        public MusteriValidator()
        {
            RuleFor(x => x.Ad)
                .NotEmpty().WithMessage("Ad alanı boş bırakılamaz.")
                .MinimumLength(2).WithMessage("Ad en az 2 karakter olmalıdır.")
                .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir.");

            RuleFor(x => x.Soyad)
                .NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.")
                .MinimumLength(2).WithMessage("Soyad en az 2 karakter olmalıdır.")
                .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta alanı boş bırakılamaz.")
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
                .MaximumLength(100).WithMessage("E-posta en fazla 100 karakter olabilir.");

            RuleFor(x => x.Telefon)
                .NotEmpty().WithMessage("Telefon alanı boş bırakılamaz.")
                .Matches("^[0-9]{10,11}$").WithMessage("Telefon 10 veya 11 rakamdan oluşmalıdır.");

            RuleFor(x => x.DogumTarihi)
                .Must(dogumTarihi => !dogumTarihi.HasValue || dogumTarihi.Value.Date.AddYears(18) <= DateTime.UtcNow.Date)
                .WithMessage("Müşteri en az 18 yaşında olmalıdır.");
        }
    }
}
