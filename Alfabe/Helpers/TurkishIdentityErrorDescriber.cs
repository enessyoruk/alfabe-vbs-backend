using Microsoft.AspNetCore.Identity;

public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length)
        => new IdentityError { Code = nameof(PasswordTooShort), Description = $"Şifre en az {length} karakter olmalıdır." };

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Şifre en az bir özel karakter içermelidir." };

    public override IdentityError PasswordRequiresLower()
        => new IdentityError { Code = nameof(PasswordRequiresLower), Description = "Şifre en az bir küçük harf içermelidir." };

    public override IdentityError PasswordRequiresUpper()
        => new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "Şifre en az bir büyük harf içermelidir." };

    public override IdentityError DuplicateEmail(string email)
        => new IdentityError { Code = nameof(DuplicateEmail), Description = $"{email} adresi zaten kayıtlı." };

    public override IdentityError DuplicateUserName(string userName)
        => new IdentityError { Code = nameof(DuplicateUserName), Description = $"{userName} kullanıcı adı zaten kullanılmaktadır." };
}
