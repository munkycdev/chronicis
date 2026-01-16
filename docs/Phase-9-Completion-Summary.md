# Phase 9 Completion Summary

**Completed:** January 16, 2026  
**Phase:** Update GitHub Actions Deployment

## Changes Made

### 1. Created New Client Deployment Workflow

**New File:** `.github/workflows/deploy-client.yml`

```yaml
name: Deploy Chronicis Client

on:
  push:
    branches:
      - main
    paths:
      - 'src/Chronicis.Client/**'
      - 'src/Chronicis.Client.Host/**'
      - 'src/Chronicis.Shared/**'
      - '.github/workflows/deploy-client.yml'
  workflow_dispatch:

env:
  DOTNET_VERSION: '9.0.x'
  CLIENT_HOST_PROJECT_PATH: 'src/Chronicis.Client.Host/Chronicis.Client.Host.csproj'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - Checkout repository
      - Setup .NET 9.0
      - Restore dependencies
      - Build (Release)
      - Publish
      - Deploy to Azure Web App (chronicis-client)
```

### 2. Verified Existing API Deployment Workflow

**File:** `.github/workflows/deploy-api.yml`

Already configured correctly:
- ✅ Builds Chronicis.Api
- ✅ Deploys to app-chronicis-api
- ✅ Triggers on API/Shared changes
- ✅ Uses AZURE_API_PUBLISH_PROFILE secret

### 3. Configured GitHub Secrets

Added two publish profile secrets:
- `AZURE_API_PUBLISH_PROFILE` - For app-chronicis-api deployment
- `AZURE_CLIENT_PUBLISH_PROFILE` - For chronicis-client deployment

**Security:**
- Publish profiles contain deployment credentials
- Stored as encrypted GitHub secrets
- Never exposed in workflow logs
- Scoped to repository only

## Deployment Architecture

### Workflow Triggers

**API Deployment** (deploy-api.yml):
- Triggers on changes to:
  - `src/Chronicis.Api/**`
  - `src/Chronicis.Shared/**`
  - `.github/workflows/deploy-api.yml`
- Manual trigger available

**Client Deployment** (deploy-client.yml):
- Triggers on changes to:
  - `src/Chronicis.Client/**`
  - `src/Chronicis.Client.Host/**`
  - `src/Chronicis.Shared/**`
  - `.github/workflows/deploy-client.yml`
- Manual trigger available

**Smart Triggers:**
- Changes to Shared project trigger BOTH deployments
- API-only changes deploy only API
- Client-only changes deploy only Client

### Deployment Flow

```
Git Push to main
       │
       ├─── API Changes Detected?
       │    └─── Yes → Build & Deploy API
       │
       └─── Client Changes Detected?
            └─── Yes → Build & Deploy Client

Each deployment:
1. Checkout code
2. Setup .NET 9
3. Restore NuGet packages
4. Build (Release mode)
5. Publish to folder
6. Deploy to Azure App Service
```

## Workflow Comparison

### Before Phase 9
- API: Deployed via deploy-api.yml ✅
- Client: Deployed via Azure Static Web Apps ❌

### After Phase 9
- API: Deployed via deploy-api.yml ✅
- Client: Deployed via deploy-client.yml ✅
- Both use same pattern
- Independent deployments

## Verification Steps

After push, verify deployments:

### 1. Check GitHub Actions
- Go to: https://github.com/{your-repo}/actions
- Verify both workflows ran successfully
- Check for any errors in logs

### 2. Verify API Deployment
```powershell
# Test health endpoint
curl https://app-chronicis-api.azurewebsites.net/health
```

Expected: JSON response with health status

### 3. Verify Client Deployment
- Open: https://chronicis-client.azurewebsites.net
- Should load Blazor WASM application
- Check browser console for errors
- Verify can log in (Auth0)

### 4. Verify Integration
- Log into client
- Try to create/view worlds and articles
- Check Network tab - API calls should go to app-chronicis-api
- No CORS errors expected

### 5. Check DataDog
After ~5 minutes:
- Logs: https://app.datadoghq.com/logs?query=service%3Achronicis-api
- APM: https://app.datadoghq.com/apm/traces?query=service%3Achronicis-api
- RUM: https://app.datadoghq.com/rum/sessions?query=service%3Achronicis-client

## Static Web Apps Status

**Important:** The Static Web Apps workflow still exists:
- File: `azure-static-web-apps-ambitious-mushroom-015091e1e.yml`
- Status: Still active (will be removed in Phase 14)
- Purpose: Allows gradual migration testing
- Can run alongside new App Service deployments

The SWA workflow will continue deploying to the old URL until Phase 14.

## Troubleshooting

### If API Deployment Fails

Check:
1. Publish profile secret is correct
2. App Service name matches: `app-chronicis-api`
3. .NET version matches project: 9.0
4. Build logs for compilation errors

### If Client Deployment Fails

Check:
1. Publish profile secret is correct  
2. App Service name matches: `chronicis-client`
3. Client.Host project builds successfully
4. All project references resolve

### If Integration Fails

Check:
1. CORS settings on API (Phase 5)
2. Client appsettings.Production.json has correct API URL
3. ASPNETCORE_ENVIRONMENT=Production on client App Service
4. Auth0 configuration includes App Service URLs

## Build Performance

Typical build times:
- API: ~2-3 minutes
- Client: ~3-4 minutes (includes Blazor WASM compilation)
- Total: ~5-7 minutes for both

## Cost Impact

**No additional costs:**
- GitHub Actions: Free tier (2,000 minutes/month)
- Already covered by existing usage

## Next Steps

**Phase 10:** Local Testing
- Test API locally with DataDog
- Test Client Host locally
- Verify integration between components

**Phase 11:** Configure Custom Domains & SSL
- Point api.chronicis.app to app-chronicis-api
- Point chronicis.app to chronicis-client
- Configure SSL certificates

## Important Notes

### Publish Profiles vs Service Principals

We're using publish profiles (simpler) instead of service principals:
- **Pros:** Easy setup, works immediately
- **Cons:** Less granular permissions, need to regenerate if leaked
- **Alternative:** Azure Service Principal with RBAC (more secure, more complex)

For production, consider migrating to service principals.

### Workflow Independence

API and Client deploy independently:
- Can deploy API without touching Client
- Can deploy Client without touching API
- Shared project changes trigger both (ensures consistency)

### Manual Deployment

Both workflows support manual trigger:
1. Go to Actions tab
2. Select workflow
3. Click "Run workflow"
4. Choose branch
5. Click "Run workflow"

Useful for:
- Hotfixes
- Rollbacks
- Testing deployments

## Files Created/Modified

**Created:**
- `.github/workflows/deploy-client.yml`

**Modified:**
- None (API workflow already existed)

**GitHub Secrets Added:**
- `AZURE_API_PUBLISH_PROFILE`
- `AZURE_CLIENT_PUBLISH_PROFILE`

## Deployment URLs

### App Services (Azure)
- API: https://app-chronicis-api.azurewebsites.net
- Client: https://chronicis-client.azurewebsites.net

### Custom Domains (Phase 11)
- API: https://api.chronicis.app (to be configured)
- Client: https://chronicis.app (to be configured)

### Legacy (Phase 14 removal)
- Static Web App: https://ambitious-mushroom-015091e1e.5.azurestaticapps.net
