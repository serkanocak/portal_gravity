using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PortalGravity.Module.Identity;

public static class AuthEndpoints
{
    public record LoginRequest(string TenantSlug, string Email, string Password);
    public record TokenResponse(string AccessToken, string RefreshToken);

    public static async Task<IResult> Login(
        [FromBody] LoginRequest req,
        [FromServices] IAuthService authService,
        CancellationToken ct)
    {
        try
        {
            var result = await authService.LoginAsync(req.TenantSlug, req.Email, req.Password, ct);
            return Results.Ok(new TokenResponse(result.AccessToken, result.RefreshToken));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    public record RefreshRequest(string AccessToken, string RefreshToken);

    public static async Task<IResult> Refresh(
        [FromBody] RefreshRequest req,
        [FromServices] IAuthService authService,
        CancellationToken ct)
    {
        try
        {
            var result = await authService.RefreshTokenAsync(req.AccessToken, req.RefreshToken, ct);
            return Results.Ok(new TokenResponse(result.AccessToken, result.RefreshToken));
        }
        catch (Exception ex)
        {
            return Results.Unauthorized();
        }
    }

    public record ImpersonateRequest(string TargetTenantSlug, string TargetUserEmail);

    public static async Task<IResult> Impersonate(
        [FromBody] ImpersonateRequest req,
        [FromServices] IAuthService authService,
        CancellationToken ct)
    {
        try
        {
            var result = await authService.ImpersonateAsync(req.TargetTenantSlug, req.TargetUserEmail, ct);
            return Results.Ok(new TokenResponse(result.AccessToken, result.RefreshToken));
        }
        catch (Exception ex)
        {
            return Results.Forbid();
        }
    }

    public static async Task<IResult> StopImpersonate(
        [FromServices] IAuthService authService,
        CancellationToken ct)
    {
        try
        {
            var result = await authService.StopImpersonationAsync(ct);
            return Results.Ok(new TokenResponse(result.AccessToken, result.RefreshToken));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
