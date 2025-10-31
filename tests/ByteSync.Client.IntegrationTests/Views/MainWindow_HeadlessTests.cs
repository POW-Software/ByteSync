using Avalonia.ReactiveUI;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Views;
using FluentAssertions;
using ReactiveUI;
using Splat;

namespace ByteSync.Client.IntegrationTests.Views;

public class MainWindow_HeadlessTests : HeadlessIntegrationTest
{
    [SetUp]
    public void Setup()
    {
        // Register AvaloniaActivationForViewFetcher for ReactiveUI in headless mode
        // This is required for ReactiveWindow to work in headless tests
        // ReactiveUI uses Splat's service locator to find IActivationForViewFetcher
        Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(), typeof(IActivationForViewFetcher));
        
        BuildMoqContainer();
    }

    [Test]
    public async Task MainWindow_CanBeInstantiatedInHeadlessMode()
    {
        // Arrange & Act
        await ExecuteOnUiThread(async () =>
        {
            var mainWindow = new MainWindow();
            
            // Assert - verify the window can be created
            mainWindow.Should().NotBeNull();
        });
    }
    
    [Test]
    public async Task MainWindow_ImplementsIFileDialogService()
    {
        // Arrange & Act
        await ExecuteOnUiThread(async () =>
        {
            var mainWindow = new MainWindow();
            
            // Assert - verify the window properly implements the interface
            mainWindow.Should().NotBeNull();
            mainWindow.Should().BeAssignableTo<ByteSync.Interfaces.IFileDialogService>();
            
            // Verify the methods are available (compile-time check that they exist)
            var fileDialogService = (ByteSync.Interfaces.IFileDialogService)mainWindow;
            fileDialogService.Should().NotBeNull();
        });
    }
    
    [Test]
    public async Task ShowOpenFileDialogAsync_InHeadlessMode_ReturnsNullWithoutCrashing()
    {
        // This test validates that the method using TryGetLocalPath() handles
        // the headless scenario gracefully without throwing exceptions
        await ExecuteOnUiThread(async () =>
        {
            var mainWindow = new MainWindow();
            
            // Act - Call the actual method we fixed
            var result = await mainWindow.ShowOpenFileDialogAsync("Select Files", allowMultiple: true);
            
            // Assert - In headless mode, no dialog can be shown so it returns null
            // The important part is that it doesn't crash with InvalidOperationException
            result.Should().BeNull("StorageProvider is not available in headless mode");
        });
    }
    
    [Test]
    public async Task ShowOpenFolderDialogAsync_InHeadlessMode_ReturnsNullWithoutCrashing()
    {
        // This test validates that the method using TryGetLocalPath() handles
        // the headless scenario gracefully without throwing exceptions
        await ExecuteOnUiThread(async () =>
        {
            var mainWindow = new MainWindow();
            
            // Act - Call the actual method we fixed (this was the one that crashed with c:\)
            var result = await mainWindow.ShowOpenFolderDialogAsync("Select Folder");
            
            // Assert - In headless mode, no dialog can be shown so it returns null
            // The important part is that it doesn't crash with InvalidOperationException
            // which was the original bug when selecting root directories like c:\
            result.Should().BeNull("StorageProvider is not available in headless mode");
        });
    }
}


