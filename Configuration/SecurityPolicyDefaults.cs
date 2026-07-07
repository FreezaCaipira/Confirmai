namespace Confirmai.Configuration;

public sealed record SecurityPolicySnapshot(
    bool RequireConfirmedEmail,
    int PasswordRequiredLength,
    bool PasswordRequireDigit,
    bool PasswordRequireLowercase,
    bool PasswordRequireUppercase,
    bool PasswordRequireNonAlphanumeric,
    int PasswordRequiredUniqueChars,
    int LockoutMaxFailedAccessAttempts,
    int LockoutMinutes,
    int SessionTimeoutMinutes);

public static class SecurityPolicyDefaults
{
    public static SecurityPolicySnapshot Create(bool isDevelopment)
    {
        if (isDevelopment)
        {
            return new SecurityPolicySnapshot(
                RequireConfirmedEmail: false,
                PasswordRequiredLength: 6,
                PasswordRequireDigit: true,
                PasswordRequireLowercase: true,
                PasswordRequireUppercase: false,
                PasswordRequireNonAlphanumeric: false,
                PasswordRequiredUniqueChars: 1,
                LockoutMaxFailedAccessAttempts: 5,
                LockoutMinutes: 15,
                SessionTimeoutMinutes: 720);
        }

        return new SecurityPolicySnapshot(
            RequireConfirmedEmail: true,
            PasswordRequiredLength: 10,
            PasswordRequireDigit: true,
            PasswordRequireLowercase: true,
            PasswordRequireUppercase: true,
            PasswordRequireNonAlphanumeric: true,
            PasswordRequiredUniqueChars: 3,
            LockoutMaxFailedAccessAttempts: 5,
            LockoutMinutes: 15,
            SessionTimeoutMinutes: 480);
    }
}


