using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using STAJ.Data;
using STAJ.Middleware;
using STAJ.Repositories;
using STAJ.Results;
using STAJ.Services;

Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day).CreateLogger();
try
{
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
var supportedCultures = new[] { "tr-TR", "en-US" };
builder.Services.AddLocalization(options => options.ResourcesPath = "");
builder.Services.Configure<RequestLocalizationOptions>(options => { options.SetDefaultCulture("tr-TR"); options.AddSupportedCultures(supportedCultures); options.AddSupportedUICultures(supportedCultures); options.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider()); });
builder.Services.AddCors(options => options.AddPolicy("AngularPolicy", policy => policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options => { options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"], IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)) }; });
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options => { options.InvalidModelStateResponseFactory = context => { var errors = context.ModelState.Where(x => x.Value?.Errors.Count > 0).SelectMany(x => x.Value!.Errors).Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Geçersiz veri gönderildi." : x.ErrorMessage).ToList(); return new BadRequestObjectResult(new DataResult<List<string>>(false, "Gönderilen bilgiler geçersiz.", errors)); }; });
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen(); builder.Services.AddScoped<IMusteriRepository, MusteriRepository>(); builder.Services.AddScoped<MusteriService>(); builder.Services.AddScoped<AuthService>(); builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
var app = builder.Build();
using (var scope = app.Services.CreateScope()) { var context = scope.ServiceProvider.GetRequiredService<AppDbContext>(); await DataSeeder.SeedAsync(context); }
var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture("tr-TR").AddSupportedCultures(supportedCultures).AddSupportedUICultures(supportedCultures); localizationOptions.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());
app.UseRequestLocalization(localizationOptions); app.UseSerilogRequestLogging(); app.UseMiddleware<ExceptionMiddleware>(); app.UseCors("AngularPolicy"); app.UseSwagger(); app.UseSwaggerUI(); if (!app.Environment.IsDevelopment()) app.UseHsts(); app.UseHttpsRedirection(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers(); app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "Application terminated unexpectedly."); }
finally { Log.CloseAndFlush(); }
