**Chronicis ResourceCompiler**
This console app compiles raw JSON datasets into denormalized JSON documents and index files based on a YAML manifest. It is deterministic, offline, and schema-agnostic, with relationships driven entirely by the manifest.

**Scope**
- Load and validate manifest (YAML).
- Load raw JSON arrays and extract PKs.
- Canonicalize keys and build PK/FK indexes.
- Assemble compiled documents in memory.
- Write outputs to disk using a collision-safe layout.

Phase 0 rules and output layout are defined in `src/Chronicis.ResourceCompiler/docs/Phase0.Contract.md`.

**CLI Usage**
Required arguments:
- `--manifest <path>`
- `--raw <path>`
- `--out <path>`

Optional arguments:
- `--maxDepth <int>`
- `--verbose`
- `--help`

Example:
```bash
dotnet run --project src/Chronicis.ResourceCompiler -- \
  --manifest src/Chronicis.ResourceCompiler.Tests/TestData/manifests/phase4/assembly-basic.yml \
  --raw src/Chronicis.ResourceCompiler.Tests/TestData/raw/phase4 \
  --out .\out \
  --maxDepth 3 \
  --verbose
```

**Manifest Notes**
- The primary key field is defined per entity using `pk`.
- Child relationships use `children[].fk.field`.
- Ordering uses `orderBy.field` and `orderBy.direction`.
- Fields support dotted paths for nested values. Example: `fields.parent` or `fields.pk`.

**Outputs**
Folder names are collision-safe and deterministic: `<slug>-<shortHash>`.

Compiled documents:
- `out/<rootEntitySlug-hash>/documents.json`

PK index:
- `out/<entitySlug-hash>/indexes/by-pk.json`
- Maps canonical PK string to document ordinal (0-based).

FK index:
- `out/<entitySlug-hash>/indexes/fk/<childSlug-hash>__<fkFieldSlug-hash>.json`
- Maps canonical parent key to ordered list of child canonical PKs.

**Warnings And Exit Codes**
- Warnings are aggregated from all phases.
- Exit code `0`: no Error-severity warnings.
- Exit code `1`: one or more Error-severity warnings.
- `--verbose` prints each warning with severity, code, entity, path, and message.

**Constraints**
- No network access.
- No database.
- Deterministic output using System.Text.Json only.

**Tests And Fixtures**
- Tests: `src/Chronicis.ResourceCompiler.Tests`
- Fixtures: `src/Chronicis.ResourceCompiler.Tests/TestData`
