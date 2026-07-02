-- Optional: set historical timestamps for admin@mw.com after bootstrap sync.
-- Run against MyWeddingDbDemo when you need exact CreatedAt/UpdatedAt values.
UPDATE Users
SET
    CreatedAt = '2024-01-15T08:00:00.0000000',
    UpdatedAt = '2025-06-01T10:30:00.0000000'
WHERE Id = 'zHvTgDy7CZVDYtyUTHNatugwwg82'
  AND Email = 'admin@mw.com';
