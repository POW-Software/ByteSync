# ByteSync Server Deployment Guide

This guide provides instructions for deploying the **ByteSync Server** components on Azure, including the **Azure Function**, **Redis**, **SignalR**, and storage solutions.

---

## Technology and Requirements

These projects are written in **C#** and target a specific version of **.NET**. You can find the exact .NET version required by checking the corresponding `.csproj` files. You must have the appropriate .NET SDK installed to build and deploy the solution.

## Components to Deploy

1. **Azure Function**

   - **Windows-only** deployment (currently not tested on Linux).
   - This is the main entry point for ByteSync's server-side functionality, handling requests and orchestrating other services.
   - Deployed from the `ByteSync.Functions` project.
   - You can deploy via the Azure Portal or from your local machine (Visual Studio / CLI). Make sure to configure your connection strings and secrets.

2. **Redis**

   - An in-memory data store used for caching and rapid data retrieval.
   - Useful for session management, real-time messaging, or other scenarios where speed is critical.
   - You can specify a prefix (`Prefix`) in configuration to logically group or namespace your cache keys.

3. **SignalR**

   - Real-time communication service for handling live updates and messaging between clients and server.
   - Enables real-time synchronization features in ByteSync.

4. **AzureBlobStorage or Cloudflare R2**

   - Storage solution for files and data.
   - You can choose between Azure Blob Storage or Cloudflare R2 depending on your preferences and requirements.
   - Configuration depends on which storage provider you select.

---

## Projects Requiring Configuration

The following projects require configuration to function properly:

- **ByteSync.Functions**
- **ByteSync.Functions.IntegrationTests**

## Configuration Storage Options

You can store and manage application settings for these projects in several ways:

- **local.settings.json (for local development):** Typically used for local debugging and testing. They can be filled in based on the associated template files.
- **Azure App Configuration:** A dedicated service for hosting and managing settings in Azure (optional, but if not used, everything must be stored in configuration files).
- **.NET User Secrets:** A secure way to store local secrets without committing them to source control.

> **Important:** All `local.settings.json` files are automatically excluded by `.gitignore` using the pattern `**/*local.settings.json`. Ensure you keep it that way and never commit these files to source control.

---

## Template Configuration Files

The following template files must be properly filled with your test/development values:

- **src/ByteSync.Functions/local.settings.template.json** - Main Azure Function configuration template
- **tests/ByteSync.Functions.IntegrationTests/functions-integration-tests.local.settings.template.json** - Integration tests configuration template

Copy these template files, remove the `.template` suffix, and fill them with your actual configuration values.

---

## Required Configuration Properties

The configuration properties that must be set include:

- **Redis:** Connection string and prefix for caching
- **SignalR:** Connection string for real-time communication
- **AzureBlobStorage or CloudflareR2:** Storage provider configuration (choose one)
- **AppSettings:** Application-specific settings including a unique secret

**`AppSettings:Secret`** should be a unique random string (for example, generated via `openssl rand -base64 32`) to secure tokens or other sensitive operations.

Refer to the template files mentioned above for the exact structure and property names.

---

## Configuration Options

### 1. Using Template Files

1. Copy the template files and remove the `.template` suffix:
   ```bash
   # For ByteSync.Functions
   cp src/ByteSync.Functions/local.settings.template.json src/ByteSync.Functions/local.settings.json
   
   # For ByteSync.Functions.IntegrationTests
   cp tests/ByteSync.Functions.IntegrationTests/functions-integration-tests.local.settings.template.json tests/ByteSync.Functions.IntegrationTests/functions-integration-tests.local.settings.json
   ```

2. Edit the copied files and fill in your actual configuration values.

### 2. Using .NET User Secrets

For secure storage of secrets during local development, you can use **[.NET Secret Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)**.

Navigate to each project directory and set the secrets according to the structure shown in the template files:

```bash
# Navigate to the ByteSync.Functions project directory
cd src/ByteSync.Functions

# Set secrets according to the template structure
dotnet user-secrets set "AppSettings:Secret" "YOUR_UNIQUE_RANDOM_SEED"
# ... add other secrets as needed
```

Repeat for **ByteSync.Functions.IntegrationTests** by navigating to `tests/ByteSync.Functions.IntegrationTests` and setting the same secrets.

---

### 3. Using Azure App Configuration

1. **Create an Azure App Configuration** resource via the Azure Portal.
2. **Add** the configuration keys as shown in the template files.
3. **Link** your Azure Function to the App Configuration resource.
4. Ensure your Azure Function is configured to pull values from Azure App Configuration.

---

## Final Steps

1. **Deploy** your Azure Function (Windows) to Azure.
2. **Create** your Redis and SignalR resources.
3. **Set up** your chosen storage solution (Azure Blob Storage or Cloudflare R2).
4. **Verify** that your secrets and connection strings are correctly configured (via local or remote settings).
5. **Test** the deployment to confirm everything is working.

---

## Best Practices

- **Security:** Never commit secrets (e.g. `local.settings.json`) to source control.
- **Production Secrets:** For production, prefer environment variables, Azure Key Vault, or Azure App Configuration.
- **Monitoring:** Enable Azure monitoring for real-time logs and alerts.

For issues or questions, please open an issue on the [ByteSync GitHub repository](https://github.com/POW-Software/ByteSync).