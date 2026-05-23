
using Microsoft.EntityFrameworkCore;
using Souqify.Application.Interfaces;
using Souqify.Application.Mappings;
using Souqify.Application.Services;
using Souqify.Extensions;
using Souqify.Filter;
using Souqify.Infrastructure.Queries;
using Souqify.Infrastructure.Repositories;
using Souqify.Middlewares;
using FluentValidation;
using Souqify.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Souqify.Infrastructure;
using Souqify.Application.Validations.Product;
using Souqify.Infrastructure.Auth;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.RateLimiting;
using Souqify.Infrastructure.Auditing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

//i dont need to add the rest of validators bcs assembly will scan the entire file where the CreateProductValidator
//lives and fins the classes that inherit AbstractValidator and register them all
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();


var issuer = builder.Configuration["Authentication:Issuer"]
    ?? throw new InvalidOperationException("Authentication: Issuer not configured");
var audience = builder.Configuration["Authentication:Audience"]
    ?? throw new InvalidOperationException("Authentication: Audience not configured");
var key = builder.Configuration["Authentication:SecretKey"]
    ?? throw new InvalidOperationException("Authentication: Secret key not configured");


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime=true,
        ClockSkew=TimeSpan.Zero,
        ValidAudience = audience,
        ValidIssuer = issuer,
        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(key))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBeAdmin", policy =>
    {
        policy.RequireRole("admin");
    });
});

//HSTS config
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(typeof(ProductMappingProfile).Assembly);

//connection to database
builder.Services.AddScoped<AuditIntercepter>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDatabase(connectionString);

//identity user
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    //Password configurations
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    //Lockout configurations
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(3);
}).AddEntityFrameworkStores<SouqifyDbContext>();


//rate limiter
builder.Services.AddRateLimiter(limiterOptions =>
{
    //
    limiterOptions.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }
        else
        {
            context.HttpContext.Response.Headers.RetryAfter = "60";
        }

        context.HttpContext.Response.ContentType = "application/problem+json";

        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            title = "Too many requests",
            status = 429,
            detail = "You have exceeded the allowed number of requests. Please try again later."
        });

        await context.HttpContext.Response.WriteAsync(body, cancellationToken);

        //i use the manual appove method bcs WriteAsJsonAsync will override (application/problem+json) and make it (application/json)
        //await context.HttpContext.Response.WriteAsJsonAsync(new
        //{
        //    title = "Too many requests",
        //    status = 429,
        //    detail = "You have exceeded the allowed number of requests. Please try again later."
        //}, cancellationToken);
    };

    //this is global rate limiter for all users
    //limiterOptions.AddSlidingWindowLimiter("LoginLimiter", options =>
    //{
    //    options.Window = TimeSpan.FromMinutes(1);
    //    options.SegmentsPerWindow = 5;
    //    options.PermitLimit = 5;
    //});

    //this is per ip address rate limiter
    limiterOptions.AddPolicy("LoginLimiter", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "UnKnown",
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                PermitLimit = 5,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    limiterOptions.AddPolicy("RefreshLimiter", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                PermitLimit = 20,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

//CORS config
builder.Services.AddCors(options =>
{
    options.AddPolicy("souqify", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:4200");//this where the frontend runs not when the api runs
            policy.AllowAnyMethod();
            policy.AllowAnyHeader();
            policy.AllowCredentials();
        }
        else
        {
            policy.WithOrigins("https://souqify.com");
            policy.SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            policy.WithHeaders("Authorization", "Content-Type");
            policy.WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS");
            policy.AllowCredentials();
        }

    });
});


//DI services
builder.Services.AddScoped<IAdminProductQueries, AdminProductQueries>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductQueries, ProductQueries>();
builder.Services.AddScoped<ICategoryQueries, CategoryQueries>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var adminEmail = app.Configuration["Seed:Email"];
    var adminPassword = app.Configuration["Seed:Password"];

    foreach(var item in new[] { "Customer", "admin" })
    {
        if(!await roleManager.RoleExistsAsync(item))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(item));
        }
    }

    if (!(string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword)))
    {
        var user = await userManager.FindByEmailAsync(adminEmail);

        if (user == null)
        {
            user = new ApplicationUser
            {
                FirstName = "fahed",
                LastName = "alobaidi",
                PhoneNumber = "12345678910",
                Email = adminEmail,
                UserName = adminEmail,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
            };

            await userManager.CreateAsync(user, adminPassword);
            
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}

app.UseMiddleware<GlobalExceptionHandliongMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors("souqify");

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
