using IndianChemist.Repositories;
using IndianChemist.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT" ) ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
var repository = new JsonFileRepository(builder.Configuration["DataDirectory"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
builder.Services.AddSingleton(repository);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddControllersWithViews().ConfigureApiBehaviorOptions(options =>
    options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new
    {
        message = "Invalid request. Check required fields and data types.",
        errors = context.ModelState.Where(e => e.Value!.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray())
    }));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidIssuer = "IndianChemist",
        ValidateAudience = true,
        ValidAudience = "IndianChemist",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(repository.Key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(10)
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new() { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    try
    {
        await next();
    }
    catch (BadHttpRequestException)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Invalid request. Check field values and data types."
        });
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Unhandled request failure");
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Request could not be completed."
            });
        }
    }
});
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
