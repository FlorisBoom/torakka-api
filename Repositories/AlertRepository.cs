using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MangaAlert.Entities;
using MangaAlert.Settings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MangaAlert.Repositories
{
  public class AlertRepository: IAlertRepository
  {
    private const string CollectionName = "alerts";
    private readonly IMongoCollection<Alert> _alertsCollection;
    private readonly FilterDefinitionBuilder<Alert> _filterBuilder = Builders<Alert>.Filter;

    public AlertRepository(IMongoDbSettings settings)
    {
      var client = new MongoClient(settings.ConnectionString);
      IMongoDatabase database = client.GetDatabase(settings.DatabaseName);
      _alertsCollection = database.GetCollection<Alert>(CollectionName);
    }

    public async Task<Alert> GetAlertForUser(Guid alertId, Guid userId)
    {
      var filter =
        _filterBuilder.Eq(alert => alert.Id, alertId)
        & _filterBuilder.Eq(alert => alert.UserId, userId);

      return await _alertsCollection.Find(filter).SingleOrDefaultAsync();
    }

    #nullable enable
    public async Task<IEnumerable<Alert>> GetAlertsForUser(
      Guid userId,
      string? status,
      int? limit,
      string? sortBy,
      int? sortOption,
      int? page
      )
    {
      var filter = _filterBuilder.Eq(alert => alert.UserId, userId);
      var sortObject = new BsonDocument("LatestChapterUpdatedAt", -1);

      if (!string.IsNullOrEmpty(status)) {
        filter = _filterBuilder.Eq(alert => alert.UserId, userId)
          & _filterBuilder.Eq(alert => alert.Status, status);
      }

      if (!string.IsNullOrEmpty(sortBy)) {
        sortObject = new BsonDocument(sortBy, -1);
      }

      if (sortOption != null) {
        sortObject = new BsonDocument("CreatedAt", sortOption);
      }

      if (!string.IsNullOrEmpty(sortBy) && sortOption != null) {
        sortObject = new BsonDocument(sortBy, sortOption);
      }

      return await _alertsCollection
        .Find(filter)
        .Skip((page - 1) * limit)
        .Sort(sortObject)
        .Limit(limit)
        .ToListAsync();
    }

    public async Task CreateAlert(Alert alert)
    {
      await _alertsCollection.InsertOneAsync(alert);
    }

    public async Task UpdateAlert(Alert alert)
    {
      var filter = _filterBuilder.Eq(existingAlert => existingAlert.Id, alert.Id);

      await _alertsCollection.ReplaceOneAsync(filter, alert);
     }

    public async Task DeleteAlert(Guid alertId)
    {
      var filter = _filterBuilder.Eq(alert => alert.Id, alertId);

      await _alertsCollection.DeleteOneAsync(filter);
    }
  }
}
