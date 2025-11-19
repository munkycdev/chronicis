# Chronicis - Phase 0: Infrastructure & Setup

Welcome to Chronicis! This is your Phase 0 starter package with everything you need to get up and running.

## 🎯 Phase 0 Goals

- ✅ Establish Azure infrastructure  
- ✅ Create working Blazor WASM + Azure Functions skeleton
- ✅ Configure MudBlazor with Chronicis theme
- ✅ Implement health check endpoint
- ✅ Verify end-to-end connectivity

## 📋 Prerequisites

Before you begin, ensure you have:

1. **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **Azure Functions Core Tools** - [Install Guide](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
3. **Visual Studio 2022** or **VS Code** with C# extension
4. **Azure CLI** (for infrastructure setup) - [Install](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
5. **SQL Server** - LocalDB, Express, or Docker

### Verify Prerequisites

```bash
dotnet --version          # Should be 8.0.x or higher
func --version            # Should be 4.x
az --version              # Azure CLI
```

## 🚀 Quick Start (Local Development)

### Step 1: Restore Dependencies

```bash
# From the chronicis-phase0 directory
dotnet restore
```

### Step 2: Start the Azure Functions API

```bash
# Open a terminal
cd src/Chronicis.Api
func start
```

You should see:
```
Functions:
    Health: [GET] http://localhost:7071/api/health
```

### Step 3: Start the Blazor Client

```bash
# Open a NEW terminal
cd src/Chronicis.Client
dotnet run
```

You should see:
```
Now listening on: https://localhost:5001
```

### Step 4: Test the Application

1. Open your browser to `https://localhost:5001`
2. You should see the Chronicis welcome page
3. The health check should show "✓ API is healthy!"

If you see the success message, **Phase 0 is complete!** 🎉

## 🏗️ Project Structure

```
chronicis-phase0/
├── Chronicis.sln                    # Solution file
├── src/
│   ├── Chronicis.Client/            # Blazor WebAssembly
│   │   ├── Layout/
│   │   │   └── MainLayout.razor     # App layout with MudBlazor theme
│   │   ├── Pages/
│   │   │   └── Home.razor           # Home page with health check
│   │   ├── wwwroot/
│   │   │   ├── index.html
│   │   │   └── appsettings.json     # API URL configuration
│   │   └── Program.cs               # Client startup
│   │
│   ├── Chronicis.Api/               # Azure Functions
│   │   ├── Functions/
│   │   │   └── HealthFunction.cs    # Health check endpoint
│   │   ├── host.json
│   │   ├── local.settings.json      # Local configuration
│   │   └── Program.cs               # API startup
│   │
│   └── Chronicis.Shared/            # Shared models
│       └── Models/
│           └── HealthCheckResponse.cs
```

## 🎨 Chronicis Theme

The MudBlazor theme is configured in `MainLayout.razor` with these colors:

- **Primary (Beige-Gold):** `#C4AF8E`
- **Secondary (Slate Grey):** `#3A4750`  
- **AppBar/Drawer (Deep Blue-Grey):** `#1F2A33`
- **Background (Soft Off-White):** `#F4F0EA`

## ☁️ Azure Infrastructure Setup (Optional for Phase 0)

For local development, you don't need Azure yet. When you're ready to deploy:

### Create Azure Resources

```bash
# Login to Azure
az login

# Set variables
RESOURCE_GROUP="rg-chronicis-dev"
LOCATION="eastus"
SQL_SERVER="sql-chronicis-dev"
SQL_DB="Chronicis"
KEYVAULT="kv-chronicis-dev"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create SQL Server
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user chronicis-admin \
  --admin-password 'YourSecurePassword123!'

# Create database
az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DB \
  --service-objective Basic

# Allow Azure services
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Create Key Vault
az keyvault create \
  --name $KEYVAULT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

### Create Static Web App

Use the Azure Portal or GitHub integration to create a Static Web App. It will auto-configure GitHub Actions for deployment.

## 🗄️ Database Setup

### Option 1: SQL Server LocalDB (Windows)

Already configured in `local.settings.json`:

```json
"ConnectionStrings:ChronicisDb": "Server=(localdb)\\mssqllocaldb;Database=Chronicis;Trusted_Connection=True;MultipleActiveResultSets=true"
```

Start LocalDB:
```bash
sqllocaldb start mssqllocaldb
```

### Option 2: SQL Server Express

Install SQL Server Express, then update connection string in `local.settings.json`:

```json
"ConnectionStrings:ChronicisDb": "Server=localhost\\SQLEXPRESS;Database=Chronicis;Trusted_Connection=True;MultipleActiveResultSets=true"
```

### Option 3: Docker SQL Server

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name sql-server \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

Connection string:
```json
"ConnectionStrings:ChronicisDb": "Server=localhost,1433;Database=Chronicis;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

### Option 4: Azure SQL Database

Get connection string from Azure Portal and add to `local.settings.json`.

## 🧪 Testing the Health Check

### Via Browser
Navigate to: `http://localhost:7071/api/health`

Expected response:
```json
{
  "status": "Healthy",
  "message": "API is healthy!",
  "timestamp": "2025-11-18T12:34:56.789Z"
}
```

### Via curl
```bash
curl http://localhost:7071/api/health
```

### Via the Blazor App
The home page automatically calls the health check and displays the result.

## 🔧 Common Issues & Solutions

### Issue: "dotnet command not found"
**Solution:** Install .NET 8 SDK and restart your terminal.

### Issue: "func: command not found"  
**Solution:** Install Azure Functions Core Tools:
```bash
npm install -g azure-functions-core-tools@4
```

### Issue: Port 5001 or 7071 already in use
**Solution:** Kill the process or use different ports:
```bash
# Kill process on Windows
netstat -ano | findstr :7071
taskkill /PID <PID> /F

# Kill process on Mac/Linux
lsof -i :7071
kill -9 <PID>
```

### Issue: SQL Server connection failed
**Solution:** 
1. Verify SQL Server is running
2. Check connection string in `local.settings.json`
3. Ensure database exists

### Issue: CORS error when calling API
**Solution:** Azure Functions automatically handles CORS for Static Web Apps. For local dev, it should work by default. If not, ensure both are running.

### Issue: "Cannot find MudBlazor components"
**Solution:** Ensure you ran `dotnet restore`. Try:
```bash
dotnet clean
dotnet restore
dotnet build
```

## 📝 Development Workflow

1. **Start Functions API first** (in one terminal)
2. **Start Blazor Client** (in another terminal)  
3. **Make changes** to code
4. **Hot reload** should work for Blazor
5. **Restart Functions** if you change API code
6. **Commit frequently** to Git

## ✅ Phase 0 Success Criteria

You've completed Phase 0 when:

- [x] Blazor app runs at `https://localhost:5001`
- [x] Azure Functions at `http://localhost:7071`
- [x] Health check endpoint returns "Healthy"
- [x] Client successfully calls API
- [x] MudBlazor theme displays correctly
- [x] No console errors

## 🎯 Next Steps: Phase 1

Once Phase 0 is complete, you're ready for **Phase 1: Core Data Model & Tree Navigation**!

Phase 1 will add:
- Article entity and database
- Hierarchical tree structure  
- Read-only tree navigation
- Article detail view

## 📚 Useful Resources

- [Blazor WebAssembly Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Azure Functions Docs](https://learn.microsoft.com/en-us/azure/azure-functions/)
- [MudBlazor Components](https://mudblazor.com/components/list)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

## 🤝 Getting Help

If you encounter issues:

1. Check the troubleshooting section above
2. Review the implementation plan
3. Check the specs in `/mnt/project/`
4. Ask Claude for help with specific errors

## 📄 License

Part of the Chronicis project - modify as needed!

---

**Ready to start Phase 1?** Create a new chat with Claude, upload the implementation plan and specs, and say: *"I've completed Phase 0 and I'm ready to start Phase 1."*

Good luck! 🐉📖
