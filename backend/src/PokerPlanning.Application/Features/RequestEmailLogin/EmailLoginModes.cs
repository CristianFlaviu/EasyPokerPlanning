using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.RequestEmailLogin;

internal static class EmailLoginModes
{
    public const string Login = "login";
    public const string SignUp = "signup";

    public static bool TryParse(string value, out EmailLoginMode mode)
    {
        if (string.Equals(value, Login, StringComparison.OrdinalIgnoreCase))
        {
            mode = EmailLoginMode.Login;
            return true;
        }

        if (string.Equals(value, SignUp, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "sign-up", StringComparison.OrdinalIgnoreCase))
        {
            mode = EmailLoginMode.SignUp;
            return true;
        }

        mode = EmailLoginMode.Login;
        return false;
    }
}
