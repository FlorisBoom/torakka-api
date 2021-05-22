using System;
using System.Threading.Tasks;
using MangaAlert.Dtos;
using MangaAlert.Entities;
using MangaAlert.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace MangaAlert.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class UserController: ControllerBase
  {
    private readonly IUserRepository _repository;

    public UserController(IUserRepository repository)
    {
      this._repository = repository;
    }

    // POST /Users
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto userDto)
    {
      User user = new() {
        Id = Guid.NewGuid(),
        UserName = userDto.UserName,
        Email = userDto.Email,
        Password = userDto.Password
      };

      await _repository.CreateUser(user);

      return Ok(new {
        data = user
      });
    }
  }
}
