CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `Roles` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_Roles` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `SubscriptionPlans` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Price` decimal(18,2) NOT NULL,
        `Currency` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
        `BillingInterval` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `StripeProductId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `StripePriceId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `FeaturesJson` longtext CHARACTER SET utf8mb4 NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_SubscriptionPlans` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `Users` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Email` varchar(256) CHARACTER SET utf8mb4 NOT NULL,
        `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
        `FirstName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `LastName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `StripeCustomerId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `WebhookEvents` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `StripeEventId` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `EventType` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `PayloadJson` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Status` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `ReceivedAtUtc` datetime(6) NOT NULL,
        `ProcessedAtUtc` datetime(6) NULL,
        `ErrorMessage` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_WebhookEvents` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `AuditLogs` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NULL,
        `Action` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `EntityName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `EntityId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `DetailsJson` longtext CHARACTER SET utf8mb4 NULL,
        `IpAddress` varchar(50) CHARACTER SET utf8mb4 NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_AuditLogs` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AuditLogs_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `RefreshTokens` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `Token` varchar(256) CHARACTER SET utf8mb4 NOT NULL,
        `ExpiresAtUtc` datetime(6) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `RevokedAtUtc` datetime(6) NULL,
        `ReplacedByToken` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedByIp` longtext CHARACTER SET utf8mb4 NULL,
        `RevokedByIp` longtext CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_RefreshTokens` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RefreshTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `Subscriptions` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `PlanId` int NOT NULL,
        `StripeSubscriptionId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Status` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `CurrentPeriodStartUtc` datetime(6) NOT NULL,
        `CurrentPeriodEndUtc` datetime(6) NOT NULL,
        `CancelAtPeriodEnd` tinyint(1) NOT NULL,
        `CanceledAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_Subscriptions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Subscriptions_SubscriptionPlans_PlanId` FOREIGN KEY (`PlanId`) REFERENCES `SubscriptionPlans` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Subscriptions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `UserRoles` (
        `UserId` int NOT NULL,
        `RoleId` int NOT NULL,
        CONSTRAINT `PK_UserRoles` PRIMARY KEY (`UserId`, `RoleId`),
        CONSTRAINT `FK_UserRoles_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_UserRoles_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `Invoices` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `SubscriptionId` int NULL,
        `InvoiceNumber` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `StripeInvoiceId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `BillingPeriod` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `InvoiceDateUtc` datetime(6) NOT NULL,
        `DueDateUtc` datetime(6) NOT NULL,
        `Subtotal` decimal(18,2) NOT NULL,
        `Tax` decimal(18,2) NOT NULL,
        `Total` decimal(18,2) NOT NULL,
        `Currency` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
        `Status` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `PdfFilePath` varchar(500) CHARACTER SET utf8mb4 NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_Invoices` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Invoices_Subscriptions_SubscriptionId` FOREIGN KEY (`SubscriptionId`) REFERENCES `Subscriptions` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_Invoices_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `PaymentReminders` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SubscriptionId` int NOT NULL,
        `ScheduledForUtc` datetime(6) NOT NULL,
        `SentAtUtc` datetime(6) NULL,
        `ReminderType` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `Status` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `ErrorMessage` longtext CHARACTER SET utf8mb4 NULL,
        `RetryCount` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_PaymentReminders` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_PaymentReminders_Subscriptions_SubscriptionId` FOREIGN KEY (`SubscriptionId`) REFERENCES `Subscriptions` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE TABLE `Payments` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `SubscriptionId` int NULL,
        `InvoiceId` int NULL,
        `StripePaymentIntentId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `StripeChargeId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Amount` decimal(18,2) NOT NULL,
        `Currency` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
        `PaymentMethodSummary` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Status` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `FailureMessage` longtext CHARACTER SET utf8mb4 NULL,
        `PaymentDateUtc` datetime(6) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_Payments` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Payments_Invoices_InvoiceId` FOREIGN KEY (`InvoiceId`) REFERENCES `Invoices` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_Payments_Subscriptions_SubscriptionId` FOREIGN KEY (`SubscriptionId`) REFERENCES `Subscriptions` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_Payments_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_AuditLogs_UserId` ON `AuditLogs` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Invoices_InvoiceNumber` ON `Invoices` (`InvoiceNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Invoices_StripeInvoiceId` ON `Invoices` (`StripeInvoiceId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_Invoices_SubscriptionId` ON `Invoices` (`SubscriptionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_Invoices_UserId` ON `Invoices` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_PaymentReminders_SubscriptionId` ON `PaymentReminders` (`SubscriptionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_Payments_InvoiceId` ON `Payments` (`InvoiceId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Payments_StripePaymentIntentId` ON `Payments` (`StripePaymentIntentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_Payments_SubscriptionId` ON `Payments` (`SubscriptionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_Payments_UserId` ON `Payments` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_RefreshTokens_Token` ON `RefreshTokens` (`Token`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_RefreshTokens_UserId` ON `RefreshTokens` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Roles_Name` ON `Roles` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_SubscriptionPlans_StripePriceId` ON `SubscriptionPlans` (`StripePriceId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_Subscriptions_PlanId` ON `Subscriptions` (`PlanId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Subscriptions_StripeSubscriptionId` ON `Subscriptions` (`StripeSubscriptionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_Subscriptions_UserId` ON `Subscriptions` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE INDEX `IX_UserRoles_RoleId` ON `UserRoles` (`RoleId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Users_StripeCustomerId` ON `Users` (`StripeCustomerId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_WebhookEvents_StripeEventId` ON `WebhookEvents` (`StripeEventId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114153315_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261114153315_InitialCreate', '8.0.11');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    ALTER TABLE `Subscriptions` ADD `SubscriptionPlanPriceId` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    ALTER TABLE `SubscriptionPlans` MODIFY COLUMN `Currency` varchar(5) CHARACTER SET utf8mb4 NOT NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    ALTER TABLE `SubscriptionPlans` ADD `IsPopular` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    ALTER TABLE `Payments` MODIFY COLUMN `Currency` varchar(5) CHARACTER SET utf8mb4 NOT NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    ALTER TABLE `Invoices` MODIFY COLUMN `Currency` varchar(5) CHARACTER SET utf8mb4 NOT NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    CREATE TABLE `SubscriptionPlanFeatures` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SubscriptionPlanId` int NOT NULL,
        `FeatureName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `FeatureValue` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `DisplayOrder` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_SubscriptionPlanFeatures` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_SubscriptionPlanFeatures_SubscriptionPlans_SubscriptionPlanId` FOREIGN KEY (`SubscriptionPlanId`) REFERENCES `SubscriptionPlans` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    CREATE TABLE `SubscriptionPlanPrices` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SubscriptionPlanId` int NOT NULL,
        `BillingInterval` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `Amount` decimal(18,2) NOT NULL,
        `Currency` varchar(5) CHARACTER SET utf8mb4 NOT NULL,
        `StripePriceId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_SubscriptionPlanPrices` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_SubscriptionPlanPrices_SubscriptionPlans_SubscriptionPlanId` FOREIGN KEY (`SubscriptionPlanId`) REFERENCES `SubscriptionPlans` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    CREATE INDEX `IX_Subscriptions_SubscriptionPlanPriceId` ON `Subscriptions` (`SubscriptionPlanPriceId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    CREATE INDEX `IX_SubscriptionPlanFeatures_SubscriptionPlanId` ON `SubscriptionPlanFeatures` (`SubscriptionPlanId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    CREATE UNIQUE INDEX `IX_SubscriptionPlanPrices_SubscriptionPlanId_BillingInterval_Cu~` ON `SubscriptionPlanPrices` (`SubscriptionPlanId`, `BillingInterval`, `Currency`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    ALTER TABLE `Subscriptions` ADD CONSTRAINT `FK_Subscriptions_SubscriptionPlanPrices_SubscriptionPlanPriceId` FOREIGN KEY (`SubscriptionPlanPriceId`) REFERENCES `SubscriptionPlanPrices` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261114203401_AddNormalizedSubscriptionPlans') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261114203401_AddNormalizedSubscriptionPlans', '8.0.11');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

