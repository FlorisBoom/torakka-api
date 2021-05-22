using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MangaAlert.Dtos;
using MangaAlert.Entities;
using MangaAlert.Repositories;
using Microsoft.AspNetCore.Mvc;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MangaAlert.Controllers
{
  [ApiController]
  [Produces("application/json")]
  [Route("[controller]")]
  public class AlertsController: ControllerBase
  {
    private readonly IAlertRepository _repository;

    public AlertsController(IAlertRepository repository)
    {
      this._repository = repository;
    }

    // GET /alerts
    [HttpGet]
    public async Task<ActionResult<AlertDto>> GetAlerts()
    {
      var alerts = (await _repository.GetAlerts())
        .Select(alert => alert.AsDto());

      return Ok(new {
        data = alerts
      });
    }

    // GET /alerts/{userId}/{alertId}
    [HttpGet("{userId}/{alertId}")]
    public async Task<ActionResult<AlertDto>> GetAlertForUser(Guid userId, Guid alertId)
    {
      Alert alert = await _repository.GetAlertForUser(alertId, userId);

      if (alert is null) return NotFound();

      return Ok(new {
        data = alert.AsDto()
      });
    }

    // POST /alerts/{userId}
    [HttpPost("{userId}")]
    public async Task<ActionResult<AlertDto>> CreateAlert(Guid userId, CreateAlertDto alertDto)
    {
      Alert alert = new() {
        Id = Guid.NewGuid(),
        Title = alertDto.Title,
        Url = alertDto.Url,
        LatestChapter = alertDto.LatestChapter,
        UserId = userId
      };

      await _repository.CreateAlert(alert);

      var returnObject = new {
        data = alert.AsDto()
      };

      return CreatedAtAction(nameof(GetAlertForUser), new {
        userId = alert.UserId, alertId = alert.Id
      }, returnObject);
    }

    // PUT /Alerts/{userId}/{alertId}
    [HttpPut("{userId}/{alertId}")]
    public async Task<ActionResult<AlertDto>> UpdateAlert(Guid userId, Guid alertId, UpdateAlertDto alertDto)
    {
      Alert existingAlert = await _repository.GetAlertForUser(alertId, userId);

      if (existingAlert is null) return NotFound();

      Alert updatedAlert = (existingAlert with {
        Title = alertDto.Title,
        Url = alertDto.Url,
        LatestChapter = alertDto.LatestChapter
      });

      await _repository.UpdateAlert(updatedAlert);

      return Ok(new {
        data = updatedAlert.AsDto()
      });
    }

    // DELETE /Alerts/{userId}/{alertId}
    [HttpDelete("{userId}/{alertId}")]
    public async Task<ActionResult> DeleteAlert(Guid userId, Guid alertId)
    {
      Alert existingAlert = await _repository.GetAlertForUser(alertId, userId);

      if (existingAlert is null) return NotFound();

      await _repository.DeleteAlert(alertId);

      return StatusCode(200,  new {
        data = "success"
      });
    }
  }
}
