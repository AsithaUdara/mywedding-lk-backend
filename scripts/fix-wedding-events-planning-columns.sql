-- Run on your MyWedding SQL database if planner dashboard shows "Invalid column name" errors.
-- Safe to re-run: each column is added only if missing.

IF COL_LENGTH('dbo.WeddingEvents', 'TaskPlanPhase') IS NULL
BEGIN
    ALTER TABLE [WeddingEvents] ADD [TaskPlanPhase] int NOT NULL CONSTRAINT [DF_WeddingEvents_TaskPlanPhase] DEFAULT 0;
END;

IF COL_LENGTH('dbo.WeddingEvents', 'EstimatedGuestCount') IS NULL
    ALTER TABLE [WeddingEvents] ADD [EstimatedGuestCount] int NULL;

IF COL_LENGTH('dbo.WeddingEvents', 'GuestCountMax') IS NULL
    ALTER TABLE [WeddingEvents] ADD [GuestCountMax] int NULL;

IF COL_LENGTH('dbo.WeddingEvents', 'WeddingStyle') IS NULL
    ALTER TABLE [WeddingEvents] ADD [WeddingStyle] nvarchar(128) NULL;

IF COL_LENGTH('dbo.WeddingEvents', 'VenuePreference') IS NULL
    ALTER TABLE [WeddingEvents] ADD [VenuePreference] nvarchar(512) NULL;

IF COL_LENGTH('dbo.WeddingEvents', 'MustHavesNotes') IS NULL
    ALTER TABLE [WeddingEvents] ADD [MustHavesNotes] nvarchar(max) NULL;

IF COL_LENGTH('dbo.WeddingEvents', 'ServicesAlreadyBooked') IS NULL
    ALTER TABLE [WeddingEvents] ADD [ServicesAlreadyBooked] nvarchar(max) NULL;

IF COL_LENGTH('dbo.WeddingEvents', 'CulturalOrReligiousNotes') IS NULL
    ALTER TABLE [WeddingEvents] ADD [CulturalOrReligiousNotes] nvarchar(max) NULL;

IF COL_LENGTH('dbo.WeddingEvents', 'BriefCompletedAt') IS NULL
    ALTER TABLE [WeddingEvents] ADD [BriefCompletedAt] datetime2 NULL;

-- Record migrations so EF does not try to re-apply (optional if you use dotnet ef database update instead)
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260602161000_AddEventTaskPlanPhase')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260602161000_AddEventTaskPlanPhase', N'8.0.6');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260602180000_AddEventPlanningBrief')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260602180000_AddEventPlanningBrief', N'8.0.6');
