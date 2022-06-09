// startup.cs liveness and readiness probes

services.AddHealthChecks()
                .AddCheck<StartupHostedServiceHealthCheck>(
                    "hosted_service_startup",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "ready" });

            services.AddHealthChecks()
                .AddCheck<HealthCheckController>(
                    "Database availability check",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "healthz", "ready" });

app.UseEndpoints(endpoints => {
                endpoints.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = (check) => check.Tags.Contains("healthz") });
                endpoints.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = (check) => check.Tags.Contains("ready") });

                endpoints.MapControllers();
            });
			
			
// StartupHostedServiceHealthCheck.cs

public class StartupHostedServiceHealthCheck : IHealthCheck {
        private volatile bool _startupTaskCompleted = false;

        public string Name => "slow_dependency_check";

        public bool StartupTaskCompleted {
            get => _startupTaskCompleted;
            set => _startupTaskCompleted = value;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default(CancellationToken)) {
            if(StartupTaskCompleted) {
                return Task.FromResult(
                    HealthCheckResult.Healthy("The startup task is finished."));
            }

            return Task.FromResult(
                HealthCheckResult.Unhealthy("The startup task is still running."));
        }
    }

// StartupTaskCompleted is set to true in the end of Startup.cs

// HealthCheckController
public class HealthCheckController : IHealthCheck {
        private readonly IMediator _mediator;
        public HealthCheckController(IMediator mediator) {
            _mediator = mediator;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
		// there is a request to database inside
            var result = await _mediator.Send(new GetAvailabilityDatabaseQuery());

            return result.IsAvailable
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Cannot reach db");
        }
