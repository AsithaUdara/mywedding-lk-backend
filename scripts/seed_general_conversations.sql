-- Seed a default "# general" conversation for each existing wedding event
-- This script is idempotent - it won't create duplicates if run multiple times

INSERT INTO [dbo].[Conversations] (Id, Name, EventId, CreatedAt)
SELECT NEWID(), '# general', Id, GETUTCDATE()
FROM [dbo].[WeddingEvents]
WHERE Id NOT IN (SELECT EventId FROM [dbo].[Conversations]);

-- Verify the results
SELECT COUNT(*) AS [New Conversations Created] FROM [dbo].[Conversations];
