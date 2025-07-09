namespace WhatsAppAIAssistantBot.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Health check controller that provides endpoints for monitoring application health and readiness.
    /// These endpoints are typically used by load balancers, orchestrators, and monitoring systems
    /// to determine if the application is healthy and ready to receive traffic.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Returns the basic health status of the application.
        /// This endpoint indicates whether the application is running and responsive.
        /// </summary>
        /// <returns>
        /// An object containing health status, current UTC timestamp, and application version
        /// </returns>
        /// <response code="200">Application is healthy and responsive</response>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        /// <summary>
        /// Returns the readiness status of the application.
        /// This endpoint indicates whether the application is ready to handle requests.
        /// Typically used by Kubernetes readiness probes and deployment verification.
        /// </summary>
        /// <returns>
        /// An object containing readiness status and current UTC timestamp
        /// </returns>
        /// <response code="200">Application is ready to handle requests</response>
        [HttpGet("ready")]
        public IActionResult Ready()
        {
            return Ok(new
            {
                status = "ready",
                timestamp = DateTime.UtcNow
            });
        }
    }
}