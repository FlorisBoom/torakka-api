using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MangaAlert.Settings;
using Microsoft.IdentityModel.Tokens;

namespace MangaAlert.Services
{
  public interface IJwtManagerService
  {
    IImmutableDictionary<string, RefreshToken> UsersRefreshTokensReadOnlyDictionary { get; }
    Task<JwtAuthResult> GenerateTokens(string username, Claim[] claims, DateTime now);
    Task<JwtAuthResult> Refresh(string refreshToken, string accessToken, DateTime now);
    void RemoveExpiredRefreshTokens(DateTime now);
    Task RemoveRefreshTokenByUserId(string userIdw);
    (ClaimsPrincipal, JwtSecurityToken) DecodeJwtToken(string token);
  }

  public class JwtManagerService : IJwtManagerService
  {
    public IImmutableDictionary<string, RefreshToken> UsersRefreshTokensReadOnlyDictionary =>
      _usersRefreshTokens.ToImmutableDictionary();

    private readonly ConcurrentDictionary<string, RefreshToken>
      _usersRefreshTokens; // can store in a database or a distributed cache

    private readonly IJwtSettings _jwtSettings;
    private readonly byte[] _secret;

    public JwtManagerService(IJwtSettings jwtSettings)
    {
      this._jwtSettings = jwtSettings;
      this._usersRefreshTokens = new ConcurrentDictionary<string, RefreshToken>();
      this._secret = Encoding.ASCII.GetBytes(jwtSettings.Secret);
    }

    public void RemoveExpiredRefreshTokens(DateTime now)
    {
      var expiredTokens = _usersRefreshTokens.Where(x => x.Value.ExpireAt < now).ToList();
      foreach (var expiredToken in expiredTokens) {
        _usersRefreshTokens.TryRemove(expiredToken.Key, out _);
      }
    }

    public async Task RemoveRefreshTokenByUserId(string userId)
    {
      var refreshTokens = _usersRefreshTokens.Where(x => x.Value.UserId == userId).ToList();
      foreach (var refreshToken in refreshTokens) {
        _usersRefreshTokens.TryRemove(refreshToken.Key, out _);
      }
    }

    public async Task<JwtAuthResult> GenerateTokens(string userId, Claim[] claims, DateTime now)
    {
      var shouldAddAudienceClaim =
        string.IsNullOrWhiteSpace(claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Aud)?.Value);
      var jwtToken = new JwtSecurityToken(
        _jwtSettings.Issuer,
        shouldAddAudienceClaim ? _jwtSettings.Audience : string.Empty,
        claims,
        expires: now.AddMinutes(_jwtSettings.AccessTokenExpiration),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(_secret),
          SecurityAlgorithms.HmacSha256Signature));
      var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

      var refreshToken = new RefreshToken {
        UserId = userId,
        TokenString = GenerateRefreshTokenString(),
        ExpireAt = now.AddMinutes(_jwtSettings.RefreshTokenExpiration)
      };
      _usersRefreshTokens.AddOrUpdate(refreshToken.TokenString, refreshToken, (_, _) => refreshToken);

      return new JwtAuthResult {
        AccessToken = accessToken,
        RefreshToken = refreshToken
      };
    }

    public async Task<JwtAuthResult> Refresh(string refreshToken, string accessToken, DateTime now)
    {
      var (principal, jwtToken) = DecodeJwtToken(accessToken);
      if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature)) {
        throw new SecurityTokenException("Invalid token");
      }

      var userId = principal.Identity?.Name;
      if (!_usersRefreshTokens.TryGetValue(refreshToken, out var existingRefreshToken)) {
        throw new SecurityTokenException("Invalid token");
      }

      if (existingRefreshToken.UserId != userId|| existingRefreshToken.ExpireAt < now) {
        throw new SecurityTokenException("Invalid token");
      }

      return await GenerateTokens(userId, principal.Claims.ToArray(), now); // need to recover the original claims
    }

    public (ClaimsPrincipal, JwtSecurityToken) DecodeJwtToken(string token)
    {
      if (string.IsNullOrWhiteSpace(token)) {
        throw new SecurityTokenException("Invalid token");
      }

      var principal = new JwtSecurityTokenHandler()
        .ValidateToken(token,
          new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_secret),
            ValidAudience = _jwtSettings.Audience,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
          },
          out var validatedToken);
      return (principal, validatedToken as JwtSecurityToken);
    }

    private static string GenerateRefreshTokenString()
    {
      var randomNumber = new byte[32];
      using var randomNumberGenerator = RandomNumberGenerator.Create();
      randomNumberGenerator.GetBytes(randomNumber);
      return Convert.ToBase64String(randomNumber);
    }
  }

  public class JwtAuthResult
  {
    public string AccessToken { get; set; }

    public RefreshToken RefreshToken { get; set; }
  }

  public class RefreshToken
  {
    public string UserId { get; set; }

    public string TokenString { get; set; }

    public DateTime ExpireAt { get; set; }
  }
}
