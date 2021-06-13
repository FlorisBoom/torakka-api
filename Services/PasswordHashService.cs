using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MangaAlert.Repositories;

namespace MangaAlert.Services
{
  public class PasswordHash: IPasswordHash
  {
    private readonly IUserRepository _userRepository;
    private const int Iterations = 100000;

    public PasswordHash(IUserRepository userRepository)
    {
      this._userRepository = userRepository;
    }

    public async Task<string> HashPassword(string plainPassword)
    {
      // Create salt value with cryptographic PRNG
      byte[] salt;
      new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

      // Create Rfc2898DeriveBytes to get has value
      var pbkdf2 = new Rfc2898DeriveBytes(plainPassword, salt, Iterations);
      byte[] hash = pbkdf2.GetBytes(20);

      // Combine the salt and password bytes for later
      byte[] hashBytes = new byte[36];
      Array.Copy(salt, 0, hashBytes, 0, 16);
      Array.Copy(hash, 0, hashBytes, 16, 20);

      // Turn the combined salt & hash into a string for storage
      var savedPasswordHash = Convert.ToBase64String(hashBytes);

      return savedPasswordHash;
    }

    public async Task<bool> IsCorrectPassword(Guid userId, string plainPassword)
    {
      var passwordHash = (await _userRepository.GetUser(userId)).Password;

      // Extract bytes
      byte[] hashBytes = Convert.FromBase64String(passwordHash);

      // Get the salt
      byte[] salt = new byte[16];
      Array.Copy(hashBytes, 0, salt, 0, 16);

      // Compute has on the password the user entered
      var pbkdf2 = new Rfc2898DeriveBytes(plainPassword, salt, Iterations);
      byte[] hash = pbkdf2.GetBytes(20);

      for (int i = 0; i < 20; i++) {
        if (hashBytes[i + 16] != hash[i]) {
          return false;
        }
      }

      return true;
    }
  }

  public interface IPasswordHash
  {
    Task<string> HashPassword(string plainPassword);

    Task<bool> IsCorrectPassword(Guid userId, string plainPassword);
  }
}
