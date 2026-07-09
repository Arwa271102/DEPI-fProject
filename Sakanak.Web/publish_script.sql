IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [RegistrationDate] datetime2 NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [Admins] (
        [AdminId] int NOT NULL IDENTITY,
        [ApplicationUserId] uniqueidentifier NOT NULL,
        [RoleLevel] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Admins] PRIMARY KEY ([AdminId]),
        CONSTRAINT [FK_Admins_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [Landlords] (
        [LandlordId] int NOT NULL IDENTITY,
        [ApplicationUserId] uniqueidentifier NOT NULL,
        [VerificationStatus] bit NOT NULL,
        [TotalProperties] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Landlords] PRIMARY KEY ([LandlordId]),
        CONSTRAINT [FK_Landlords_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [Apartments] (
        [ApartmentId] int NOT NULL IDENTITY,
        [LandlordId] int NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [PricePerMonth] decimal(18,2) NOT NULL,
        [TotalSeats] int NOT NULL,
        [Amenities] nvarchar(max) NOT NULL,
        [VirtualTourUrl] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Apartments] PRIMARY KEY ([ApartmentId]),
        CONSTRAINT [FK_Apartments_Landlords_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [Landlords] ([LandlordId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [ApartmentGroups] (
        [GroupId] int NOT NULL IDENTITY,
        [ApartmentId] int NOT NULL,
        [MaxMembers] int NOT NULL,
        [GroupStatus] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_ApartmentGroups] PRIMARY KEY ([GroupId]),
        CONSTRAINT [FK_ApartmentGroups_Apartments_ApartmentId] FOREIGN KEY ([ApartmentId]) REFERENCES [Apartments] ([ApartmentId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [Requests] (
        [RequestId] int NOT NULL IDENTITY,
        [LandlordId] int NOT NULL,
        [ApartmentId] int NOT NULL,
        [ReviewedByAdminId] int NULL,
        [Status] nvarchar(50) NOT NULL DEFAULT N'Pending',
        [Message] nvarchar(2000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ResolvedAt] datetime2 NULL,
        CONSTRAINT [PK_Requests] PRIMARY KEY ([RequestId]),
        CONSTRAINT [FK_Requests_Admins_ReviewedByAdminId] FOREIGN KEY ([ReviewedByAdminId]) REFERENCES [Admins] ([AdminId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Requests_Apartments_ApartmentId] FOREIGN KEY ([ApartmentId]) REFERENCES [Apartments] ([ApartmentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Requests_Landlords_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [Landlords] ([LandlordId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [Students] (
        [StudentId] int NOT NULL IDENTITY,
        [ApplicationUserId] uniqueidentifier NOT NULL,
        [University] nvarchar(200) NOT NULL,
        [Faculty] nvarchar(200) NOT NULL,
        [LatePaymentCount] int NOT NULL DEFAULT 0,
        [ApartmentGroupId] int NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([StudentId]),
        CONSTRAINT [FK_Students_ApartmentGroups_ApartmentGroupId] FOREIGN KEY ([ApartmentGroupId]) REFERENCES [ApartmentGroups] ([GroupId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Students_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [Bookings] (
        [BookingId] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [ApartmentId] int NOT NULL,
        [ApartmentGroupId] int NULL,
        [BookingDate] datetime2 NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [RequestedStartDate] datetime2 NOT NULL,
        [RequestedEndDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([BookingId]),
        CONSTRAINT [FK_Bookings_ApartmentGroups_ApartmentGroupId] FOREIGN KEY ([ApartmentGroupId]) REFERENCES [ApartmentGroups] ([GroupId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Bookings_Apartments_ApartmentId] FOREIGN KEY ([ApartmentId]) REFERENCES [Apartments] ([ApartmentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [LifestyleQuestionnaires] (
        [QuestionnaireId] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [SleepSchedule] nvarchar(50) NOT NULL,
        [IsSmoker] bit NOT NULL,
        [HygieneLevel] int NOT NULL,
        [StudyHabits] nvarchar(50) NOT NULL,
        [SocialPreference] nvarchar(50) NOT NULL,
        [GenderPreference] nvarchar(50) NOT NULL,
        [LastUpdated] datetime2 NOT NULL,
        CONSTRAINT [PK_LifestyleQuestionnaires] PRIMARY KEY ([QuestionnaireId]),
        CONSTRAINT [FK_LifestyleQuestionnaires_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [Contracts] (
        [ContractId] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [StudentId] int NOT NULL,
        [ApartmentId] int NOT NULL,
        [LandlordId] int NOT NULL,
        [VerifiedByAdminId] int NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [ReviewedAt] datetime2 NULL,
        [Status] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Contracts] PRIMARY KEY ([ContractId]),
        CONSTRAINT [FK_Contracts_Admins_VerifiedByAdminId] FOREIGN KEY ([VerifiedByAdminId]) REFERENCES [Admins] ([AdminId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Contracts_Apartments_ApartmentId] FOREIGN KEY ([ApartmentId]) REFERENCES [Apartments] ([ApartmentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Contracts_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Contracts_Landlords_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [Landlords] ([LandlordId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Contracts_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [Media] (
        [MediaId] int NOT NULL IDENTITY,
        [Url] nvarchar(500) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [EntityType] nvarchar(50) NOT NULL,
        [EntityId] int NOT NULL,
        [ApartmentId] int NULL,
        [ContractId] int NULL,
        [LandlordId] int NULL,
        [StudentId] int NULL,
        CONSTRAINT [PK_Media] PRIMARY KEY ([MediaId]),
        CONSTRAINT [FK_Media_Apartments_ApartmentId] FOREIGN KEY ([ApartmentId]) REFERENCES [Apartments] ([ApartmentId]),
        CONSTRAINT [FK_Media_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts] ([ContractId]),
        CONSTRAINT [FK_Media_Landlords_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [Landlords] ([LandlordId]),
        CONSTRAINT [FK_Media_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE TABLE [Payments] (
        [PaymentId] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [LandlordId] int NOT NULL,
        [ApartmentId] int NOT NULL,
        [ContractId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [PaymentDate] datetime2 NULL,
        [Status] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
        CONSTRAINT [FK_Payments_Apartments_ApartmentId] FOREIGN KEY ([ApartmentId]) REFERENCES [Apartments] ([ApartmentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts] ([ContractId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Landlords_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [Landlords] ([LandlordId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Admins_ApplicationUserId] ON [Admins] ([ApplicationUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Admins_RoleLevel] ON [Admins] ([RoleLevel]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_ApartmentGroups_ApartmentId] ON [ApartmentGroups] ([ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ApartmentGroups_ApartmentId_GroupStatus] ON [ApartmentGroups] ([ApartmentId], [GroupStatus]) WHERE [GroupStatus] = ''Open''');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Apartments_City_IsActive] ON [Apartments] ([City], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Apartments_LandlordId] ON [Apartments] ([LandlordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Apartments_PricePerMonth_IsActive] ON [Apartments] ([PricePerMonth], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Bookings_ApartmentGroupId] ON [Bookings] ([ApartmentGroupId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Bookings_ApartmentId_Status] ON [Bookings] ([ApartmentId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Bookings_StudentId_Status] ON [Bookings] ([StudentId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Contracts_ApartmentId_Status] ON [Contracts] ([ApartmentId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contracts_BookingId] ON [Contracts] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Contracts_EndDate] ON [Contracts] ([EndDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Contracts_LandlordId_Status] ON [Contracts] ([LandlordId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Contracts_StudentId_Status] ON [Contracts] ([StudentId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Contracts_VerifiedByAdminId] ON [Contracts] ([VerifiedByAdminId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Landlords_ApplicationUserId] ON [Landlords] ([ApplicationUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Landlords_VerificationStatus] ON [Landlords] ([VerificationStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LifestyleQuestionnaires_StudentId] ON [LifestyleQuestionnaires] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Media_ApartmentId] ON [Media] ([ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Media_ContractId] ON [Media] ([ContractId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Media_EntityType_EntityId] ON [Media] ([EntityType], [EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Media_EntityType_EntityId_Type] ON [Media] ([EntityType], [EntityId], [Type]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Media_LandlordId] ON [Media] ([LandlordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Media_StudentId] ON [Media] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Payments_ApartmentId] ON [Payments] ([ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Payments_ContractId] ON [Payments] ([ContractId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Payments_DueDate_Status] ON [Payments] ([DueDate], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Payments_LandlordId_Status] ON [Payments] ([LandlordId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Payments_StudentId_Status] ON [Payments] ([StudentId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Requests_ApartmentId_Status] ON [Requests] ([ApartmentId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Requests_LandlordId_Status] ON [Requests] ([LandlordId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Requests_ReviewedByAdminId] ON [Requests] ([ReviewedByAdminId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE INDEX [IX_Students_ApartmentGroupId] ON [Students] ([ApartmentGroupId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Students_ApplicationUserId] ON [Students] ([ApplicationUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427144439_IdentityProfilesRefactor'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260427144439_IdentityProfilesRefactor', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428221216_AgeAdded'
)
BEGIN
    ALTER TABLE [Students] ADD [Age] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428221216_AgeAdded'
)
BEGIN
    ALTER TABLE [Landlords] ADD [Age] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428221216_AgeAdded'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260428221216_AgeAdded', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428235633_AddSoftDeleteToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [DeletedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428235633_AddSoftDeleteToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428235633_AddSoftDeleteToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260428235633_AddSoftDeleteToUser', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430030113_AddIsProfileCompleteToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsProfileComplete] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430030113_AddIsProfileCompleteToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430030113_AddIsProfileCompleteToUser', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501040404_MediaRefactoredd'
)
BEGIN
    ALTER TABLE [Media] DROP CONSTRAINT [FK_Media_Apartments_ApartmentId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501040404_MediaRefactoredd'
)
BEGIN
    ALTER TABLE [Media] DROP CONSTRAINT [FK_Media_Contracts_ContractId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501040404_MediaRefactoredd'
)
BEGIN
    ALTER TABLE [Media] DROP CONSTRAINT [FK_Media_Landlords_LandlordId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501040404_MediaRefactoredd'
)
BEGIN
    ALTER TABLE [Media] DROP CONSTRAINT [FK_Media_Students_StudentId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501040404_MediaRefactoredd'
)
BEGIN
    ALTER TABLE [Media] ADD CONSTRAINT [FK_Media_Apartments_ApartmentId] FOREIGN KEY ([ApartmentId]) REFERENCES [Apartments] ([ApartmentId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501040404_MediaRefactoredd'
)
BEGIN
    ALTER TABLE [Media] ADD CONSTRAINT [FK_Media_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts] ([ContractId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501040404_MediaRefactoredd'
)
BEGIN
    ALTER TABLE [Media] ADD CONSTRAINT [FK_Media_Landlords_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [Landlords] ([LandlordId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501040404_MediaRefactoredd'
)
BEGIN
    ALTER TABLE [Media] ADD CONSTRAINT [FK_Media_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260501040404_MediaRefactoredd'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260501040404_MediaRefactoredd', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625215904_Phasetwolast'
)
BEGIN
    ALTER TABLE [Requests] ADD [PreviousValues] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625215904_Phasetwolast'
)
BEGIN
    ALTER TABLE [Requests] ADD [Type] nvarchar(50) NOT NULL DEFAULT N'ApartmentUpload';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625215904_Phasetwolast'
)
BEGIN
    ALTER TABLE [Landlords] ADD [RejectionReason] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625215904_Phasetwolast'
)
BEGIN
    ALTER TABLE [Landlords] ADD [VerificationRequestedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625215904_Phasetwolast'
)
BEGIN
    ALTER TABLE [Landlords] ADD [VerifiedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625215904_Phasetwolast'
)
BEGIN
    ALTER TABLE [Landlords] ADD [VerifiedByAdminId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625215904_Phasetwolast'
)
BEGIN
    CREATE INDEX [IX_Landlords_VerificationRequestedAt] ON [Landlords] ([VerificationRequestedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625215904_Phasetwolast'
)
BEGIN
    CREATE INDEX [IX_Landlords_VerifiedByAdminId] ON [Landlords] ([VerifiedByAdminId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625215904_Phasetwolast'
)
BEGIN
    ALTER TABLE [Landlords] ADD CONSTRAINT [FK_Landlords_Admins_VerifiedByAdminId] FOREIGN KEY ([VerifiedByAdminId]) REFERENCES [Admins] ([AdminId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625215904_Phasetwolast'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625215904_Phasetwolast', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626213240_Phase3BookingSystem'
)
BEGIN
    ALTER TABLE [Bookings] ADD [AcceptedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626213240_Phase3BookingSystem'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CancelledAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626213240_Phase3BookingSystem'
)
BEGIN
    ALTER TABLE [Bookings] ADD [Message] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626213240_Phase3BookingSystem'
)
BEGIN
    ALTER TABLE [Bookings] ADD [RejectedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626213240_Phase3BookingSystem'
)
BEGIN
    ALTER TABLE [Bookings] ADD [RejectionReason] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626213240_Phase3BookingSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260626213240_Phase3BookingSystem', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626230325_Phasethreeedits'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CancellationReason] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626230325_Phasethreeedits'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260626230325_Phasethreeedits', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627011458_Phase4ContractManagement'
)
BEGIN
    ALTER TABLE [Contracts] ADD [DocumentUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627011458_Phase4ContractManagement'
)
BEGIN
    ALTER TABLE [Contracts] ADD [RejectionReason] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627011458_Phase4ContractManagement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260627011458_Phase4ContractManagement', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627184730_Phase4FinalBusinessLogic'
)
BEGIN
    ALTER TABLE [Bookings] ADD [AddressAtBooking] nvarchar(300) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627184730_Phase4FinalBusinessLogic'
)
BEGIN
    ALTER TABLE [Bookings] ADD [AmenitiesAtBooking] nvarchar(1000) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627184730_Phase4FinalBusinessLogic'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CityAtBooking] nvarchar(100) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627184730_Phase4FinalBusinessLogic'
)
BEGIN
    ALTER TABLE [Bookings] ADD [PricePerMonthAtBooking] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627184730_Phase4FinalBusinessLogic'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260627184730_Phase4FinalBusinessLogic', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627191812_AddContractCancellationFields'
)
BEGIN
    ALTER TABLE [Contracts] ADD [CancellationDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627191812_AddContractCancellationFields'
)
BEGIN
    ALTER TABLE [Contracts] ADD [CancellationReason] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627191812_AddContractCancellationFields'
)
BEGIN
    ALTER TABLE [Contracts] ADD [CancelledByAdminId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627191812_AddContractCancellationFields'
)
BEGIN
    CREATE INDEX [IX_Contracts_CancelledByAdminId] ON [Contracts] ([CancelledByAdminId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627191812_AddContractCancellationFields'
)
BEGIN
    ALTER TABLE [Contracts] ADD CONSTRAINT [FK_Contracts_Admins_CancelledByAdminId] FOREIGN KEY ([CancelledByAdminId]) REFERENCES [Admins] ([AdminId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627191812_AddContractCancellationFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260627191812_AddContractCancellationFields', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702173454_Phase5PaymentSystem'
)
BEGIN
    ALTER TABLE [Payments] ADD [CreatedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702173454_Phase5PaymentSystem'
)
BEGIN
    ALTER TABLE [Payments] ADD [PaidAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702173454_Phase5PaymentSystem'
)
BEGIN
    ALTER TABLE [Payments] ADD [StripePaymentIntentId] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702173454_Phase5PaymentSystem'
)
BEGIN
    ALTER TABLE [Payments] ADD [StripeSessionId] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702173454_Phase5PaymentSystem'
)
BEGIN
    ALTER TABLE [Contracts] ADD [ActivatedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702173454_Phase5PaymentSystem'
)
BEGIN
    CREATE INDEX [IX_Payments_StripeSessionId] ON [Payments] ([StripeSessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702173454_Phase5PaymentSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702173454_Phase5PaymentSystem', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702215214_FinalPhase5'
)
BEGIN
    ALTER TABLE [Contracts] ADD [CompletedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702215214_FinalPhase5'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CompletedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702215214_FinalPhase5'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702215214_FinalPhase5', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705195444_Phase55And6AssignmentMatching'
)
BEGIN
    DROP INDEX [IX_ApartmentGroups_ApartmentId_GroupStatus] ON [ApartmentGroups];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705195444_Phase55And6AssignmentMatching'
)
BEGIN
    ALTER TABLE [ApartmentGroups] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705195444_Phase55And6AssignmentMatching'
)
BEGIN
    ALTER TABLE [ApartmentGroups] ADD [GroupName] nvarchar(100) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705195444_Phase55And6AssignmentMatching'
)
BEGIN
    CREATE INDEX [IX_ApartmentGroups_ApartmentId_GroupStatus] ON [ApartmentGroups] ([ApartmentId], [GroupStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260705195444_Phase55And6AssignmentMatching'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260705195444_Phase55And6AssignmentMatching', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706230152_Phase7CommunicationFeatures'
)
BEGIN
    CREATE TABLE [Messages] (
        [MessageId] int NOT NULL IDENTITY,
        [SenderUserId] uniqueidentifier NOT NULL,
        [RecipientUserId] uniqueidentifier NOT NULL,
        [SenderType] nvarchar(30) NOT NULL,
        [RecipientType] nvarchar(30) NOT NULL,
        [RelatedEntityId] int NULL,
        [RelatedEntityType] nvarchar(50) NULL,
        [MessageText] nvarchar(1000) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [IsRead] bit NOT NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([MessageId]),
        CONSTRAINT [FK_Messages_AspNetUsers_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Messages_AspNetUsers_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706230152_Phase7CommunicationFeatures'
)
BEGIN
    CREATE TABLE [Notifications] (
        [NotificationId] int NOT NULL IDENTITY,
        [RecipientUserId] uniqueidentifier NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [Type] nvarchar(80) NOT NULL,
        [ActionUrl] nvarchar(500) NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
        CONSTRAINT [FK_Notifications_AspNetUsers_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706230152_Phase7CommunicationFeatures'
)
BEGIN
    CREATE INDEX [IX_Messages_RecipientUserId_IsRead_SentAt] ON [Messages] ([RecipientUserId], [IsRead], [SentAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706230152_Phase7CommunicationFeatures'
)
BEGIN
    CREATE INDEX [IX_Messages_SenderUserId_RecipientUserId_SentAt] ON [Messages] ([SenderUserId], [RecipientUserId], [SentAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706230152_Phase7CommunicationFeatures'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientUserId_IsRead_CreatedAt] ON [Notifications] ([RecipientUserId], [IsRead], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706230152_Phase7CommunicationFeatures'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706230152_Phase7CommunicationFeatures', N'8.0.26');
END;
GO

COMMIT;
GO

