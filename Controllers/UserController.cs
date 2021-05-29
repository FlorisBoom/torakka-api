using System;
using System.Threading.Tasks;
using MangaAlert.Dtos;
using MangaAlert.Entities;
using MangaAlert.Repositories;
using MangaAlert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaAlert.Controllers
{
  [ApiController]
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

    // POST /users
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
        UserName = userDto.UserName,
        Email = userDto.Email,
        Password = await _hasher.HashPassword(userDto.Password)
      };

      await _repository.CreateUser(user);

      return Ok(new {
        data = new {
          id = user.Id,
          userName = user.UserName,
          email = user.Email,
        }
      });
    }


    // PUT /users/{userId}
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
        UserName = userDto.UserName,
        Email = userDto.Email,
        Password = userDto.Password
      };

      await _repository.UpdateUser(user);

      return Ok(new {
        data = new {
          id = user.Id,
          userName = user.UserName,
          email = user.Email
        }
      });
    }

    // DELETE /users/{userId}
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
  }
}
