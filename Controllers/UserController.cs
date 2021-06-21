using System;
using System.Threading.Tasks;
using MangaAlert.Dtos;
using MangaAlert.Entities;
using MangaAlert.Repositories;
using MangaAlert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Internal.Account;
using Microsoft.AspNetCore.Mvc;

namespace MangaAlert.Controllers
{
  [ApiController]
  // [EnableCors]
  [Route("[controller]")]
  public class UserController: ControllerBase
  {
    private readonly IUserRepository _repository;
    private readonly IPasswordHash _hasher;

    public UserController(IUserRepository repository, IPasswordHash hasher)
    {
      this._repository = repository;
      this._hasher = hasher;
    }

    // POST /user
    [HttpPost]
    public async Task<ActionResult> CreateUser(CreateUserDto userDto)
    {
      var existingUser = await _repository.GetUserByEmail(userDto.Email);

      if (existingUser is not null) {
        return StatusCode(403, new {
         message = "Email address is already taken."
        });
      }

      User user = new() {
        Id = Guid.NewGuid(),
        Email = userDto.Email,
        Password = await _hasher.HashPassword(userDto.Password)
      };

      await _repository.CreateUser(user);

      return Ok(new {
        data = new {
          id = user.Id,
          email = user.Email,
        }
      });
    }


    // PUT /user/{userId}
    [Authorize]
    [HttpPut]
    public async Task<ActionResult> UpdateUser(Guid userId, CreateUserDto userDto)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to update this user."
        });
      }

      User user = new() {
        Email = userDto.Email,
        Password = userDto.Password
      };

      await _repository.UpdateUser(user);

      return Ok(new {
        data = new {
          id = user.Id,
          email = user.Email
        }
      });
    }

    // DELETE /user/{userId}
    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> DeleteUser(Guid userId)
    {
      if (User.Identity.Name != userId.ToString()) {
        return StatusCode(403, new {
          message = "User does not have the permission to delete this user."
        });
      }

      await _repository.DeleteUser(userId);

      return StatusCode(200,  new {
        data = "success"
      });
    }

    // POST /user/reset-password
    [HttpPost("reset-password")]
    public async Task<ActionResult> SetNewPassword(CreateUserDto userDto)
    {
      var existingUser = await _repository.GetUserByEmail(userDto.Email);

      if (string.IsNullOrEmpty(existingUser.Id.ToString())) {
        return StatusCode(404, new {
          message = "No user found for email."
        });
      }

      User user = (existingUser with {
        Password = await _hasher.HashPassword(userDto.Password),
      });

      await _repository.UpdateUser(user);

      return StatusCode(200,  new {
        data = "success"
      });
    }
  }
}
