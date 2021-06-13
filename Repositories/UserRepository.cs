using System;
using System.Threading.Tasks;
using MangaAlert.Entities;
using MangaAlert.Settings;
using MongoDB.Driver;

namespace MangaAlert.Repositories
{
  public class UserRepository : IUserRepository
  {
    private const string CollectionName = "users";
    private readonly IMongoCollection<User> _usersCollection;
    private readonly FilterDefinitionBuilder<User> _filterBuilder = Builders<User>.Filter;

    public UserRepository(IMongoDbSettings settings)
    {
      var client = new MongoClient(settings.ConnectionString);
      IMongoDatabase database = client.GetDatabase(settings.DatabaseName);
      _usersCollection = database.GetCollection<User>(CollectionName);
    }

    public async Task<User> GetUser(Guid userId)
    {
      var filter = _filterBuilder.Eq(user => user.Id, userId);

      return await _usersCollection.Find(filter).SingleOrDefaultAsync();
    }

    public async Task<User> GetUserByEmail(string email)
    {
      var filter = _filterBuilder.Eq(user => user.Email, email);

      return await _usersCollection.Find(filter).SingleOrDefaultAsync();
    }

    public async Task CreateUser(User user)
    {
      await _usersCollection.InsertOneAsync(user);
    }

    public async Task UpdateUser(User user)
    {
      var filter = _filterBuilder.Eq(existingUser => existingUser.Id, user.Id);

      await _usersCollection.ReplaceOneAsync(filter, user);
    }

    public async Task DeleteUser(Guid userId)
    {
      var filter = _filterBuilder.Eq(user => user.Id, userId);

      await _usersCollection.DeleteOneAsync(filter);
    }
  }
}
