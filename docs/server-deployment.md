# ByteSync Server Deployment Guide

This guide provides instructions for deploying the **ByteSync Server** components on Azure, including the **Azure Function**, **Azure Cosmos DB**, and **Redis**.

---

## Technology and Requirements

These projects are written in **C#** and target a specific version of **.NET**. You can find the exact .NET version required by checking the corresponding `.csproj` files. You must have the appropriate .NET SDK installed to build and deploy the solution.

## Components to Deploy

1. **Azure Function**

   - **Windows-only** deployment (currently not tested on Linux).
   - This is the main entry point for ByteSync’s server-side functionality, handling requests and orchestrating other services.
   - Deployed from the `ByteSync.Functions` project.
   - You can deploy via the Azure Portal or from your local machine (Visual Studio / CLI). Make sure to configure your connection strings and secrets.

2. **Azure Cosmos DB**

   - A globally distributed, multi-model database.
   - Stores persistent data for ByteSync (such as metadata, user data, or other domain-related information).
   - The necessary containers may be auto-created on startup if configured properly.

3. **Redis**

   - An in-memory data store used for caching and rapid data retrieval.
   - Useful for session management, real-time messaging, or other scenarios where speed is critical.
   - You can specify a prefix (`Prefix`) in configuration to logically group or namespace your cache keys.

---

## Projects Requiring Configuration

The following projects require configuration to function properly:

- **ByteSync.Functions**
- **ByteSync.Functions.IntegrationTests**
- **ByteSync.ServerCommon.Tests**

You can store and manage application settings for these projects in several ways:

- **Azure App Configuration:** A dedicated service for hosting and managing settings in Azure. Only for the **ByteSync.Functions** project.
- **local.settings.json (for local development):** Typically used for local debugging and testing.
- **.NET User Secrets:** A secure way to store local secrets without committing them to source control.

---

## Required Configuration Properties

Below is the JSON structure indicating which properties must be set. In particular, **`AppSettings:Secret`** should be a unique random string (for example, generated via `openssl rand -base64 32`) to secure tokens or other sensitive operations:

```json
{
  "Redis": {
    "ConnectionString": "",
    "Prefix": ""
  },
  "BlobStorage": {
    "Endpoint": "",
    "Container": "",
    "AccountKey": "",
    "AccountName": ""
  },
  "SignalR": {
    "ConnectionString": ""
  },
  "CosmosDb": {
    "ConnectionString": "",
    "DatabaseName": ""
  },
  "AppSettings": {
    "Secret": "YOUR_UNIQUE_RANDOM_SEED",
    "AnnouncementsUrl": ""
  }
}
```

---

## Configuration Options

### 1. Using `local.settings.json`

`local.settings.json` is typically used for local development. An example might look like this:

```json
{
  "IsEncrypted": false,
  "Values": {
    "Redis:ConnectionString": "<your-redis-connection-string>",
    "Redis:Prefix": "<your-redis-prefix>",
    "BlobStorage:Endpoint": "<your-blob-endpoint>",
    "BlobStorage:Container": "<your-container-name>",
    "BlobStorage:AccountKey": "<your-account-key>",
    "BlobStorage:AccountName": "<your-account-name>",
    "SignalR:ConnectionString": "<your-signalr-connection-string>",
    "CosmosDb:ConnectionString": "<your-cosmosdb-connection-string>",
    "CosmosDb:DatabaseName": "<your-database-name>",
    "AppSettings:Secret": "YOUR_UNIQUE_RANDOM_SEED",
    "AppSettings:AnnouncementsUrl": ""
  }
}
```

> **Important:** `local.settings.json` is automatically excluded by default in `.gitignore` so it won't be committed to source control. Ensure you keep it that way. If you see it in your changes, verify that the ignore rules haven't been modified.

---

### 2. Using .NET User Secrets

For secure storage of secrets during local development, you can use **[.NET Secret Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)**.

Below is an example of how to set these secrets for **ByteSync.Functions**:

```bash
# Navigate to the ByteSync.Functions project directory
cd ByteSync.Functions

# Set each secret
# Redis
  dotnet user-secrets set "Redis:ConnectionString" "<your-redis-connection-string>"
  dotnet user-secrets set "Redis:Prefix" "<your-redis-prefix>"

# Blob Storage
  dotnet user-secrets set "BlobStorage:Endpoint" "<your-blob-endpoint>"
  dotnet user-secrets set "BlobStorage:Container" "<your-container-name>"
  dotnet user-secrets set "BlobStorage:AccountKey" "<your-account-key>"
  dotnet user-secrets set "BlobStorage:AccountName" "<your-account-name>"

# SignalR
  dotnet user-secrets set "SignalR:ConnectionString" "<your-signalr-connection-string>"

# Cosmos DB
  dotnet user-secrets set "CosmosDb:ConnectionString" "<your-cosmosdb-connection-string>"
  dotnet user-secrets set "CosmosDb:DatabaseName" "<your-database-name>"

# App Settings
  dotnet user-secrets set "AppSettings:Secret" "YOUR_UNIQUE_RANDOM_SEED"
  dotnet user-secrets set "AppSettings:AnnouncementsUrl" ""
```

Repeat these steps for **ByteSync.Functions.IntegrationTests** and **ByteSync.ServerCommon.Tests**, navigating to each project's directory and setting the same secrets.

---

### 3. Using Azure App Configuration

1. **Create an Azure App Configuration** resource via the Azure Portal.
2. **Add** the keys listed above (e.g., `Redis:ConnectionString`, `BlobStorage:Endpoint`, etc.).
3. **Link** your Azure Function to the App Configuration resource.
4. Ensure your Azure Function is configured to pull values from Azure App Configuration.

---

## Final Steps

1. **Deploy** your Azure Function (Windows) to Azure.
2. **Create** your Cosmos DB and Redis resources.
3. **Verify** that your secrets and connection strings are correctly configured (via local or remote settings).
4. **Test** the deployment to confirm everything is working.

---

## Best Practices

- **Security:** Never commit secrets (e.g. `local.settings.json`) to source control.
- **Production Secrets:** For production, prefer environment variables, Azure Key Vault, or Azure App Configuration.
- **Monitoring:** Enable Azure monitoring for real-time logs and alerts.

For issues or questions, please open an issue on the [ByteSync GitHub repository](https://github.com/POW-Software/ByteSync).