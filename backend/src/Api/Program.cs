var builder = WebApplication.CreateBuilder(args);

// Sprint 3 (Core Kernel) registers each Foundation framework's own services here as
// it is implemented -- e.g. builder.Services.AddHrisAuthorization(...),
// builder.Services.AddHrisAudit(...). None is registered yet; this host currently
// exposes only the health endpoints docs/08-devops/monitoring-and-alerting.md and
// docs/08-devops/ci-cd-pipeline.md already depend on.
builder.Services.AddHealthChecks();

var app = builder.Build();

// docs/08-devops/monitoring-and-alerting.md, "Health Monitoring (NFR-OB-003)":
// liveness and readiness are distinct endpoints with distinct check sets, never
// merged into one.
//
// /health/live  -- "is the process itself still functioning" -- no external
//                  dependency checks. The container host restarts on failure here
//                  (docs/08-devops/containerization.md).
app.MapHealthChecks("/health/live");

// /health/ready -- "should this instance currently receive traffic" -- database and
//                  cache connectivity checks belong here once EF Core and Redis are
//                  wired up in a later Sprint. ci-cd-pipeline.md's post-deployment
//                  "Health check verification" step calls this endpoint, not
//                  /health/live -- an instance that is alive but still warming its
//                  connection pool should not be judged a failed deployment.
app.MapHealthChecks("/health/ready");

app.Run();
