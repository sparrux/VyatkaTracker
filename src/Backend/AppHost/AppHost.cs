var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("vyatka-db")
    .WithDataVolume("vyatka_postgres-db")
    .WithPgAdmin(pgAdmin => 
        pgAdmin.WithHostPort(5050));

var identityDb = postgres.AddDatabase("identitydb");
var hubDb = postgres.AddDatabase("hubdb");

var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume("vyatka_rabbitmq");

var identityApi = builder.AddProject<Projects.Identity_WebAPI>(
        "identity-api")
    .WithExternalHttpEndpoints()
    .WithReference(identityDb)
    .WithHttpHealthCheck("/health");

var hubApi = builder.AddProject<Projects.Hub_Web>(
        "hub-api")
    .WithExternalHttpEndpoints()
    .WithReference(identityApi)
    .WithReference(hubDb)
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithHttpHealthCheck("/health");

var frontend = "../../Frontend";

var identityApp = builder.AddJavaScriptApp("identity-app", frontend)
    .WithRunScript("start:identity")
    .WithReference(identityApi)
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WaitFor(identityApi);

var hubApp = builder.AddJavaScriptApp("hub-app", frontend)
    .WithRunScript("start:hub")
    .WithReference(identityApi)
    .WithReference(hubApi)
    .WithHttpEndpoint(port: 4201, env: "PORT")
    .WaitFor(identityApi)
    .WaitFor(hubApi);

var identityIsHttps = identityApp.GetEndpoint("https").Exists;
var hubIsHttps = hubApp.GetEndpoint("https").Exists;
var identityApiIsHttps = identityApi.GetEndpoint("https").Exists;
var hubApiIsHttps = hubApi.GetEndpoint("https").Exists;

var webClientEndpoint = identityApp.GetEndpoint(identityIsHttps ? "https" : "http");
var hubAppEndpoint = hubApp.GetEndpoint(hubIsHttps ? "https" : "http");
var identityApiEndpoint = identityApi.GetEndpoint(identityApiIsHttps ? "https" : "http");
var hubApiEndpoint = hubApi.GetEndpoint(hubApiIsHttps ? "https" : "http");

identityApi.WithEnvironment("Clients:identity-app:Url", webClientEndpoint);
identityApi.WithEnvironment("Clients:hub-app:Url", hubAppEndpoint);
identityApi.WithEnvironment("Clients:hub-bff:Url", hubApiEndpoint);
identityApi.WithEnvironment("Clients:hub-bff:RedirectUri", $"{hubApiEndpoint}/auth/callback");
identityApi.WithEnvironment("Clients:hub-scalar:Url", hubApiEndpoint);
identityApi.WithEnvironment("Clients:hub-scalar:RedirectUri", $"{hubApiEndpoint}/scalar/v1");
identityApi.WithEnvironment("Idp:Authority", identityApiEndpoint);
identityApi.WithEnvironment("Idp:LoginPageUrl", $"{webClientEndpoint}/login");

hubApi.WithEnvironment("OAuth:Authority", identityApiEndpoint);
hubApi.WithEnvironment("Clients:hub-app:Url", hubAppEndpoint);
hubApi.WithEnvironment("Clients:hub-scalar:RedirectUri", $"{hubApiEndpoint}/scalar/v1");

builder.Build().Run();