# ByteSync Client Deployment Documentation

## Technology and Requirements

These projects are written in **C#** and target a specific version of **.NET**. You can find the exact .NET version required by checking the `.csproj` file of the **ByteSync.Client** project. You must have the appropriate .NET SDK installed to build and deploy the solution.

## Cloning and Building from Source

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/POW-Software/ByteSync.git
   ```

   Navigate to the `ByteSync.Client` folder if needed.

2. **Open the C# Project**:

   - Open the `.sln` (solution) file in Visual Studio or Visual Studio Code.
   - Verify that the .NET version specified in the `.csproj` is installed on your system.

3. **Create `local.settings.json`** at the project root. Example content:

   ```json
   {
     "LocalDebugUrl": "",
     "DevelopmentUrl": "",
  "StagingUrl": "",
  "ProductionUrl": "",
  "UpdatesDefinitionUrl": "",
  "AnnouncementsDefinitionUrl": ""
  }
  ```

   Fill in any relevant URLs or other settings your environment requires.

4. **Build the Project**:

   - Use the command line:
     ```bash
     dotnet build
     ```
   - Or build directly through Visual Studio / VS Code.

5. **Run the Client**:

   ```bash
   dotnet run
   ```

