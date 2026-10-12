using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Alfabe.Controllers.Api
{
    [ApiController]
    [Route("api/vbs/auth")]
    [Produces("application/json")]
    [Tags("VBS")]
    public class VbsAuthController : ControllerBase
    {
        private readonly AlfabeContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public VbsAuthController(
            AlfabeContext db,
            UserManager<AppUser> userManager,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _config = config;
            _env = env;
        }

        public class RegisterRequest
        {
            [Required, MinLength(3), MaxLength(150)]
            [RegularExpression(@"^[A-Za-zÇĞİÖŞÜçğıöşü\s]+$", ErrorMessage = "Ad Soyad yalnızca harf ve boşluk içerebilir.")]
            public string Name { get; set; } = "";

            [Required]
            [RegularExpression(@"^\d{11}$", ErrorMessage = "Telefon numarası 11 haneli olmalı ve sadece rakamlardan oluşmalıdır.")]
            public string PhoneNumber { get; set; } = "";

            [Required, EmailAddress, MaxLength(100)]
            public string Email { get; set; } = "";

            [Required]
            public string UserType { get; set; } = ""; // parent | teacher

            [Required, MinLength(6)]
            public string Password { get; set; } = "";
        }

        public class LoginRequest
        {
            [Required, EmailAddress]
            public string Email { get; set; } = "";

            [Required, MinLength(6)]
            public string Password { get; set; } = "";
        }

        private static string NormalizeEmail(string? email) =>
            (email ?? "").Trim().ToLowerInvariant();

        private static bool IsParentOrTeacher(IList<string> roles) =>
            roles.Contains("Parent") || roles.Contains("Teacher");

        private string GetSessionSecret()
        {
            var s = _config["Jwt:SessionSecret"];
            if (!string.IsNullOrWhiteSpace(s)) return s;
            return "alfabe-dev-session-secret-change-me";
        }

        private int GetSessionDays()
        {
            if (int.TryParse(_config["Jwt:SessionDays"], out var days) && days > 0)
                return days;
            return 7;
        }

        private void IssueSessionCookie(AppUser user, IList<string> roles)
        {
            var secret = GetSessionSecret();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
        new Claim("name", user.AdSoyad ?? ""),
        new Claim("uid", user.Id),
    };

            foreach (var r in roles)
                claims.Add(new Claim("roles", r));

            if (!string.IsNullOrWhiteSpace(user.SessionVersion))
                claims.Add(new Claim("session_ver", user.SessionVersion));

            if (!string.IsNullOrWhiteSpace(user.SessionVersion))
                claims.Add(new Claim("session_ver", user.SessionVersion));

            var now = DateTimeOffset.UtcNow;
            var jwtExpires = now.AddDays(GetSessionDays());

            var issuer = _config["Jwt:Issuer"] ?? "alfabe";
            var audience = _config["Jwt:Audience"] ?? "vbs";

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: jwtExpires.UtcDateTime,
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = jwtExpires.UtcDateTime
            };

            var roleCookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = jwtExpires.UtcDateTime
            };

            Response.Cookies.Append("vbs_session", jwt, cookieOptions);

            Response.Cookies.Append(
                "vbs_role",
                roles.Contains("Teacher") ? "Teacher" :
                roles.Contains("Parent") ? "Parent" : "",
                roleCookieOptions
            );
        }

        private void ClearSessionCookie()
        {
            var clearOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                SameSite = SameSiteMode.None,
                Secure = true,
                Path = "/"
            };

            Response.Cookies.Append("vbs_session", "", clearOptions);
            Response.Cookies.Append("vbs_role", "", clearOptions);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Geçersiz kayıt verisi." });

            var email = NormalizeEmail(req.Email);
            var phoneRaw = (req.PhoneNumber ?? "").Trim();
            var userTypeRaw = (req.UserType ?? "").Trim();

            if (string.IsNullOrWhiteSpace(phoneRaw))
                return BadRequest(new { error = "Telefon numarası zorunludur." });

            var userTypeLower = userTypeRaw.ToLowerInvariant();
            if (userTypeLower != "parent" && userTypeLower != "teacher")
                return BadRequest(new { error = "Geçersiz kullanıcı tipi (parent/teacher)." });

            var existsEmailInVbs = await _db.VbsUsers.AnyAsync(x => x.Email == email);
            var existsEmailInIdentity = await _db.Users.AnyAsync(x => x.Email == email);
            var existsPhoneInVbs = await _db.VbsUsers.AnyAsync(x => x.PhoneNumber == phoneRaw);

            if (existsEmailInVbs || existsEmailInIdentity || existsPhoneInVbs)
            {
                return Conflict(new
                {
                    error = "Bu telefon veya e-posta ile daha önce başvuru yapılmış."
                });
            }

            var tempUser = new AppUser
            {
                UserName = email,
                Email = email
            };

            var passwordHash = _userManager.PasswordHasher.HashPassword(tempUser, req.Password);

            var now = DateTime.UtcNow;

            var vbsUser = new VbsUser
            {
                FullName = (req.Name ?? "").Trim(),
                PhoneNumber = phoneRaw,
                Email = email,
                UserType = userTypeLower,
                PasswordHash = passwordHash,
                IsApproved = false,
                CreatedAt = now,
                ApprovedAt = null
            };

            _db.VbsUsers.Add(vbsUser);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Başvurunuz alınmıştır. Admin onayından sonra sisteme giriş yapabileceksiniz."
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Geçersiz giriş verisi." });

            var email = NormalizeEmail(req.Email);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return Unauthorized(new { error = "Kullanıcı bulunamadı veya şifre hatalı." });

            var ok = await _userManager.CheckPasswordAsync(user, req.Password);
            if (!ok)
                return Unauthorized(new { error = "Kullanıcı bulunamadı veya şifre hatalı." });

            var roles = await _userManager.GetRolesAsync(user);
            if (!IsParentOrTeacher(roles))
                return StatusCode(403, new { error = "Bu ekran yalnızca Veli/Öğretmen içindir." });

            var hasApprovedVbs = await _db.VbsUsers
                .AnyAsync(x => x.Email.ToLower() == email && x.IsApproved);

            if (!hasApprovedVbs)
                return Unauthorized(new { error = "Başvurunuz henüz onaylanmamış." });

            int? teacherNumericId = null;

            if (roles.Contains("Teacher"))
            {
                var teacherRow = await _db.Ogretmenler
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.AppUserId == user.Id);

                if (teacherRow != null)
                    teacherNumericId = teacherRow.Id;
            }

            var now = DateTime.UtcNow;
            user.SessionVersion = Guid.NewGuid().ToString("N");
            user.SessionExpiresAt = now.AddMinutes(30);

            await _userManager.UpdateAsync(user);

            IssueSessionCookie(user, roles);

            return Ok(new
            {
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    name = user.AdSoyad,
                    roles,
                    teacherNumericId = teacherNumericId,

                    sessionVersion = user.SessionVersion,
                    sessionExpiresAt = user.SessionExpiresAt
                }
            });

        }
    }
}
