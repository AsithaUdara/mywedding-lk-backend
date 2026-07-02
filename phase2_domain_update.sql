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
    WHERE [MigrationId] = N'20251224133847_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(450) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(max) NOT NULL,
        [LastName] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251224133847_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251224133847_InitialCreate', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251224155048_AddWeddingEventsTable'
)
BEGIN
    CREATE TABLE [WeddingEvents] (
        [Id] uniqueidentifier NOT NULL,
        [EventName] nvarchar(max) NOT NULL,
        [EventDate] datetime2 NOT NULL,
        [CreatedById] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WeddingEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WeddingEvents_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251224155048_AddWeddingEventsTable'
)
BEGIN
    CREATE INDEX [IX_WeddingEvents_CreatedById] ON [WeddingEvents] ([CreatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251224155048_AddWeddingEventsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251224155048_AddWeddingEventsTable', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225090559_AddEventOrganizersTable'
)
BEGIN
    ALTER TABLE [WeddingEvents] DROP CONSTRAINT [FK_WeddingEvents_Users_CreatedById];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225090559_AddEventOrganizersTable'
)
BEGIN
    CREATE TABLE [EventOrganizers] (
        [EventId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Role] int NOT NULL,
        [PermissionLevel] int NOT NULL,
        [JoinedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EventOrganizers] PRIMARY KEY ([EventId], [UserId]),
        CONSTRAINT [FK_EventOrganizers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EventOrganizers_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225090559_AddEventOrganizersTable'
)
BEGIN
    CREATE INDEX [IX_EventOrganizers_UserId] ON [EventOrganizers] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225090559_AddEventOrganizersTable'
)
BEGIN
    ALTER TABLE [WeddingEvents] ADD CONSTRAINT [FK_WeddingEvents_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225090559_AddEventOrganizersTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251225090559_AddEventOrganizersTable', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225161257_AddEventTasksTable'
)
BEGIN
    CREATE TABLE [EventTasks] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [DueDate] datetime2 NULL,
        [EventId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EventTasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventTasks_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225161257_AddEventTasksTable'
)
BEGIN
    CREATE INDEX [IX_EventTasks_EventId] ON [EventTasks] ([EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225161257_AddEventTasksTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251225161257_AddEventTasksTable', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225181519_AddBudgetAndExpenseTables'
)
BEGIN
    ALTER TABLE [WeddingEvents] ADD [TotalBudget] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225181519_AddBudgetAndExpenseTables'
)
BEGIN
    CREATE TABLE [BudgetCategories] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK_BudgetCategories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225181519_AddBudgetAndExpenseTables'
)
BEGIN
    CREATE TABLE [Expenses] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ExpenseDate] datetime2 NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [BudgetCategoryId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Expenses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Expenses_BudgetCategories_BudgetCategoryId] FOREIGN KEY ([BudgetCategoryId]) REFERENCES [BudgetCategories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Expenses_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225181519_AddBudgetAndExpenseTables'
)
BEGIN
    CREATE INDEX [IX_Expenses_BudgetCategoryId] ON [Expenses] ([BudgetCategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225181519_AddBudgetAndExpenseTables'
)
BEGIN
    CREATE INDEX [IX_Expenses_EventId] ON [Expenses] ([EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225181519_AddBudgetAndExpenseTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251225181519_AddBudgetAndExpenseTables', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227062225_AddVendorEntities'
)
BEGIN
    CREATE TABLE [VendorCategories] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK_VendorCategories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227062225_AddVendorEntities'
)
BEGIN
    CREATE TABLE [Vendors] (
        [UserId] nvarchar(450) NOT NULL,
        [BusinessName] nvarchar(max) NOT NULL,
        [BusinessDescription] nvarchar(max) NULL,
        [WebsiteUrl] nvarchar(max) NULL,
        [City] nvarchar(max) NULL,
        [Province] nvarchar(max) NULL,
        [VerificationStatus] int NOT NULL,
        [AverageRating] decimal(3,2) NOT NULL,
        CONSTRAINT [PK_Vendors] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_Vendors_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227062225_AddVendorEntities'
)
BEGIN
    CREATE TABLE [VendorReviews] (
        [Id] uniqueidentifier NOT NULL,
        [Rating] int NOT NULL,
        [ReviewContent] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [VendorId] nvarchar(450) NOT NULL,
        [ReviewerId] nvarchar(450) NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_VendorReviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VendorReviews_Users_ReviewerId] FOREIGN KEY ([ReviewerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VendorReviews_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_VendorReviews_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227062225_AddVendorEntities'
)
BEGIN
    CREATE TABLE [VendorServices] (
        [Id] uniqueidentifier NOT NULL,
        [ServiceName] nvarchar(max) NOT NULL,
        [ServiceDescription] nvarchar(max) NULL,
        [BasePrice] decimal(18,2) NOT NULL,
        [PricingType] int NOT NULL,
        [VendorId] nvarchar(450) NOT NULL,
        [CategoryId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_VendorServices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VendorServices_VendorCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [VendorCategories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VendorServices_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([UserId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227062225_AddVendorEntities'
)
BEGIN
    CREATE INDEX [IX_VendorReviews_EventId] ON [VendorReviews] ([EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227062225_AddVendorEntities'
)
BEGIN
    CREATE INDEX [IX_VendorReviews_ReviewerId] ON [VendorReviews] ([ReviewerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227062225_AddVendorEntities'
)
BEGIN
    CREATE INDEX [IX_VendorReviews_VendorId] ON [VendorReviews] ([VendorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227062225_AddVendorEntities'
)
BEGIN
    CREATE INDEX [IX_VendorServices_CategoryId] ON [VendorServices] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227062225_AddVendorEntities'
)
BEGIN
    CREATE INDEX [IX_VendorServices_VendorId] ON [VendorServices] ([VendorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227062225_AddVendorEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251227062225_AddVendorEntities', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227103347_AddBookingEntities'
)
BEGIN
    CREATE TABLE [VendorBookings] (
        [Id] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [FinalAmount] decimal(18,2) NOT NULL,
        [ServiceDate] datetime2 NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [ServiceId] uniqueidentifier NOT NULL,
        [BookedById] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_VendorBookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VendorBookings_Users_BookedById] FOREIGN KEY ([BookedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VendorBookings_VendorServices_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [VendorServices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VendorBookings_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227103347_AddBookingEntities'
)
BEGIN
    CREATE TABLE [BookingContracts] (
        [Id] uniqueidentifier NOT NULL,
        [ContractFileUrl] nvarchar(max) NULL,
        [ClientSignedAt] datetime2 NULL,
        [VendorSignedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BookingContracts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BookingContracts_VendorBookings_Id] FOREIGN KEY ([Id]) REFERENCES [VendorBookings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227103347_AddBookingEntities'
)
BEGIN
    CREATE INDEX [IX_VendorBookings_BookedById] ON [VendorBookings] ([BookedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227103347_AddBookingEntities'
)
BEGIN
    CREATE INDEX [IX_VendorBookings_EventId] ON [VendorBookings] ([EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227103347_AddBookingEntities'
)
BEGIN
    CREATE INDEX [IX_VendorBookings_ServiceId] ON [VendorBookings] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227103347_AddBookingEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251227103347_AddBookingEntities', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227115356_FixBookingContractRelationship'
)
BEGIN
    ALTER TABLE [BookingContracts] DROP CONSTRAINT [FK_BookingContracts_VendorBookings_Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227115356_FixBookingContractRelationship'
)
BEGIN
    ALTER TABLE [BookingContracts] ADD CONSTRAINT [FK_BookingContracts_VendorBookings_Id] FOREIGN KEY ([Id]) REFERENCES [VendorBookings] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227115356_FixBookingContractRelationship'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251227115356_FixBookingContractRelationship', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128172546_AddStylePreferencesToEvent'
)
BEGIN
    ALTER TABLE [WeddingEvents] ADD [StylePreferences] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128172546_AddStylePreferencesToEvent'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128172546_AddStylePreferencesToEvent', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128184953_AddActivityFeedItemsTable'
)
BEGIN
    CREATE TABLE [ActivityFeedItems] (
        [Id] uniqueidentifier NOT NULL,
        [ItemType] int NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_ActivityFeedItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ActivityFeedItems_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ActivityFeedItems_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128184953_AddActivityFeedItemsTable'
)
BEGIN
    CREATE INDEX [IX_ActivityFeedItems_EventId] ON [ActivityFeedItems] ([EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128184953_AddActivityFeedItemsTable'
)
BEGIN
    CREATE INDEX [IX_ActivityFeedItems_UserId] ON [ActivityFeedItems] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128184953_AddActivityFeedItemsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128184953_AddActivityFeedItemsTable', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    CREATE TABLE [Conversations] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Conversations_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    CREATE TABLE [Messages] (
        [Id] uniqueidentifier NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ConversationId] uniqueidentifier NOT NULL,
        [SenderId] nvarchar(450) NOT NULL,
        [AttachedVendorServiceId] uniqueidentifier NULL,
        [AttachedEventTaskId] uniqueidentifier NULL,
        [AttachedExpenseId] uniqueidentifier NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Messages_EventTasks_AttachedEventTaskId] FOREIGN KEY ([AttachedEventTaskId]) REFERENCES [EventTasks] ([Id]),
        CONSTRAINT [FK_Messages_Expenses_AttachedExpenseId] FOREIGN KEY ([AttachedExpenseId]) REFERENCES [Expenses] ([Id]),
        CONSTRAINT [FK_Messages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Messages_VendorServices_AttachedVendorServiceId] FOREIGN KEY ([AttachedVendorServiceId]) REFERENCES [VendorServices] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    CREATE TABLE [MessageReadStatuses] (
        [MessageId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [ReadAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MessageReadStatuses] PRIMARY KEY ([MessageId], [UserId]),
        CONSTRAINT [FK_MessageReadStatuses_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MessageReadStatuses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    CREATE INDEX [IX_Conversations_EventId] ON [Conversations] ([EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    CREATE INDEX [IX_MessageReadStatuses_UserId] ON [MessageReadStatuses] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    CREATE INDEX [IX_Messages_AttachedEventTaskId] ON [Messages] ([AttachedEventTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    CREATE INDEX [IX_Messages_AttachedExpenseId] ON [Messages] ([AttachedExpenseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    CREATE INDEX [IX_Messages_AttachedVendorServiceId] ON [Messages] ([AttachedVendorServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    CREATE INDEX [IX_Messages_ConversationId] ON [Messages] ([ConversationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260129070915_AddCollaborationHubEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260129070915_AddCollaborationHubEntities', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    ALTER TABLE [Vendors] ADD [PrimaryCategoryId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    ALTER TABLE [VendorBookings] ADD [VendorUserId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE TABLE [EventInvitations] (
        [Id] uniqueidentifier NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Token] nvarchar(450) NOT NULL,
        [InvitedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [IsAccepted] bit NOT NULL,
        [AcceptedAt] datetime2 NULL,
        [InvitedById] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_EventInvitations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventInvitations_Users_InvitedById] FOREIGN KEY ([InvitedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventInvitations_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE TABLE [Polls] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [CreatedById] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Polls] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Polls_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Polls_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE TABLE [PollOptions] (
        [Id] uniqueidentifier NOT NULL,
        [OptionText] nvarchar(max) NOT NULL,
        [PollId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_PollOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PollOptions_Polls_PollId] FOREIGN KEY ([PollId]) REFERENCES [Polls] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE TABLE [PollVotes] (
        [Id] uniqueidentifier NOT NULL,
        [PollOptionId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [VotedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PollVotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PollVotes_PollOptions_PollOptionId] FOREIGN KEY ([PollOptionId]) REFERENCES [PollOptions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PollVotes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE INDEX [IX_Vendors_PrimaryCategoryId] ON [Vendors] ([PrimaryCategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE INDEX [IX_VendorBookings_VendorUserId] ON [VendorBookings] ([VendorUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE INDEX [IX_EventInvitations_EventId] ON [EventInvitations] ([EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE INDEX [IX_EventInvitations_InvitedById] ON [EventInvitations] ([InvitedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EventInvitations_Token] ON [EventInvitations] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE INDEX [IX_PollOptions_PollId] ON [PollOptions] ([PollId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE INDEX [IX_Polls_CreatedById] ON [Polls] ([CreatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE INDEX [IX_Polls_EventId] ON [Polls] ([EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE INDEX [IX_PollVotes_PollOptionId] ON [PollVotes] ([PollOptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    CREATE INDEX [IX_PollVotes_UserId] ON [PollVotes] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    ALTER TABLE [VendorBookings] ADD CONSTRAINT [FK_VendorBookings_Vendors_VendorUserId] FOREIGN KEY ([VendorUserId]) REFERENCES [Vendors] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    ALTER TABLE [Vendors] ADD CONSTRAINT [FK_Vendors_VendorCategories_PrimaryCategoryId] FOREIGN KEY ([PrimaryCategoryId]) REFERENCES [VendorCategories] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218195820_AddPollsAndInvitationsFixed'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260218195820_AddPollsAndInvitationsFixed', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228165412_AddIsActiveToVendorService'
)
BEGIN
    ALTER TABLE [VendorServices] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228165412_AddIsActiveToVendorService'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260228165412_AddIsActiveToVendorService', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228165623_UpdateDefaultIsActiveToTrue'
)
BEGIN
    UPDATE VendorServices SET IsActive = 1
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228165623_UpdateDefaultIsActiveToTrue'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[VendorServices]') AND [c].[name] = N'IsActive');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [VendorServices] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [VendorServices] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228165623_UpdateDefaultIsActiveToTrue'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260228165623_UpdateDefaultIsActiveToTrue', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303195104_AddRoleToInvitation'
)
BEGIN
    ALTER TABLE [EventInvitations] ADD [PermissionLevel] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303195104_AddRoleToInvitation'
)
BEGIN
    ALTER TABLE [EventInvitations] ADD [Role] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303195104_AddRoleToInvitation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260303195104_AddRoleToInvitation', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303201233_UpdateOrganizerRoleEnum'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260303201233_UpdateOrganizerRoleEnum', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521053252_AddVendorInquiry'
)
BEGIN
    CREATE TABLE [VendorInquiries] (
        [Id] uniqueidentifier NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [SenderEmail] nvarchar(max) NOT NULL,
        [SenderId] nvarchar(max) NOT NULL,
        [VendorId] nvarchar(450) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [IsRead] bit NOT NULL,
        CONSTRAINT [PK_VendorInquiries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VendorInquiries_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([UserId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521053252_AddVendorInquiry'
)
BEGIN
    CREATE INDEX [IX_VendorInquiries_VendorId] ON [VendorInquiries] ([VendorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521053252_AddVendorInquiry'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260521053252_AddVendorInquiry', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE TABLE [BookingPaymentTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [GatewayName] nvarchar(max) NOT NULL,
        [GatewayPaymentId] nvarchar(max) NULL,
        [IdempotencyKey] nvarchar(450) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [Currency] nvarchar(max) NULL,
        [RawCallbackPayload] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [PaidAt] datetime2 NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BookingPaymentTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BookingPaymentTransactions_VendorBookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [VendorBookings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE TABLE [CommissionSettlements] (
        [Id] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [GrossAmount] decimal(18,2) NOT NULL,
        [CommissionAmount] decimal(18,2) NOT NULL,
        [VendorNetAmount] decimal(18,2) NOT NULL,
        [CommissionRate] decimal(5,4) NOT NULL,
        [IsVendorPayoutSettled] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [SettledAt] datetime2 NULL,
        CONSTRAINT [PK_CommissionSettlements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CommissionSettlements_VendorBookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [VendorBookings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE TABLE [EventItineraries] (
        [Id] uniqueidentifier NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [IsAiGenerated] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EventItineraries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventItineraries_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE TABLE [VendorSubscriptions] (
        [Id] uniqueidentifier NOT NULL,
        [VendorId] nvarchar(450) NOT NULL,
        [Tier] int NOT NULL,
        [Status] int NOT NULL,
        [MonthlyFee] decimal(18,2) NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_VendorSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VendorSubscriptions_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([UserId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE TABLE [WeddingPlanners] (
        [UserId] nvarchar(450) NOT NULL,
        [BusinessName] nvarchar(max) NOT NULL,
        [BusinessDescription] nvarchar(max) NULL,
        [ContactPhone] nvarchar(max) NULL,
        [City] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WeddingPlanners] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_WeddingPlanners_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE TABLE [EventItineraryItems] (
        [Id] uniqueidentifier NOT NULL,
        [ItineraryId] uniqueidentifier NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_EventItineraryItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventItineraryItems_EventItineraries_ItineraryId] FOREIGN KEY ([ItineraryId]) REFERENCES [EventItineraries] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE TABLE [PlannerClientEvents] (
        [Id] uniqueidentifier NOT NULL,
        [PlannerId] nvarchar(450) NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [ClientUserId] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PlannerClientEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlannerClientEvents_Users_ClientUserId] FOREIGN KEY ([ClientUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlannerClientEvents_WeddingEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [WeddingEvents] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PlannerClientEvents_WeddingPlanners_PlannerId] FOREIGN KEY ([PlannerId]) REFERENCES [WeddingPlanners] ([UserId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE TABLE [PlannerSubscriptions] (
        [Id] uniqueidentifier NOT NULL,
        [PlannerId] nvarchar(450) NOT NULL,
        [Tier] int NOT NULL,
        [Status] int NOT NULL,
        [MonthlyFee] decimal(18,2) NOT NULL,
        [MaxConcurrentEvents] int NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PlannerSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlannerSubscriptions_WeddingPlanners_PlannerId] FOREIGN KEY ([PlannerId]) REFERENCES [WeddingPlanners] ([UserId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE INDEX [IX_BookingPaymentTransactions_BookingId] ON [BookingPaymentTransactions] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BookingPaymentTransactions_IdempotencyKey] ON [BookingPaymentTransactions] ([IdempotencyKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CommissionSettlements_BookingId] ON [CommissionSettlements] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE INDEX [IX_EventItineraries_EventId] ON [EventItineraries] ([EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE INDEX [IX_EventItineraryItems_ItineraryId] ON [EventItineraryItems] ([ItineraryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE INDEX [IX_PlannerClientEvents_ClientUserId] ON [PlannerClientEvents] ([ClientUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE INDEX [IX_PlannerClientEvents_EventId] ON [PlannerClientEvents] ([EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlannerClientEvents_PlannerId_EventId] ON [PlannerClientEvents] ([PlannerId], [EventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE INDEX [IX_PlannerSubscriptions_PlannerId] ON [PlannerSubscriptions] ([PlannerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    CREATE INDEX [IX_VendorSubscriptions_VendorId] ON [VendorSubscriptions] ([VendorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260523100703_AddPlannerPaymentsSubscriptionsAndAi', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524113754_AddVendorContactPhone'
)
BEGIN
    ALTER TABLE [Vendors] ADD [ContactPhone] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524113754_AddVendorContactPhone'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524113754_AddVendorContactPhone', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524124044_AddVendorBillingProfile'
)
BEGIN
    CREATE TABLE [VendorBillingProfiles] (
        [VendorId] nvarchar(450) NOT NULL,
        [CardholderName] nvarchar(max) NULL,
        [CardBrand] nvarchar(max) NULL,
        [Last4] nvarchar(max) NULL,
        [ExpiryMonth] tinyint NULL,
        [ExpiryYear] smallint NULL,
        [PayHerePaymentMethod] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_VendorBillingProfiles] PRIMARY KEY ([VendorId]),
        CONSTRAINT [FK_VendorBillingProfiles_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([UserId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524124044_AddVendorBillingProfile'
)
BEGIN
    CREATE TABLE [VendorSubscriptionCheckouts] (
        [Id] uniqueidentifier NOT NULL,
        [VendorId] nvarchar(450) NOT NULL,
        [Tier] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [PaidAt] datetime2 NULL,
        CONSTRAINT [PK_VendorSubscriptionCheckouts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VendorSubscriptionCheckouts_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([UserId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524124044_AddVendorBillingProfile'
)
BEGIN
    CREATE INDEX [IX_VendorSubscriptionCheckouts_VendorId] ON [VendorSubscriptionCheckouts] ([VendorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524124044_AddVendorBillingProfile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524124044_AddVendorBillingProfile', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524160852_AddVendorServiceImages'
)
BEGIN
    ALTER TABLE [VendorServices] ADD [GalleryUrlsJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524160852_AddVendorServiceImages'
)
BEGIN
    ALTER TABLE [VendorServices] ADD [PrimaryImageUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524160852_AddVendorServiceImages'
)
BEGIN
    ALTER TABLE [Vendors] ADD [CoverImageUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524160852_AddVendorServiceImages'
)
BEGIN
    ALTER TABLE [Vendors] ADD [GalleryUrlsJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524160852_AddVendorServiceImages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524160852_AddVendorServiceImages', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524171926_AddServiceListingDetails'
)
BEGIN
    ALTER TABLE [VendorServices] ADD [ListingDetailsJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524171926_AddServiceListingDetails'
)
BEGIN
    ALTER TABLE [VendorServices] ADD [Tagline] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524171926_AddServiceListingDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524171926_AddServiceListingDetails', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528140753_Phase1_PurgePollsAndStyle'
)
BEGIN
    DROP TABLE [PollVotes];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528140753_Phase1_PurgePollsAndStyle'
)
BEGIN
    DROP TABLE [PollOptions];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528140753_Phase1_PurgePollsAndStyle'
)
BEGIN
    DROP TABLE [Polls];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528140753_Phase1_PurgePollsAndStyle'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WeddingEvents]') AND [c].[name] = N'StylePreferences');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [WeddingEvents] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [WeddingEvents] DROP COLUMN [StylePreferences];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528140753_Phase1_PurgePollsAndStyle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260528140753_Phase1_PurgePollsAndStyle', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    ALTER TABLE [WeddingEvents] ADD [EventLifecycleStage] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    ALTER TABLE [WeddingEvents] ADD [ManagingPlannerId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    ALTER TABLE [EventTasks] ADD [AssignedToUserId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    ALTER TABLE [EventTasks] ADD [DependsOnTaskId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    ALTER TABLE [EventTasks] ADD [StartDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    CREATE INDEX [IX_WeddingEvents_ManagingPlannerId] ON [WeddingEvents] ([ManagingPlannerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    CREATE INDEX [IX_EventTasks_AssignedToUserId] ON [EventTasks] ([AssignedToUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    CREATE INDEX [IX_EventTasks_DependsOnTaskId] ON [EventTasks] ([DependsOnTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    ALTER TABLE [EventTasks] ADD CONSTRAINT [FK_EventTasks_EventTasks_DependsOnTaskId] FOREIGN KEY ([DependsOnTaskId]) REFERENCES [EventTasks] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    ALTER TABLE [EventTasks] ADD CONSTRAINT [FK_EventTasks_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    ALTER TABLE [WeddingEvents] ADD CONSTRAINT [FK_WeddingEvents_WeddingPlanners_ManagingPlannerId] FOREIGN KEY ([ManagingPlannerId]) REFERENCES [WeddingPlanners] ([UserId]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260528141450_Phase2_DomainUpdate_PlannerMultiTenancy', N'8.0.6');
END;
GO

COMMIT;
GO

