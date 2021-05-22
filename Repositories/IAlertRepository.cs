using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MangaAlert.Entities;

namespace MangaAlert.Repositories
{
  public interface IAlertRepository
  {
    Task<Alert> GetAlertForUser (Guid alertId, Guid userId);

    Task<IEnumerable<Alert>> GetAlerts();

    Task CreateAlert(Alert alert);

    Task UpdateAlert(Alert alert);

    Task DeleteAlert(Guid alertId);
  }
}
