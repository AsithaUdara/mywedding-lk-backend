-- Unpublish all listings for a vendor (draft mode: IsActive = 0).
-- Safe: does not delete data; vendor can re-publish from the vendor dashboard.
DECLARE @BusinessName NVARCHAR(200) = N'Test Business';

UPDATE vs
SET vs.IsActive = 0
FROM VendorServices AS vs
INNER JOIN Vendors AS v ON v.UserId = vs.VendorId
WHERE v.BusinessName = @BusinessName;

SELECT
    v.UserId AS VendorId,
    v.BusinessName,
    COUNT(*) AS ListingsSetToDraft
FROM Vendors AS v
INNER JOIN VendorServices AS vs ON vs.VendorId = v.UserId
WHERE v.BusinessName = @BusinessName
GROUP BY v.UserId, v.BusinessName;
