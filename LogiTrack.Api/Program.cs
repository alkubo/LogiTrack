using LogiTrack.Api; // context
using LogiTrack.Api.Models; // models
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Memory cache
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // simple cap
});

// Add services to the container.
// Register DbContext with DI specifying SQLite connection
builder.Services.AddDbContext<LogiTrackContext>(options =>
    options.UseSqlite("Data Source=logitrack.db"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<LogiTrackContext>()
    .AddDefaultTokenProviders();

// JWT configuration (demo secret)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-change";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LogiTrack";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Hosted service for migrations & initial seed
builder.Services.AddHostedService<StartupInitializer>();

var app = builder.Build();

// Global exception handling / minimal problem details
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "Unexpected server error", detail = ex.Message });
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Hosted service implementation
public class StartupInitializer : IHostedService
{
    private readonly IServiceProvider _sp;
    public StartupInitializer(IServiceProvider sp) => _sp = sp;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LogiTrackContext>();
        await context.Database.MigrateAsync(cancellationToken);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        const string managerRole = "Manager";
        if (!await roleManager.RoleExistsAsync(managerRole))
        {
            await roleManager.CreateAsync(new IdentityRole(managerRole));
        }

        var adminEmail = "manager@logitrack.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            await userManager.CreateAsync(admin, "Pass@word1!");
            await userManager.AddToRoleAsync(admin, managerRole);
        }

        if (!context.InventoryItems.Any())
        {
            context.InventoryItems.Add(new InventoryItem
            {
                Name = "Pallet Jack",
                Quantity = 12,
                Location = "Warehouse A"
            });
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
