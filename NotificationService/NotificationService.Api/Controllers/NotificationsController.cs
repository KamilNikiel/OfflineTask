using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Dtos;
using NotificationService.Api.Mappers;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Models;

namespace NotificationService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationDispatcher _dispatcher;

        public NotificationsController(INotificationDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendNotificationRequest request, CancellationToken cancellationToken)
        {
            var notification = NotificationMapper.MapToNotification(request);

            var dispatchStatus = await _dispatcher.DispatchAsync(notification, cancellationToken);

            var response = NotificationMapper.MapToResponse(notification, dispatchStatus);

            return dispatchStatus switch
            {
                DispatchStatus.Success => Ok(response),
                DispatchStatus.Delayed => Accepted(response),
                DispatchStatus.Failed => StatusCode(500, response),
                _ => StatusCode(500, response)
            };
        }
    }
}