using System;
using System.Threading.Tasks;
using MangaAlert.Entities;

namespace MangaAlert.Repositories
{
  public interface IUserRepository
  {
    Task<User> GetUser(Guid userId);

    Task<User> GetUserByEmail(string email);

    Task CreateUser(User user);

    Task UpdateUser(User user);

    Task DeleteUser(Guid userId);
  }
}
