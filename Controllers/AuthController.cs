using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MangaAlert.Dtos;
using MangaAlert.Repositories;
using MangaAlert.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace MangaAlert.Controllers
{
  [ApiController]
  [EnableCors]
  [Produces("application/json")]
  [Route("[controller]")]
  public class AuthController: ControllerBase
  {
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHash _passwordHashService;
    private readonly IJwtManagerService _jwtManagerService;

    public AuthController(IUserRepository userRepository, IPasswordHash passwordHash, IJwtManagerService jwtManagerService)
    {
      this._userRepository = userRepository;
      this._passwordHashService = passwordHash;
      this._jwtManagerService = jwtManagerService;
    }

    // POST /auth
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> AuthenticateUser(AuthDto authDto)
    {
      var user = await _userRepository.GetUserByEmail(authDto.Email);

      if (user is null) {
        return NotFound(new {
          message = "User not found."
        });
      }

      if (!await _passwordHashService.IsCorrectPassword(user.Id, authDto.Password)) {
        return StatusCode(401, new {
          message = "Password not recognized."
        });
      }

      // Generate jwt access and refresh tokens and return it
      var claims = new[] {
        new Claim(ClaimTypes.Name, user.Id.ToString())
      };

      var jwtResult = await _jwtManagerService.GenerateTokens(user.Id.ToString(), claims, DateTime.Now);
      return Ok(new {
        data = new {
          accessToken = jwtResult.AccessToken,
          refreshToken = jwtResult.RefreshToken
        }
      });
    }

    // POST /auth/logout
    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult> LogUserOut()
    {
      var userId = User.Identity.Name;
      await _jwtManagerService.RemoveRefreshTokenByUserId(userId);
      return Ok(new {
        data = "success"
      });
    }

    // POST /auth/refresh-token
    [HttpPost("refresh-token")]
    public async Task<ActionResult> RefreshToken(RefreshTokenRequestDto refreshTokenRequestDto)
    {
      if (string.IsNullOrWhiteSpace(refreshTokenRequestDto.RefreshToken))
      {
        return Unauthorized();
      }

      var accessToken = await HttpContext.GetTokenAsync("Bearer", "access_token");
      var jwtResult = await _jwtManagerService.Refresh(refreshTokenRequestDto.RefreshToken, accessToken, DateTime.Now);

      return Ok(new {
        data = new {
          accessToken = jwtResult.AccessToken,
          refreshToken = jwtResult.RefreshToken.TokenString
        }
      });
    }
  }
}
