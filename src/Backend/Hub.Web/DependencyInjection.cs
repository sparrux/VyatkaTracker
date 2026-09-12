using Asp.Versioning;
using Hub.Application.Abstractions;
using Hub.Web.Authentication;
using Hub.Web.Authentication.OAuth.Events;
using Hub.Web.Authentication.OAuth.Store;
using Hub.Web.Endpoints;
using Hub.Web.Hangfire;
using Hub.Web.OpenApi;
using Hub.Web.Services.Seeders;
using Hub.Web.Services.Users;
using ServiceDefaults;
using OAuthOptions = Hub.Web.Authentication.OAuth.OAuthOptions;

namespace Hub.Web;

static class DependencyInjection
{
    extension(WebApplicationBuilder builder)
    {
        public void AddWeb()
        {
            builder.AddServiceDefaults();

            builder.Services.AddMemoryCache(); // TODO: DistributedCache
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IUserContext, CurrentUserContext>();
            builder.Services.AddScoped<IUserProvisioningService, UserProvisioningService>();
            builder.Services.AddScoped<OpenIdConnectAuthEvents>();
            
            builder.Services.AddScoped<ISeeder, UsersSeeder>();

            builder.Services.AddOptions<OAuthOptions>()
                .Bind(builder.Configuration.GetSection(OAuthOptions.SectionName))
                .ValidateOnStart();

            builder.Services.AddSingleton<IOAuthTokenStore, MemoryOAuthTokenStore>();

            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;

                options.ApiVersionReader =
                    new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<OAuth2SecuritySchemeTransformer>();
            });

            builder.AddAuthentication();
            builder.AddCors();
            builder.Services.AddAuthorization();
            
            builder.AddHangfireHost();
        }

        void AddCors()
        {
            var origin = builder.Configuration["Clients:hub-app:Url"];

            if (string.IsNullOrWhiteSpace(origin))
                throw new InvalidOperationException("Clients:hub-app:Url is not configured for CORS.");

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(origin)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }
    }

    extension(WebApplication app)
    {
        public async Task Seed()
        {
            var scope = app.Services.CreateScope();
            
            var seeders = scope.ServiceProvider.GetRequiredService<IEnumerable<ISeeder>>();

            foreach (var seeder in seeders)
            {
                await seeder.Seed(CancellationToken.None);
            }
        }
        
        public void MapEndpoints()
        {
            app.MapAuthEndpoints();
            app.MapUserEndpoints();
            app.MapEventEndpoints();
            app.MapGroupEndpoints();
            app.MapDonationEndpoints();
            app.MapPaymentWebhookEndpoints();
        }

        public void MapWebOpenApi()
        {
            app.MapOpenApi();
            app.MapScalar();
        }
    }
}
