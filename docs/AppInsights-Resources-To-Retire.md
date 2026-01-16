# Application Insights Resources to Retire

**Document Purpose:** Track Application Insights resources that will be deleted in Phase 13 after DataDog migration is complete and stable.

**Created:** January 16, 2026  
**Status:** Awaiting DataDog migration completion

## Resources to Delete

| Name | Resource Group | Location | Type | Status | Notes |
|------|---------------|----------|------|--------|-------|
| appi-chronicis-dev | rg-chronicis-dev | westus | microsoft.insights/components | Succeeded | Original App Insights instance |
| app-chronicis-api | rg-chronicis-dev | westus2 | microsoft.insights/components | Succeeded | API-specific App Insights |

## Deletion Commands (Phase 13)

```powershell
# Delete appi-chronicis-dev
az monitor app-insights component delete `
  --resource-group rg-chronicis-dev `
  --app appi-chronicis-dev

# Delete app-chronicis-api
az monitor app-insights component delete `
  --resource-group rg-chronicis-dev `
  --app app-chronicis-api
```

## Pre-Deletion Checklist

Before deleting these resources in Phase 13:

- [ ] DataDog has been running successfully for 24-48 hours
- [ ] All expected telemetry is appearing in DataDog (logs, traces, RUM)
- [ ] No gaps in monitoring coverage identified
- [ ] Team has reviewed DataDog dashboards and confirmed completeness
- [ ] Backup of any critical App Insights queries or dashboards completed
- [ ] Connection strings removed from Key Vault
- [ ] All code references to Application Insights removed

## Associated Resources to Clean Up

### Key Vault Secrets
Check for and remove these secrets from Key Vault:
```powershell
# List all secrets to identify App Insights related ones
az keyvault secret list --vault-name <keyvault-name> --output table

# Example secrets that may need deletion:
# - ApplicationInsights--ConnectionString
# - ApplicationInsights--InstrumentationKey
```

### NuGet Package References
Search and remove from .csproj files:
- `Microsoft.ApplicationInsights.AspNetCore`
- `Microsoft.ApplicationInsights`
- Any other `Microsoft.ApplicationInsights.*` packages

### Configuration Files
Remove Application Insights configuration from:
- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Production.json`

## Cost Impact

**Before Migration:**
- Application Insights data ingestion: ~$5-10/month

**After Migration:**
- Application Insights: $0 (deleted)
- DataDog: $0 (free tier)

**Estimated Monthly Savings:** $5-10

## Notes

- Both resources are in the same resource group (rg-chronicis-dev)
- Different locations (westus vs westus2) - likely created at different times
- Monitor DataDog for at least 1 week before deletion (recommend extending soak period from plan's 24-48 hours)
