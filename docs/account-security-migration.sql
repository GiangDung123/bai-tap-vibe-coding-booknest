START TRANSACTION;

ALTER TABLE `Users` ADD `AuthVersion` bigint NOT NULL DEFAULT 1;

ALTER TABLE `Users` ADD `DefaultShippingAddress` varchar(1000) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

ALTER TABLE `Users` ADD `IsActive` tinyint(1) NOT NULL DEFAULT TRUE;

ALTER TABLE `Users` ADD `PhoneNumber` varchar(40) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

ALTER TABLE `Users` ADD `UpdatedAt` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00';

UPDATE `Users` SET `UpdatedAt` = `CreatedAt`

CREATE TABLE `SecurityGuards` (
    `Id` int NOT NULL AUTO_INCREMENT,
    CONSTRAINT `PK_SecurityGuards` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `UserAudits` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ActorId` int NOT NULL,
    `TargetId` int NOT NULL,
    `Action` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `OldValue` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `NewValue` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_UserAudits` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_UserAudits_Users_ActorId` FOREIGN KEY (`ActorId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_UserAudits_Users_TargetId` FOREIGN KEY (`TargetId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

INSERT INTO `SecurityGuards` (`Id`)
VALUES (1);

CREATE INDEX `IX_Users_Role_IsActive` ON `Users` (`Role`, `IsActive`);

CREATE INDEX `IX_UserAudits_ActorId` ON `UserAudits` (`ActorId`);

CREATE INDEX `IX_UserAudits_TargetId_CreatedAt` ON `UserAudits` (`TargetId`, `CreatedAt`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261024073046_AccountSecurityAndUserManagement', '8.0.22');

COMMIT;

