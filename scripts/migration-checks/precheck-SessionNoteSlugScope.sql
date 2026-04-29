-- Pre-flight diagnostic for migration: UrlRestructure_SessionNoteSlugScope
--
-- Lists every session that, after the planned slug derivation, would produce
-- a within-session collision. These sessions will have auto-suffixed slugs
-- (e.g. "munkys-notes-2") after the migration runs.
--
-- Run this against the target database BEFORE applying the migration.

WITH Derived AS (
    -- Derive candidate slug for each GUID-shaped session note
    -- This is an approximation: T-SQL slug derivation results may differ slightly
    -- from the application's SlugGenerator for non-ASCII characters.
    SELECT
        a.Id,
        a.SessionId,
        a.Title,
        a.Slug AS CurrentSlug,
        a.CreatedAt,
        -- Simple approximation: lowercase, replace common non-alphanum with hyphen
        -- For exact results, apply the migration in a dev environment first.
        LOWER(
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(a.Title, '''', ''),
                    ' ', '-'),
                '.', '-'),
            ',', '-')
        ) AS ApproxDerivedSlug
    FROM Articles a
    WHERE a.Type = 11
      AND a.SessionId IS NOT NULL
      AND LEN(a.Slug) = 36
      AND a.Slug LIKE '________-____-____-____-____________'
),
Collisions AS (
    SELECT
        d.SessionId,
        d.ApproxDerivedSlug AS DerivedSlug,
        COUNT(*) AS NoteCount
    FROM Derived d
    GROUP BY d.SessionId, d.ApproxDerivedSlug
    HAVING COUNT(*) > 1
)
SELECT
    c.SessionId,
    s.Name AS SessionName,
    c.DerivedSlug,
    c.NoteCount,
    'Notes with slug "' + c.DerivedSlug + '" will be suffixed: '
        + c.DerivedSlug + ', '
        + c.DerivedSlug + '-2, ...' AS Impact
FROM Collisions c
LEFT JOIN Sessions s ON s.Id = c.SessionId
ORDER BY c.SessionId, c.DerivedSlug;

-- Summary row
SELECT
    COUNT(DISTINCT SessionId) AS SessionsAffected,
    COUNT(*) AS TotalGuidSlugsToBackfill
FROM Articles
WHERE Type = 11
  AND SessionId IS NOT NULL
  AND LEN(Slug) = 36
  AND Slug LIKE '________-____-____-____-____________';
