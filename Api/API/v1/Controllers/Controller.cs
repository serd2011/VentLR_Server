using Microsoft.AspNetCore.Mvc;

using Application.DTO;
using Application.Services;
using API.v1.Models.Controller;

namespace API.v1.Controllers
{
    [Route("")]
    [ApiController]
    public class Controller : ControllerBase
    {
        private readonly I_HW_API _hwController;

        public Controller(I_HW_API hwController)
        {
            _hwController = hwController;
        }

        [HttpGet("status/")]
        public IActionResult Status()
        {
            SystemStatus status = _hwController.GetStatus();
            StatusResponse response = new StatusResponse();
            response.state = status.SystemState;
            response.targetTemperature = status.TargetTemperature;
            response.airTemperature = status.Temperature;
            response.heaterCapacity = status.CapacityOfHeater;
            StatusResponse.Failures failures = new StatusResponse.Failures();
            failures.heater = status.Termostat;
            failures.fan = status.FanRelay;
            failures.frequencyConverter = status.FCError;
            response.failures = failures;

            return new OkObjectResult(response);
        }

        [HttpPost("enable/")]
        public IActionResult Enable([FromBody] EnableRequest request)
        {
            _hwController.SendEnable(request.enable);
            return new OkResult();
        }

        [HttpPost("targetTemperature/")]
        public IActionResult targetTemperature([FromBody] TragetTemperatureRequest request)
        {
            _hwController.SendTemperature(request.temperature);
            return new OkResult();
        }

    }
}