using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Services;
using FormationManager.Infrastructure.Exceptions;
using FormationManager.Infrastructure.OCR;
using FormationManager.Infrastructure.AI;
using FormationManager.Infrastructure.Sync;
using FormationManager.Infrastructure.HealthChecks;
using FormationManager.Infrastructure.AI;
using Serilog;
using Serilog.Events;

// Configuration Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Démarrage de l'application FormatiX");

    var builder = WebApplication.CreateBuilder(args);

    // Utilisation de Serilog pour le logging
    builder.Host.UseSerilog();

    // Configuration de la base de données SQLite
    builder.Services.AddDbContext<FormationDbContext>(options =>
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    // Configuration de l'identité
    builder.Services.AddIdentity<Utilisateur, IdentityRole<int>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<FormationDbContext>()
    .AddDefaultTokenProviders();

    // Configuration des cookies
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

    // Configuration des services MVC
    builder.Services.AddControllersWithViews()
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        });

    // Services métier existants
    builder.Services.AddScoped<IDocumentService, DocumentService>();
    builder.Services.AddScoped<IQualiopiService, QualiopiService>();
    builder.Services.AddScoped<IQualiopiAutoProofService, QualiopiAutoProofService>();
    builder.Services.AddScoped<IBPFService, BPFService>();
    builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddScoped<IOrganizationService, OrganizationService>();
    builder.Services.AddScoped<ISiteContext, SiteContext>();
    builder.Services.AddScoped<ICritereSuggestionService, CritereSuggestionService>();
    builder.Services.AddScoped<IInscriptionLinkService, InscriptionLinkService>();
    builder.Services.AddScoped<IConflitsSessionService, ConflitsSessionService>();
    builder.Services.AddScoped<IFacturationService, FacturationService>();
    builder.Services.AddScoped<IVeilleRssService, VeilleRssService>();

    // Services infrastructure
    builder.Services.AddHttpClient(); // Pour HTTP clients (Ollama, Sync)
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IOCRService, TesseractOCRService>();
    builder.Services.AddScoped<IAIService, OllamaAIService>();
    builder.Services.AddHostedService<OllamaAutoStartHostedService>();
    builder.Services.AddScoped<ISyncService, SyncService>();

    // Health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<FormationDbContext>("database", tags: new[] { "db", "sqlite" })
        .AddCheck<OllamaHealthCheck>("ollama", tags: new[] { "ai", "ollama" });

    // Gestion d'erreurs globale
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // API Documentation (Swagger)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "FormatiX API",
            Version = "v1",
            Description = "API FormatiX pour la gestion de formations Qualiopi avec synchronisation décentralisée",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "FormatiX Support",
                Email = "support@formatix.fr"
            }
        });

        // Configuration pour JWT si nécessaire plus tard
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header utilisant le schéma Bearer",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
    });

    var app = builder.Build();

    // Configuration du pipeline HTTP
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FormatiX API v1");
            c.RoutePrefix = "swagger";
        });
    }
    else
    {
        app.UseExceptionHandler();
        app.UseHsts();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // Health check endpoint
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    exception = e.Value.Exception?.Message,
                    duration = e.Value.Duration.TotalMilliseconds,
                    tags = e.Value.Tags.ToArray()
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            }, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            await context.Response.WriteAsync(result);
        }
    });

    // Configuration des routes
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "api",
        pattern: "api/{controller}/{action=Index}/{id?}");

    // Initialisation des données
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<FormationDbContext>();
            var userManager = services.GetRequiredService<UserManager<Utilisateur>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

            // Migrations automatiques
            await context.Database.MigrateAsync();
            logger.LogInformation("Base de données migrée avec succès");

            // Initialisation des données de seed
            await SeedData.InitializeAsync(services, userManager, roleManager);
            logger.LogInformation("Base de données initialisée avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'initialisation de la base de données");
            throw;
        }
    }

    Log.Information("Application FormatiX démarrée avec succès");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application arrêtée de manière inattendue");
}
finally
{
    Log.CloseAndFlush();
}