using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using AutoMapper;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.OpenApi;
using Serilog;
using STAJ.Data;
using STAJ.Events;
using STAJ.Hubs;
using STAJ.Jobs;
using STAJ.Middleware;
using STAJ.Profiles;
using STAJ.Repositories;
using STAJ.Results;
using STAJ.Services;
using STAJ.Validators;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "STAJ-Backend")
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, shared: true)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var supportedCultures = new[] { "tr-TR", "en-US" };
    var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins")
        .GetChildren()
        .Select(x => x.Value)
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Cast<string>()
        .ToArray();

    if (allowedOrigins.Length == 0)
        allowedOrigins = ["http://localhost:4200"];

    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
    {
        if (builder.Environment.IsDevelopment())
        {
            jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            builder.Configuration["Jwt:Key"] = jwtKey;
            Log.Warning("Development ortamında geçici JWT anahtarı üretildi. Uygulama yeniden başlatıldığında mevcut access tokenlar geçersiz olur.");
        }
        else
        {
            throw new InvalidOperationException("Production ortamında Jwt:Key gizli yapılandırma olarak tanımlanmalıdır.");
        }
    }

    if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
        throw new InvalidOperationException("Jwt:Key en az 32 byte olmalıdır.");

    builder.Services.AddLocalization(options => options.ResourcesPath = "");
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.SetDefaultCulture("tr-TR");
        options.AddSupportedCultures(supportedCultures);
        options.AddSupportedUICultures(supportedCultures);
        options.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());
    });

    builder.Services.AddCors(options => options.AddPolicy("AngularPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

    builder.Services.AddMemoryCache();
    builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MusteriProfile>());
    builder.Services.AddSignalR();
    builder.Services.AddAuthorization(options =>
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")));

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(connectionString));
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2;
        options.Queues = new[] { "default" };
    });

    var permitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 20);
    var userPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:UserPermitLimit", 30);
    var loginPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:LoginPermitLimit", 5);
    var authPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:AuthPermitLimit", 10);
    var readPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:ReadPermitLimit", 40);
    var writePermitLimit = builder.Configuration.GetValue<int>("RateLimiting:WritePermitLimit", 15);
    var windowSeconds = builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var userId = context.User.Identity?.IsAuthenticated == true
                ? context.User.Identity.Name ?? context.User.FindFirst("sub")?.Value ?? "authenticated-user"
                : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var limit = context.User.Identity?.IsAuthenticated == true ? userPermitLimit : permitLimit;
            return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });
        options.AddFixedWindowLimiter("login", o =>
        {
            o.PermitLimit = loginPermitLimit;
            o.Window = TimeSpan.FromSeconds(windowSeconds);
            o.QueueLimit = 0;
        });
        options.AddFixedWindowLimiter("auth", o =>
        {
            o.PermitLimit = authPermitLimit;
            o.Window = TimeSpan.FromSeconds(windowSeconds);
            o.QueueLimit = 0;
        });
        options.AddFixedWindowLimiter("read", o =>
        {
            o.PermitLimit = readPermitLimit;
            o.Window = TimeSpan.FromSeconds(windowSeconds);
            o.QueueLimit = 0;
        });
        options.AddFixedWindowLimiter("write", o =>
        {
            o.PermitLimit = writePermitLimit;
            o.Window = TimeSpan.FromSeconds(windowSeconds);
            o.QueueLimit = 0;
        });
    });

    builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Geçersiz veri gönderildi." : x.ErrorMessage)
                .ToList();
            return new BadRequestObjectResult(new DataResult<List<string>>(false, "Gönderilen bilgiler geçersiz.", errors));
        };
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "STAJ API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT token girin. Örnek: Bearer {token}"
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });

    builder.Services.AddScoped<IMusteriRepository, MusteriRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IMusteriService, MusteriService>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<MailService>();
    builder.Services.AddScoped<CloudinaryImageService>();
    builder.Services.AddScoped<IDomainEventDispatcher, SignalRDomainEventDispatcher>();
    builder.Services.AddScoped<IValidator<STAJ.Entities.Musteri>, MusteriValidator>();
    builder.Services.AddScoped<ScheduledJobsService>();
    builder.Services.AddDbContext<AppDbContext>(options => options
        .UseNpgsql(connectionString)
        .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DataSeeder.SeedAsync(context);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS \"WorkflowRequests\" (\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \"Type\" text NOT NULL, \"Status\" text NOT NULL, \"RequestedBy\" text NOT NULL, \"DataJson\" text NOT NULL, \"CreatedAt\" timestamp with time zone NOT NULL, \"ReviewedAt\" timestamp with time zone NULL, \"ReviewedBy\" text NULL, \"ReviewComment\" text NULL);");
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_WorkflowRequests_Type_Status\" ON \"WorkflowRequests\" (\"Type\", \"Status\");");
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_WorkflowRequests_RequestedBy\" ON \"WorkflowRequests\" (\"RequestedBy\");");
    }

    var localizationOptions = new RequestLocalizationOptions()
        .SetDefaultCulture("tr-TR")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    localizationOptions.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());

    app.UseRequestLocalization(localizationOptions);
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging(options => options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms");
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseCors("AngularPolicy");
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<CsrfProtectionMiddleware>();
    app.UseMiddleware<AuditMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
        IsReadOnlyFunc = _ => false
    });

    var logCron = builder.Configuration.GetValue<string>("BackgroundJobs:DailyLogMaintenanceCron") ?? "0 3 * * *";
    var healthCron = builder.Configuration.GetValue<string>("BackgroundJobs:DatabaseHealthCheckCron") ?? "*/30 * * * *";
    var birthdayEmailCron = builder.Configuration.GetValue<string>("BackgroundJobs:BirthdayEmailCron") ?? "0 9 * * *";

    RecurringJob.AddOrUpdate<ScheduledJobsService>("daily-log-maintenance", job => job.GunlukLogBakimiAsync(), logCron, new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
    RecurringJob.AddOrUpdate<ScheduledJobsService>("database-health-check", job => job.VeritabaniKontroluAsync(), healthCron, new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
    RecurringJob.AddOrUpdate<ScheduledJobsService>("birthday-email", job => job.DogumGunuMailleriAsync(), birthdayEmailCron, new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

    Log.Information("Hangfire zamanlanmış işleri kaydedildi. Log bakım: {LogCron}, DB kontrol: {HealthCron}, Doğum günü maili: {BirthdayEmailCron}", logCron, healthCron, birthdayEmailCron);

    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "STAJ-Backend başlatılırken beklenmeyen hata oluştu.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
