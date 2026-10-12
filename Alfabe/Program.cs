using Alfabe.Data;
using Alfabe.Helpers;
using Alfabe.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CookieSameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var configuration = builder.Configuration;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                      ?? Array.Empty<string>();

builder.Services.AddDbContext<AlfabeContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.User.RequireUniqueEmail = true;

    
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ğĞüÜşŞıİöÖçÇ";
})
.AddEntityFrameworkStores<AlfabeContext>()
.AddErrorDescriber<TurkishIdentityErrorDescriber>()
.AddDefaultTokenProviders();

var jwtSection = configuration.GetSection("Jwt");
var sessionSecret = jwtSection["SessionSecret"];

if (string.IsNullOrWhiteSpace(sessionSecret))
    throw new Exception("SESSION_SECRET boş olamaz!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer("VbsJwt", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sessionSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2),
        NameClaimType = "name",
        RoleClaimType = "roles"
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (ctx.Request.Cookies.TryGetValue("vbs_session", out var t))
                ctx.Token = t;

            return Task.CompletedTask;
        }
    };
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = CookieSameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        },

        OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = 403;
                return Task.CompletedTask;
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontends", p =>
    {
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Alfabe API",
        Version = "v1"
    });

    c.MapType<IFormFile>(() =>
        new OpenApiSchema
        {
            Type = "string",
            Format = "binary"
        });
});

var app = builder.Build();

var fwd = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
fwd.KnownNetworks.Clear();
fwd.KnownProxies.Clear();
app.UseForwardedHeaders(fwd);

app.Use((ctx, next) =>
{
    if (ctx.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) && proto == "https")
        ctx.Request.Scheme = "https";

    return next();
});

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self' 'unsafe-inline' https:; style-src 'self' 'unsafe-inline' https:; img-src 'self' data: https:; connect-src 'self' https: http:;";
    await next();
});

if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

var uploadsPath = Path.Combine(env.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true
});

app.UseStaticFiles();
app.UseRouting();
app.UseCors("Frontends");

app.UseAuthentication();

app.Use(async (context, next) =>
{
    try
    {
        if (!context.Request.Path.StartsWithSegments("/api/vbs"))
        {
            await next();
            return;
        }

        if (!context.Request.Cookies.TryGetValue("vbs_session", out var token) ||
            string.IsNullOrWhiteSpace(token))
        {
            await next();
            return;
        }

        JwtSecurityToken jwt = null;
        try
        {
            jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        }
        catch
        {
            context.Response.StatusCode = 401;
            return;
        }

        var userId =
            jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ??
            jwt.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;

        var sessionVer = jwt.Claims.FirstOrDefault(c => c.Type == "session_ver")?.Value;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionVer))
        {
            await next();
            return;
        }

        try
        {
            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AlfabeContext>();

            var user = await db.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                context.Response.StatusCode = 401;
                return;
            }

            if (!string.Equals(user.SessionVersion, sessionVer))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var now = DateTime.UtcNow;

            if (user.SessionExpiresAt.HasValue &&
                user.SessionExpiresAt.Value < now)
            {
                context.Response.StatusCode = 401;
                return;
            }

            
            if (user.SessionExpiresAt.HasValue)
            {
                if (user.SessionExpiresAt.Value > now)
                {
                    user.SessionExpiresAt = now.AddMinutes(30);
                    await db.SaveChangesAsync(); // SQL injection riski yok → EF parametreli sorgu üretir
                }
            }
        }
        catch
        {
        }

        await next();
    }
    catch
    {
        await next(); // hiçbir durumda throw edilmez, production güvenli
    }
});

app.UseAuthorization();

app.MapControllers();

app.MapGet("/api", () => new { status = "OK" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
