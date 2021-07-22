using Microsoft.AspNetCore.Identity;

namespace Sitko.Core.Identity
{
    public class RussianIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError ConcurrencyFailure() =>
            new IdentityError
            {
                Code = "ConcurrencyFailure",
                Description = "Сбой в параллельных запросах. Возможно объект был изменен."
            };

        public override IdentityError DefaultError() =>
            new IdentityError
            {
                Code = "DefaultError", Description = "Произошла неизвестная ошибка при авторизации."
            };

        public override IdentityError DuplicateEmail(string email) => new IdentityError {Code = "DuplicateEmail", Description = $"E-mail '{email}' уже используется."};

        public override IdentityError DuplicateRoleName(string role) =>
            new IdentityError
            {
                Code = "DuplicateRoleName", Description = $"Роль с именем '{role}' уже существует."
            };

        public override IdentityError DuplicateUserName(string userName) =>
            new IdentityError
            {
                Code = "DuplicateUserName", Description = $"Пользователь '{userName}' уже зарегистрирован."
            };

        public override IdentityError InvalidEmail(string email) =>
            new IdentityError
            {
                Code = "InvalidEmail", Description = $"E-mail '{email}' содержит неверный формат."
            };

        public override IdentityError InvalidRoleName(string role) =>
            new IdentityError
            {
                Code = "InvalidRoleName",
                Description = $"Имя роли '{role}' задано не верно (содержит не допустимые символы либо длину)."
            };

        public override IdentityError InvalidToken() =>
            new IdentityError
            {
                Code = "InvalidToken", Description = "Неправильно указан код подтверждения (token)."
            };

        public override IdentityError InvalidUserName(string userName) =>
            new IdentityError
            {
                Code = "InvalidUserName",
                Description =
                    $"Имя пользователя '{userName}' указано не верно (содержит не допустимые символы либо длину)."
            };

        public override IdentityError LoginAlreadyAssociated() =>
            new IdentityError
            {
                Code = "LoginAlreadyAssociated", Description = "Данный пользователь уже привязан к аккаунту."
            };

        public override IdentityError PasswordMismatch() => new IdentityError {Code = "PasswordMismatch", Description = "Пароли не совпадают."};

        public override IdentityError PasswordRequiresDigit() =>
            new IdentityError
            {
                Code = "PasswordRequiresDigit", Description = "Пароль должен содержать минимум одну цифру."
            };

        public override IdentityError PasswordRequiresLower() =>
            new IdentityError
            {
                Code = "PasswordRequiresLower", Description = "Пароль должен содержать минимум одну строчную букву."
            };

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            new IdentityError
            {
                Code = "PasswordRequiresNonAlphanumeric",
                Description = "Пароль должен содержать минимум один специальный символ (не буквенно-цифровой)."
            };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
            new IdentityError
            {
                Code = "PasswordRequiresUniqueChars",
                Description = $"Пароль должен содержать минимум '{uniqueChars}' не повторяющихся символов."
            };
    }
}
