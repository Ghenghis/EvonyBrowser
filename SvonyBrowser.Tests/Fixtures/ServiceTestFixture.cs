using System;
using System.IO;

namespace SvonyBrowser.Tests.Fixtures;

/// <summary>
/// Base fixture for service tests providing common setup and teardown.
/// </summary>
public class ServiceTestFixture : IDisposable
{
    protected readonly string TestDataPath;
    protected readonly string TestLogPath;
    protected readonly string TestConfigPath;
    
    public ServiceTestFixture()
    {
        // Create isolated test directories
        var testRoot = Path.Combine(Path.GetTempPath(), "SvonyBrowserTests", Guid.NewGuid().ToString());
        TestDataPath = Path.Combine(testRoot, "data");
        TestLogPath = Path.Combine(testRoot, "logs");
        TestConfigPath = Path.Combine(testRoot, "config");
        
        Directory.CreateDirectory(TestDataPath);
        Directory.CreateDirectory(TestLogPath);
        Directory.CreateDirectory(TestConfigPath);
    }
    
    public virtual void Dispose()
    {
        // Cleanup test directories
        try
        {
            var testRoot = Path.GetDirectoryName(TestDataPath);
            if (testRoot != null && Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

/// <summary>
/// Collection fixture for sharing state across tests in a collection.
/// </summary>
public class SharedServiceFixture : IDisposable
{
    public bool IsInitialized { get; private set; }
    
    public SharedServiceFixture()
    {
        IsInitialized = true;
    }
    
    public void Dispose()
    {
        IsInitialized = false;
    }
}

[CollectionDefinition("ServiceTests")]
public class ServiceTestCollection : ICollectionFixture<SharedServiceFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
