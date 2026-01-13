using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SvonyBrowser.Tests.Helpers;

/// <summary>
/// Helper methods for unit tests.
/// These methods create SAMPLE test data to test REAL service implementations.
/// No mocking frameworks are used - all tests exercise actual production code.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a sample packet with the specified action and data.
    /// This is test data to feed into real packet processing code.
    /// </summary>
    public static JObject CreateSamplePacket(string action, object data)
    {
        return new JObject
        {
            ["action"] = action,
            ["data"] = JToken.FromObject(data),
            ["timestamp"] = DateTime.UtcNow.ToString("o")
        };
    }
    
    // Backward compatibility alias
    public static JObject CreateMockPacket(string action, object data) => CreateSamplePacket(action, data);
    
    /// <summary>
    /// Creates sample resource data for testing.
    /// </summary>
    public static Dictionary<string, long> CreateSampleResources(
        long gold = 1000000,
        long food = 500000,
        long lumber = 300000,
        long stone = 200000,
        long iron = 100000)
    {
        return new Dictionary<string, long>
        {
            ["gold"] = gold,
            ["food"] = food,
            ["lumber"] = lumber,
            ["stone"] = stone,
            ["iron"] = iron
        };
    }
    
    // Backward compatibility alias
    public static Dictionary<string, long> CreateMockResources(
        long gold = 1000000, long food = 500000, long lumber = 300000, 
        long stone = 200000, long iron = 100000) 
        => CreateSampleResources(gold, food, lumber, stone, iron);
    
    /// <summary>
    /// Creates sample troop data for testing.
    /// </summary>
    public static Dictionary<string, int> CreateSampleTroops(
        int infantry = 10000,
        int cavalry = 5000,
        int archers = 5000,
        int siege = 1000)
    {
        return new Dictionary<string, int>
        {
            ["infantry"] = infantry,
            ["cavalry"] = cavalry,
            ["archers"] = archers,
            ["siege"] = siege
        };
    }
    
    // Backward compatibility alias
    public static Dictionary<string, int> CreateMockTroops(
        int infantry = 10000, int cavalry = 5000, int archers = 5000, int siege = 1000)
        => CreateSampleTroops(infantry, cavalry, archers, siege);
    
    /// <summary>
    /// Creates a sample hero state for testing.
    /// </summary>
    public static JObject CreateSampleHero(int id, string name, int level = 30, int stars = 5)
    {
        return new JObject
        {
            ["id"] = id,
            ["name"] = name,
            ["level"] = level,
            ["stars"] = stars,
            ["attack"] = level * 100,
            ["defense"] = level * 80,
            ["politics"] = level * 50,
            ["leadership"] = level * 90
        };
    }
    
    // Backward compatibility alias
    public static JObject CreateMockHero(int id, string name, int level = 30, int stars = 5)
        => CreateSampleHero(id, name, level, stars);
    
    /// <summary>
    /// Creates a sample city state for testing.
    /// </summary>
    public static JObject CreateSampleCity(int id, string name, int x = 100, int y = 100)
    {
        return new JObject
        {
            ["id"] = id,
            ["name"] = name,
            ["x"] = x,
            ["y"] = y,
            ["level"] = 35,
            ["resources"] = JToken.FromObject(CreateSampleResources()),
            ["troops"] = JToken.FromObject(CreateSampleTroops())
        };
    }
    
    // Backward compatibility alias
    public static JObject CreateMockCity(int id, string name, int x = 100, int y = 100)
        => CreateSampleCity(id, name, x, y);
    
    /// <summary>
    /// Creates a sample march state for testing.
    /// </summary>
    public static JObject CreateSampleMarch(int id, string type, int fromX, int fromY, int toX, int toY)
    {
        return new JObject
        {
            ["id"] = id,
            ["type"] = type,
            ["fromX"] = fromX,
            ["fromY"] = fromY,
            ["toX"] = toX,
            ["toY"] = toY,
            ["startTime"] = DateTime.UtcNow.AddMinutes(-5).ToString("o"),
            ["arrivalTime"] = DateTime.UtcNow.AddMinutes(10).ToString("o"),
            ["status"] = "marching"
        };
    }
    
    // Backward compatibility alias
    public static JObject CreateMockMarch(int id, string type, int fromX, int fromY, int toX, int toY)
        => CreateSampleMarch(id, type, fromX, fromY, toX, toY);
    
    /// <summary>
    /// Creates a temporary file with content.
    /// </summary>
    public static string CreateTempFile(string content, string extension = ".json")
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}{extension}");
        File.WriteAllText(path, content);
        return path;
    }
    
    /// <summary>
    /// Waits for a condition with timeout.
    /// </summary>
    public static async Task<bool> WaitForConditionAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var deadline = DateTime.UtcNow + timeout;
        
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;
            
            await Task.Delay(interval);
        }
        
        return false;
    }
    
    /// <summary>
    /// Generates random string of specified length.
    /// </summary>
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

/// <summary>
/// Test logger for capturing log output during tests.
/// This is NOT a mock - it's a real logger implementation for test scenarios.
/// </summary>
public class TestLogger
{
    public List<string> Messages { get; } = new();
    public List<Exception> Errors { get; } = new();
    
    public void Information(string message, params object[] args)
    {
        Messages.Add(string.Format(message.Replace("{", "{{").Replace("}", "}}"), args));
    }
    
    public void Warning(string message, params object[] args)
    {
        Messages.Add($"[WARN] {string.Format(message.Replace("{", "{{").Replace("}", "}}"), args)}");
    }
    
    public void Error(Exception ex, string message, params object[] args)
    {
        Errors.Add(ex);
        Messages.Add($"[ERROR] {string.Format(message.Replace("{", "{{").Replace("}", "}}"), args)}: {ex.Message}");
    }
    
    public void Debug(string message, params object[] args)
    {
        Messages.Add($"[DEBUG] {string.Format(message.Replace("{", "{{").Replace("}", "}}"), args)}");
    }
}

// Backward compatibility alias
public class MockLogger : TestLogger { }
