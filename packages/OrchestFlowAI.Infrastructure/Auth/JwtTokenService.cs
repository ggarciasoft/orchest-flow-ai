using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OrchestFlowAI.Domain.Entities;
namespace OrchestFlowAI.Infrastructure.Auth;

/// <summary>
/// Generates signed JWT tokens for authenticated users.
/// Tokens are valid for 8 hours and include standard claims for identity, role, and tenant.
/// </summary>
public sealed class JwtTokenService
{
    /// <summary>
    /// Generates a signed HS256 JWT token for the given user.
    /// The token includes subject, email, role, tenant_id, and display_name claims.
    /// </summary>
    /// <param name="user">The authenticated user to generate a token for.</param>
    /// <param name="jwtKey">The HMAC signing secret. Must be at least 32 characters.</param>
    /// <param name="issuer">The token issuer claim (typically the API service name).</param>
    /// <param name="audience">The intended audience for the token (typically the frontend app).</param>
    /// <returns>A signed JWT token string valid for 8 hours.</returns>
    public string GenerateToken(User user, string jwtKey, string issuer, string audience)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Include all claims needed by middleware for tenant isolation and role authorization
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("tenant_id", user.TenantId.ToString()),
            new Claim("display_name", user.DisplayName)
        };
        var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
