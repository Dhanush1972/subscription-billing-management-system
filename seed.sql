-- =====================================================================
-- Subscription Billing System - Seed Data SQL Script
-- Idempotent manual seed script for MySQL / MariaDB (e.g. MonsterASP.NET)
-- Run this script in phpMyAdmin or MySQL client AFTER executing migrate.sql
-- =====================================================================

START TRANSACTION;

-- 1. Roles
INSERT INTO `Roles` (`Id`, `Name`)
SELECT 1, 'Admin'
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `Id` = 1 OR `Name` = 'Admin');

INSERT INTO `Roles` (`Id`, `Name`)
SELECT 2, 'Customer'
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `Id` = 2 OR `Name` = 'Customer');

-- 2. Default Administrator User (admin@subscriptionbilling.com / Admin@123456!)
-- BCrypt hash for "Admin@123456!": $2a$11$AHQtOFeJstSqXBAI7EZ5HeR9pooepcv7MSbEFt1F3V2SOQgyfrQjy
INSERT INTO `Users` (`Email`, `PasswordHash`, `FirstName`, `LastName`, `StripeCustomerId`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT 
    'admin@subscriptionbilling.com', 
    '$2a$11$AHQtOFeJstSqXBAI7EZ5HeR9pooepcv7MSbEFt1F3V2SOQgyfrQjy', 
    'System', 
    'Administrator', 
    'cus_admin_seed_0001', 
    1, 
    UTC_TIMESTAMP(), 
    NULL
WHERE NOT EXISTS (SELECT 1 FROM `Users` WHERE `Email` = 'admin@subscriptionbilling.com');

-- Assign Admin Role (RoleId = 1) to admin@subscriptionbilling.com
INSERT INTO `UserRoles` (`UserId`, `RoleId`)
SELECT u.`Id`, 1
FROM `Users` u
WHERE u.`Email` = 'admin@subscriptionbilling.com'
  AND NOT EXISTS (
      SELECT 1 FROM `UserRoles` ur 
      WHERE ur.`UserId` = u.`Id` AND ur.`RoleId` = 1
  );

-- 3. Subscription Plans
-- 3.1 Basic Plan
INSERT INTO `SubscriptionPlans` (`Id`, `Name`, `Description`, `Price`, `Currency`, `BillingInterval`, `StripeProductId`, `StripePriceId`, `IsActive`, `IsPopular`, `FeaturesJson`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT 
    1,
    'Basic', 
    'Suitable for individuals and small businesses getting started.', 
    499.00, 
    'INR', 
    'Monthly', 
    NULL, 
    NULL, 
    1, 
    0, 
    '[\"Maximum 1 User\",\"Maximum 5 Projects\",\"5 GB Storage\",\"Basic dashboard\",\"Basic reporting\",\"Email support\",\"Standard security\"]', 
    UTC_TIMESTAMP(), 
    NULL
WHERE NOT EXISTS (SELECT 1 FROM `SubscriptionPlans` WHERE `Name` = 'Basic');

-- 3.2 Professional Plan
INSERT INTO `SubscriptionPlans` (`Id`, `Name`, `Description`, `Price`, `Currency`, `BillingInterval`, `StripeProductId`, `StripePriceId`, `IsActive`, `IsPopular`, `FeaturesJson`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT 
    2,
    'Professional', 
    'Designed for growing teams that need advanced tools and collaboration.', 
    999.00, 
    'INR', 
    'Monthly', 
    NULL, 
    NULL, 
    1, 
    1, 
    '[\"Maximum 5 Users\",\"Maximum 25 Projects\",\"50 GB Storage\",\"Everything in Basic\",\"Advanced dashboard\",\"Advanced reporting\",\"Team collaboration\",\"Priority email support\",\"Data export\"]', 
    UTC_TIMESTAMP(), 
    NULL
WHERE NOT EXISTS (SELECT 1 FROM `SubscriptionPlans` WHERE `Name` = 'Professional');

-- 3.3 Enterprise Plan
INSERT INTO `SubscriptionPlans` (`Id`, `Name`, `Description`, `Price`, `Currency`, `BillingInterval`, `StripeProductId`, `StripePriceId`, `IsActive`, `IsPopular`, `FeaturesJson`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT 
    3,
    'Enterprise', 
    'Designed for larger organizations requiring advanced capabilities.', 
    2499.00, 
    'INR', 
    'Monthly', 
    NULL, 
    NULL, 
    1, 
    0, 
    '[\"Unlimited Users\",\"Unlimited Projects\",\"500 GB Storage\",\"Everything in Professional\",\"Advanced analytics\",\"Unlimited team members\",\"Unlimited projects\",\"Priority support\",\"Dedicated account support\",\"Custom reporting\"]', 
    UTC_TIMESTAMP(), 
    NULL
WHERE NOT EXISTS (SELECT 1 FROM `SubscriptionPlans` WHERE `Name` = 'Enterprise');

-- 4. Subscription Plan Prices (Monthly & Yearly)
-- Basic Prices
INSERT INTO `SubscriptionPlanPrices` (`SubscriptionPlanId`, `BillingInterval`, `Amount`, `Currency`, `StripePriceId`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Monthly', 499.00, 'INR', NULL, 1, UTC_TIMESTAMP(), NULL
FROM `SubscriptionPlans` p
WHERE p.`Name` = 'Basic'
  AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanPrices` spp WHERE spp.`SubscriptionPlanId` = p.`Id` AND spp.`BillingInterval` = 'Monthly');

INSERT INTO `SubscriptionPlanPrices` (`SubscriptionPlanId`, `BillingInterval`, `Amount`, `Currency`, `StripePriceId`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Yearly', 4990.00, 'INR', NULL, 1, UTC_TIMESTAMP(), NULL
FROM `SubscriptionPlans` p
WHERE p.`Name` = 'Basic'
  AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanPrices` spp WHERE spp.`SubscriptionPlanId` = p.`Id` AND spp.`BillingInterval` = 'Yearly');

-- Professional Prices
INSERT INTO `SubscriptionPlanPrices` (`SubscriptionPlanId`, `BillingInterval`, `Amount`, `Currency`, `StripePriceId`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Monthly', 999.00, 'INR', NULL, 1, UTC_TIMESTAMP(), NULL
FROM `SubscriptionPlans` p
WHERE p.`Name` = 'Professional'
  AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanPrices` spp WHERE spp.`SubscriptionPlanId` = p.`Id` AND spp.`BillingInterval` = 'Monthly');

INSERT INTO `SubscriptionPlanPrices` (`SubscriptionPlanId`, `BillingInterval`, `Amount`, `Currency`, `StripePriceId`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Yearly', 9990.00, 'INR', NULL, 1, UTC_TIMESTAMP(), NULL
FROM `SubscriptionPlans` p
WHERE p.`Name` = 'Professional'
  AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanPrices` spp WHERE spp.`SubscriptionPlanId` = p.`Id` AND spp.`BillingInterval` = 'Yearly');

-- Enterprise Prices
INSERT INTO `SubscriptionPlanPrices` (`SubscriptionPlanId`, `BillingInterval`, `Amount`, `Currency`, `StripePriceId`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Monthly', 2499.00, 'INR', NULL, 1, UTC_TIMESTAMP(), NULL
FROM `SubscriptionPlans` p
WHERE p.`Name` = 'Enterprise'
  AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanPrices` spp WHERE spp.`SubscriptionPlanId` = p.`Id` AND spp.`BillingInterval` = 'Monthly');

INSERT INTO `SubscriptionPlanPrices` (`SubscriptionPlanId`, `BillingInterval`, `Amount`, `Currency`, `StripePriceId`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Yearly', 24990.00, 'INR', NULL, 1, UTC_TIMESTAMP(), NULL
FROM `SubscriptionPlans` p
WHERE p.`Name` = 'Enterprise'
  AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanPrices` spp WHERE spp.`SubscriptionPlanId` = p.`Id` AND spp.`BillingInterval` = 'Yearly');

-- 5. Plan Features
-- Basic Features
INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'MaxUsers', '1', 1, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Basic'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'MaxUsers');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'MaxProjects', '5', 2, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Basic'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'MaxProjects');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Storage', '5 GB', 3, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Basic'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Storage');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Basic dashboard', 'Included', 4, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Basic'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Basic dashboard');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Basic reporting', 'Included', 5, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Basic'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Basic reporting');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Email support', 'Included', 6, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Basic'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Email support');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Standard security', 'Included', 7, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Basic'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Standard security');

-- Professional Features
INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'MaxUsers', '5', 1, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Professional'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'MaxUsers');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'MaxProjects', '25', 2, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Professional'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'MaxProjects');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Storage', '50 GB', 3, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Professional'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Storage');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Everything in Basic', 'Included', 4, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Professional'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Everything in Basic');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Advanced dashboard', 'Included', 5, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Professional'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Advanced dashboard');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Advanced reporting', 'Included', 6, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Professional'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Advanced reporting');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Team collaboration', 'Included', 7, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Professional'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Team collaboration');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Priority email support', 'Included', 8, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Professional'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Priority email support');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Data export', 'Included', 9, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Professional'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Data export');

-- Enterprise Features
INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'MaxUsers', 'Unlimited', 1, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Enterprise'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'MaxUsers');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'MaxProjects', 'Unlimited', 2, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Enterprise'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'MaxProjects');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Storage', '500 GB', 3, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Enterprise'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Storage');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Everything in Professional', 'Included', 4, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Enterprise'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Everything in Professional');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Advanced analytics', 'Included', 5, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Enterprise'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Advanced analytics');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Unlimited team members', 'Included', 6, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Enterprise'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Unlimited team members');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Unlimited projects', 'Included', 7, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Enterprise'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Unlimited projects');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Priority support', 'Included', 8, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Enterprise'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Priority support');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Dedicated account support', 'Included', 9, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Enterprise'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Dedicated account support');

INSERT INTO `SubscriptionPlanFeatures` (`SubscriptionPlanId`, `FeatureName`, `FeatureValue`, `DisplayOrder`, `CreatedAtUtc`, `UpdatedAtUtc`)
SELECT p.`Id`, 'Custom reporting', 'Included', 10, UTC_TIMESTAMP(), NULL FROM `SubscriptionPlans` p WHERE p.`Name` = 'Enterprise'
AND NOT EXISTS (SELECT 1 FROM `SubscriptionPlanFeatures` spf WHERE spf.`SubscriptionPlanId` = p.`Id` AND spf.`FeatureName` = 'Custom reporting');

COMMIT;
