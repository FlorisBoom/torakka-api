namespace MangaAlert.Settings
{
  public class JwtSettings: IJwtSettings
  {
    public string Secret { get; set; }

    public string Issuer { get; set; }

    public string Audience { get; set; }

    public int AccessTokenExpiration { get; set; }

    public int RefreshTokenExpiration { get; set; }

  }

  public interface IJwtSettings
  {
    string Secret { get; set; }

    string Issuer { get; set; }

    string Audience { get; set; }

    int AccessTokenExpiration { get; set; }

    int RefreshTokenExpiration { get; set; }
  }
}
