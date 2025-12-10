namespace PersonalMedia.Blazor.Services;

public interface IAuthenticationService
{
    bool IsAuthenticated { get; }
    bool SignIn(string code);
    void SignOut();
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public AuthenticationService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public bool IsAuthenticated
    {
        get
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            return session?.GetString("Authenticated") == "true";
        }
    }

    public bool SignIn(string code)
    {
        var validCode = _configuration["AppSettings:AccessCode"] ?? "1234";

        if (code == validCode)
        {
            _httpContextAccessor.HttpContext?.Session.SetString("Authenticated", "true");
            return true;
        }

        return false;
    }

    public void SignOut()
    {
        _httpContextAccessor.HttpContext?.Session.Clear();
    }
}
