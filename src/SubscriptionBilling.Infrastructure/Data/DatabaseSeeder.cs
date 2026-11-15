using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Enums;
using SubscriptionBilling.Infrastructure.Services;
using System.Text.Json;

namespace SubscriptionBilling.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(SubscriptionBillingDbContext context, IPasswordHasher? passwordHasher = null)
    {
        // 1. Roles
        if (!await context.Roles.AnyAsync())
        {
            var adminRole = new Role { Id = 1, Name = "Admin" };
            var customerRole = new Role { Id = 2, Name = "Customer" };
            await context.Roles.AddRangeAsync(adminRole, customerRole);
            await context.SaveChangesAsync();
        }

        // 2. Default Admin User (if none exists)
        await SeedAdminUserAsync(context, passwordHasher);

        // 3. Sample Subscription Plans & Normalized Prices / Features
        await SeedSubscriptionPlansAsync(context);
    }

    private static async Task SeedAdminUserAsync(SubscriptionBillingDbContext context, IPasswordHasher? passwordHasher)
    {
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            adminRole = new Role { Name = "Admin" };
            await context.Roles.AddAsync(adminRole);
            await context.SaveChangesAsync();
        }

        var hasAdmin = await context.UserRoles.AnyAsync(ur => ur.RoleId == adminRole.Id);
        if (!hasAdmin)
        {
            var hasher = passwordHasher ?? new PasswordHasher();
            const string adminEmail = "admin@subscriptionbilling.com";

            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == adminEmail);
            if (existingUser == null)
            {
                existingUser = new User
                {
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    PasswordHash = hasher.HashPassword("Admin@123456!"),
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    StripeCustomerId = $"cus_admin_{Guid.NewGuid():N}"
                };
                await context.Users.AddAsync(existingUser);
                await context.SaveChangesAsync();
            }

            if (!await context.UserRoles.AnyAsync(ur => ur.UserId == existingUser.Id && ur.RoleId == adminRole.Id))
            {
                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = existingUser.Id,
                    RoleId = adminRole.Id
                });
                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedSubscriptionPlansAsync(SubscriptionBillingDbContext context)
    {
        // 1. Basic Plan
        var basicPlan = await context.SubscriptionPlans
            .Include(p => p.Prices)
            .Include(p => p.PlanFeatures)
            .FirstOrDefaultAsync(p => p.Name == "Basic");

        if (basicPlan == null)
        {
            basicPlan = new SubscriptionPlan
            {
                Name = "Basic",
                Description = "Suitable for individuals and small businesses getting started.",
                Price = 499.00m,
                Currency = "INR",
                BillingInterval = BillingInterval.Monthly,
                IsActive = true,
                IsPopular = false,
                FeaturesJson = JsonSerializer.Serialize(new[]
                {
                    "Maximum 1 User",
                    "Maximum 5 Projects",
                    "5 GB Storage",
                    "Basic dashboard",
                    "Basic reporting",
                    "Email support",
                    "Standard security"
                }),
                CreatedAtUtc = DateTime.UtcNow
            };

            await context.SubscriptionPlans.AddAsync(basicPlan);
            await context.SaveChangesAsync();
        }

        // Ensure Monthly & Yearly Prices for Basic Plan
        if (!await context.SubscriptionPlanPrices.AnyAsync(p => p.SubscriptionPlanId == basicPlan.Id && p.BillingInterval == BillingInterval.Monthly))
        {
            context.SubscriptionPlanPrices.Add(new SubscriptionPlanPrice
            {
                SubscriptionPlanId = basicPlan.Id,
                BillingInterval = BillingInterval.Monthly,
                Amount = 499.00m,
                Currency = "INR",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (!await context.SubscriptionPlanPrices.AnyAsync(p => p.SubscriptionPlanId == basicPlan.Id && p.BillingInterval == BillingInterval.Yearly))
        {
            context.SubscriptionPlanPrices.Add(new SubscriptionPlanPrice
            {
                SubscriptionPlanId = basicPlan.Id,
                BillingInterval = BillingInterval.Yearly,
                Amount = 4990.00m,
                Currency = "INR",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // Ensure Features for Basic Plan
        if (!await context.SubscriptionPlanFeatures.AnyAsync(f => f.SubscriptionPlanId == basicPlan.Id))
        {
            var basicFeatures = new[]
            {
                new SubscriptionPlanFeature { SubscriptionPlanId = basicPlan.Id, FeatureName = "MaxUsers", FeatureValue = "1", DisplayOrder = 1 },
                new SubscriptionPlanFeature { SubscriptionPlanId = basicPlan.Id, FeatureName = "MaxProjects", FeatureValue = "5", DisplayOrder = 2 },
                new SubscriptionPlanFeature { SubscriptionPlanId = basicPlan.Id, FeatureName = "Storage", FeatureValue = "5 GB", DisplayOrder = 3 },
                new SubscriptionPlanFeature { SubscriptionPlanId = basicPlan.Id, FeatureName = "Basic dashboard", FeatureValue = "Included", DisplayOrder = 4 },
                new SubscriptionPlanFeature { SubscriptionPlanId = basicPlan.Id, FeatureName = "Basic reporting", FeatureValue = "Included", DisplayOrder = 5 },
                new SubscriptionPlanFeature { SubscriptionPlanId = basicPlan.Id, FeatureName = "Email support", FeatureValue = "Included", DisplayOrder = 6 },
                new SubscriptionPlanFeature { SubscriptionPlanId = basicPlan.Id, FeatureName = "Standard security", FeatureValue = "Included", DisplayOrder = 7 }
            };
            await context.SubscriptionPlanFeatures.AddRangeAsync(basicFeatures);
        }

        // 2. Professional Plan (IsPopular = true)
        var proPlan = await context.SubscriptionPlans
            .Include(p => p.Prices)
            .Include(p => p.PlanFeatures)
            .FirstOrDefaultAsync(p => p.Name == "Professional");

        if (proPlan == null)
        {
            proPlan = new SubscriptionPlan
            {
                Name = "Professional",
                Description = "Designed for growing teams that need advanced tools and collaboration.",
                Price = 999.00m,
                Currency = "INR",
                BillingInterval = BillingInterval.Monthly,
                IsActive = true,
                IsPopular = true, // Popular plan indicator via database seeding
                FeaturesJson = JsonSerializer.Serialize(new[]
                {
                    "Maximum 5 Users",
                    "Maximum 25 Projects",
                    "50 GB Storage",
                    "Everything in Basic",
                    "Advanced dashboard",
                    "Advanced reporting",
                    "Team collaboration",
                    "Priority email support",
                    "Data export"
                }),
                CreatedAtUtc = DateTime.UtcNow
            };

            await context.SubscriptionPlans.AddAsync(proPlan);
            await context.SaveChangesAsync();
        }
        else if (!proPlan.IsPopular)
        {
            proPlan.IsPopular = true;
            proPlan.Price = 999.00m;
            proPlan.Currency = "INR";
        }

        // Ensure Monthly & Yearly Prices for Professional Plan
        if (!await context.SubscriptionPlanPrices.AnyAsync(p => p.SubscriptionPlanId == proPlan.Id && p.BillingInterval == BillingInterval.Monthly))
        {
            context.SubscriptionPlanPrices.Add(new SubscriptionPlanPrice
            {
                SubscriptionPlanId = proPlan.Id,
                BillingInterval = BillingInterval.Monthly,
                Amount = 999.00m,
                Currency = "INR",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (!await context.SubscriptionPlanPrices.AnyAsync(p => p.SubscriptionPlanId == proPlan.Id && p.BillingInterval == BillingInterval.Yearly))
        {
            context.SubscriptionPlanPrices.Add(new SubscriptionPlanPrice
            {
                SubscriptionPlanId = proPlan.Id,
                BillingInterval = BillingInterval.Yearly,
                Amount = 9990.00m,
                Currency = "INR",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // Ensure Features for Professional Plan
        if (!await context.SubscriptionPlanFeatures.AnyAsync(f => f.SubscriptionPlanId == proPlan.Id))
        {
            var proFeatures = new[]
            {
                new SubscriptionPlanFeature { SubscriptionPlanId = proPlan.Id, FeatureName = "MaxUsers", FeatureValue = "5", DisplayOrder = 1 },
                new SubscriptionPlanFeature { SubscriptionPlanId = proPlan.Id, FeatureName = "MaxProjects", FeatureValue = "25", DisplayOrder = 2 },
                new SubscriptionPlanFeature { SubscriptionPlanId = proPlan.Id, FeatureName = "Storage", FeatureValue = "50 GB", DisplayOrder = 3 },
                new SubscriptionPlanFeature { SubscriptionPlanId = proPlan.Id, FeatureName = "Everything in Basic", FeatureValue = "Included", DisplayOrder = 4 },
                new SubscriptionPlanFeature { SubscriptionPlanId = proPlan.Id, FeatureName = "Advanced dashboard", FeatureValue = "Included", DisplayOrder = 5 },
                new SubscriptionPlanFeature { SubscriptionPlanId = proPlan.Id, FeatureName = "Advanced reporting", FeatureValue = "Included", DisplayOrder = 6 },
                new SubscriptionPlanFeature { SubscriptionPlanId = proPlan.Id, FeatureName = "Team collaboration", FeatureValue = "Included", DisplayOrder = 7 },
                new SubscriptionPlanFeature { SubscriptionPlanId = proPlan.Id, FeatureName = "Priority email support", FeatureValue = "Included", DisplayOrder = 8 },
                new SubscriptionPlanFeature { SubscriptionPlanId = proPlan.Id, FeatureName = "Data export", FeatureValue = "Included", DisplayOrder = 9 }
            };
            await context.SubscriptionPlanFeatures.AddRangeAsync(proFeatures);
        }

        // 3. Enterprise Plan
        var enterprisePlan = await context.SubscriptionPlans
            .Include(p => p.Prices)
            .Include(p => p.PlanFeatures)
            .FirstOrDefaultAsync(p => p.Name == "Enterprise");

        if (enterprisePlan == null)
        {
            enterprisePlan = new SubscriptionPlan
            {
                Name = "Enterprise",
                Description = "Designed for larger organizations requiring advanced capabilities.",
                Price = 2499.00m,
                Currency = "INR",
                BillingInterval = BillingInterval.Monthly,
                IsActive = true,
                IsPopular = false,
                FeaturesJson = JsonSerializer.Serialize(new[]
                {
                    "Unlimited Users",
                    "Unlimited Projects",
                    "500 GB Storage",
                    "Everything in Professional",
                    "Advanced analytics",
                    "Unlimited team members",
                    "Unlimited projects",
                    "Priority support",
                    "Dedicated account support",
                    "Custom reporting"
                }),
                CreatedAtUtc = DateTime.UtcNow
            };

            await context.SubscriptionPlans.AddAsync(enterprisePlan);
            await context.SaveChangesAsync();
        }

        // Ensure Monthly & Yearly Prices for Enterprise Plan
        if (!await context.SubscriptionPlanPrices.AnyAsync(p => p.SubscriptionPlanId == enterprisePlan.Id && p.BillingInterval == BillingInterval.Monthly))
        {
            context.SubscriptionPlanPrices.Add(new SubscriptionPlanPrice
            {
                SubscriptionPlanId = enterprisePlan.Id,
                BillingInterval = BillingInterval.Monthly,
                Amount = 2499.00m,
                Currency = "INR",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (!await context.SubscriptionPlanPrices.AnyAsync(p => p.SubscriptionPlanId == enterprisePlan.Id && p.BillingInterval == BillingInterval.Yearly))
        {
            context.SubscriptionPlanPrices.Add(new SubscriptionPlanPrice
            {
                SubscriptionPlanId = enterprisePlan.Id,
                BillingInterval = BillingInterval.Yearly,
                Amount = 24990.00m,
                Currency = "INR",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // Ensure Features for Enterprise Plan
        if (!await context.SubscriptionPlanFeatures.AnyAsync(f => f.SubscriptionPlanId == enterprisePlan.Id))
        {
            var enterpriseFeatures = new[]
            {
                new SubscriptionPlanFeature { SubscriptionPlanId = enterprisePlan.Id, FeatureName = "MaxUsers", FeatureValue = "Unlimited", DisplayOrder = 1 },
                new SubscriptionPlanFeature { SubscriptionPlanId = enterprisePlan.Id, FeatureName = "MaxProjects", FeatureValue = "Unlimited", DisplayOrder = 2 },
                new SubscriptionPlanFeature { SubscriptionPlanId = enterprisePlan.Id, FeatureName = "Storage", FeatureValue = "500 GB", DisplayOrder = 3 },
                new SubscriptionPlanFeature { SubscriptionPlanId = enterprisePlan.Id, FeatureName = "Everything in Professional", FeatureValue = "Included", DisplayOrder = 4 },
                new SubscriptionPlanFeature { SubscriptionPlanId = enterprisePlan.Id, FeatureName = "Advanced analytics", FeatureValue = "Included", DisplayOrder = 5 },
                new SubscriptionPlanFeature { SubscriptionPlanId = enterprisePlan.Id, FeatureName = "Unlimited team members", FeatureValue = "Included", DisplayOrder = 6 },
                new SubscriptionPlanFeature { SubscriptionPlanId = enterprisePlan.Id, FeatureName = "Unlimited projects", FeatureValue = "Included", DisplayOrder = 7 },
                new SubscriptionPlanFeature { SubscriptionPlanId = enterprisePlan.Id, FeatureName = "Priority support", FeatureValue = "Included", DisplayOrder = 8 },
                new SubscriptionPlanFeature { SubscriptionPlanId = enterprisePlan.Id, FeatureName = "Dedicated account support", FeatureValue = "Included", DisplayOrder = 9 },
                new SubscriptionPlanFeature { SubscriptionPlanId = enterprisePlan.Id, FeatureName = "Custom reporting", FeatureValue = "Included", DisplayOrder = 10 }
            };
            await context.SubscriptionPlanFeatures.AddRangeAsync(enterpriseFeatures);
        }

        await context.SaveChangesAsync();
    }
}
