using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Auth;
using SubscriptionBilling.Application.DTOs.Users;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Exceptions;

namespace SubscriptionBilling.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly IAuditLogService _auditLogService;

    public AuthService(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IStripePaymentGateway stripeGateway,
        IAuditLogService auditLogService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _stripeGateway = stripeGateway;
        _auditLogService = auditLogService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
        {
            throw new BusinessRuleValidationException("A user with this email address already exists.");
        }

        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
        if (customerRole == null)
        {
            customerRole = new Role { Name = "Customer" };
            await _context.Roles.AddAsync(customerRole);
            await _context.SaveChangesAsync();
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Create Stripe Customer
        try
        {
            user.StripeCustomerId = await _stripeGateway.CreateCustomerAsync(user);
        }
        catch
        {
            // Fallback for offline/simulated mode
            user.StripeCustomerId = $"cus_local_{Guid.NewGuid():N}";
        }

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        await _context.UserRoles.AddAsync(new UserRole
        {
            UserId = user.Id,
            RoleId = customerRole.Id
        });
        await _context.SaveChangesAsync();

        var roles = new[] { "Customer" };
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, ipAddress);

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(user.Id, "REGISTER", "User", user.Id.ToString(), new { user.Email }, ipAddress);

        var profile = new UserProfileDto(user.Id, user.Email, user.FirstName, user.LastName, user.StripeCustomerId, roles);
        return new AuthResponse(accessToken, refreshToken.Token, refreshToken.ExpiresAtUtc, profile);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new BusinessRuleValidationException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new BusinessRuleValidationException("This user account has been deactivated. Please contact support.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, ipAddress);

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(user.Id, "LOGIN", "User", user.Id.ToString(), null, ipAddress);

        var profile = new UserProfileDto(user.Id, user.Email, user.FirstName, user.LastName, user.StripeCustomerId, roles);
        return new AuthResponse(accessToken, refreshToken.Token, refreshToken.ExpiresAtUtc, profile);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token, string? ipAddress = null)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            throw new BusinessRuleValidationException("Invalid or expired refresh token.");
        }

        var user = refreshToken.User;
        if (!user.IsActive)
        {
            throw new BusinessRuleValidationException("User account is inactive.");
        }

        // Rotate token
        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;

        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, ipAddress);
        refreshToken.ReplacedByToken = newRefreshToken.Token;

        await _context.RefreshTokens.AddAsync(newRefreshToken);
        await _context.SaveChangesAsync();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles);

        var profile = new UserProfileDto(user.Id, user.Email, user.FirstName, user.LastName, user.StripeCustomerId, roles);
        return new AuthResponse(newAccessToken, newRefreshToken.Token, newRefreshToken.ExpiresAtUtc, profile);
    }

    public async Task RevokeTokenAsync(string token, string? ipAddress = null)
    {
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.RevokedAtUtc = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return new UserProfileDto(user.Id, user.Email, user.FirstName, user.LastName, user.StripeCustomerId, roles);
    }

    public async Task<PagedResult<UserManagementDto>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserManagementDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.StripeCustomerId,
                u.IsActive,
                u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                u.CreatedAtUtc
            ))
            .ToListAsync();

        return new PagedResult<UserManagementDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task UpdateUserStatusAsync(int userId, bool isActive, int adminUserId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        user.IsActive = isActive;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(adminUserId, "UPDATE_USER_STATUS", "User", userId.ToString(), new { IsActive = isActive });
    }
}
