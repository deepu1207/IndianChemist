using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using IndianChemist.Models;
using IndianChemist.Repositories;
namespace IndianChemist.Services;

public class AuthService(JsonFileRepository db, IConfiguration configuration, IWebHostEnvironment environment)
{
    readonly PasswordHasher<Account> hasher = new();
    string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
    string Random() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    IActionResult Error(string s, int code = 400) => new ObjectResult(new { message = s }) { StatusCode = code };
    IActionResult Issue(Account a, HttpContext c)
    {
        var jwt = new JwtSecurityToken("IndianChemist", "IndianChemist", [new(ClaimTypes.NameIdentifier, a.Id), new(ClaimTypes.Name, a.Name), new(ClaimTypes.Role, a.Role)], expires: DateTime.UtcNow.AddMinutes(15), signingCredentials: new(new SymmetricSecurityKey(db.Key), SecurityAlgorithms.HmacSha256));
        var raw = Random();
        a.RefreshHash = Hash(raw);
        a.RefreshExpires = DateTime.UtcNow.AddDays(7);
        db.Save();
        c.Response.Cookies.Append("ic_refresh", raw, new()
        {
            HttpOnly = true,
            Secure = c.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = a.RefreshExpires
        });
        return new OkObjectResult(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
            user = new
            {
                a.Name,
                a.Email,
                a.Role
            }
        });
    }

    public async Task<IActionResult> RegisterAsync(Register r, HttpContext c)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Length > 100 || string.IsNullOrWhiteSpace(r.Email) || !new EmailAddressAttribute().IsValid(r.Email) || string.IsNullOrEmpty(r.Password) || r.Password.Length < 10 || r.Password.Length > 128)
            return Error("Enter a name, valid email, and password of 10–128 characters.");
        var origin = configuration["PublicOrigin"] ?? (environment.IsDevelopment() ? $"{c.Request.Scheme}://{c.Request.Host}" : null);
        if (origin is null)
            return Error("PublicOrigin must be configured.", 503);
        if (!environment.IsDevelopment() && string.IsNullOrEmpty(configuration["Smtp:Host"]))
            return Error("Email delivery is not configured.", 503);
        Account a;
        var raw = Random();
        lock (db.Sync)
        {
            if (db.Data.Accounts.Any(a => a.Email.Equals(r.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Error("This email is already registered.");
            a = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = r.Name.Trim(),
                Email = r.Email.Trim().ToLowerInvariant(),
                VerificationHash = Hash(raw),
                VerificationExpires = DateTime.UtcNow.AddHours(24)
            };
            if (a.Email.Equals(configuration["Admin:Email"], StringComparison.OrdinalIgnoreCase))
                a.Role = "Admin";
            a.PasswordHash = hasher.HashPassword(a, r.Password);
            db.Data.Accounts.Add(a);
            db.Save();
        }
        var link = $"{origin.TrimEnd('/')}/api/auth/verify?token={raw}";
        if (!string.IsNullOrWhiteSpace(configuration["Smtp:Host"]))
        {
            try
            {
                using var smtp = new SmtpClient(configuration["Smtp:Host"], configuration.GetValue("Smtp:Port", 587)) { EnableSsl = true, Credentials = new NetworkCredential(configuration["Smtp:Username"], configuration["Smtp:Password"]) };
                using var mail = new MailMessage(configuration["Smtp:From"]!, a.Email, "Verify your Indian Chemist account", $"Confirm your email within 24 hours: {link}");
                await smtp.SendMailAsync(mail);
            }
            catch { lock (db.Sync) { db.Data.Accounts.Remove(a); db.Save(); } return Error("Email could not be sent. Check SMTP settings and register again.", 503); }
        }
        else if (environment.IsDevelopment())
            return new OkObjectResult(new
            {
                message = "Account created. Open the development email below to verify your address.",
                verificationUrl = link
            });
        return new OkObjectResult(new
        {
            message = "Check your email to verify your account before signing in."
        });
    }
    public IActionResult Verify(string token)
    {
        lock (db.Sync)
        {
            var a = db.Data.Accounts.FirstOrDefault(a => a.VerificationHash == Hash(token) && a.VerificationExpires > DateTime.UtcNow);
            if (a is null)
                return Error("Verification link is invalid or expired.");
            a.Verified = true;
            a.VerificationHash = null;
            db.Save();
            return new RedirectResult("/?verified=1");
        }
    }
    public IActionResult Login(Login r, HttpContext c)
    {
        lock (db.Sync)
        {
            if (string.IsNullOrWhiteSpace(r.Email) || string.IsNullOrEmpty(r.Password))
                return Error("Email and password are required.");
            var a = db.Data.Accounts.FirstOrDefault(a => a.Email.Equals(r.Email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (a is null || hasher.VerifyHashedPassword(a, a.PasswordHash, r.Password) == PasswordVerificationResult.Failed)
                return Error("Incorrect email or password.", 401);
            if (!a.Verified)
                return Error("Verify your email before signing in.", 403);
            return Issue(a, c);
        }
    }
    public IActionResult Refresh(HttpContext c)
    {
        lock (db.Sync)
        {
            var raw = c.Request.Cookies["ic_refresh"];
            var a = raw is null ? null : db.Data.Accounts.FirstOrDefault(a => a.RefreshHash == Hash(raw) && a.RefreshExpires > DateTime.UtcNow && a.Verified);
            return a is null ? Error("Please sign in again.", 401) : Issue(a, c);
        }
    }
    public IActionResult Logout(HttpContext c)
    {
        lock (db.Sync)
        {
            var raw = c.Request.Cookies["ic_refresh"];
            var a = raw is null ? null : db.Data.Accounts.FirstOrDefault(a => a.RefreshHash == Hash(raw));
            if (a is not null)
            {
                a.RefreshHash = null;
                db.Save();
            }
            c.Response.Cookies.Delete("ic_refresh", new CookieOptions { Path = "/api/auth" });
            return new OkResult();
        }
    }
}
