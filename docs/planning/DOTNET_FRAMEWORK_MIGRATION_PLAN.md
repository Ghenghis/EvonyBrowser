# .NET Framework 4.7.2 Migration Action Plan

## Overview

This document provides a comprehensive action plan to migrate Svony Browser from .NET 6 to .NET Framework 4.7.2 with CefSharp 84.4.10 for Flash support.

---

## Why This Migration is Required

| Framework | CefSharp Version | Chromium Version | Flash Support |
|-----------|------------------|------------------|---------------|
| .NET 6 | 119.4.30 | 119 | ❌ Removed |
| .NET Framework 4.7.2 | 84.4.10 | 84 | ✅ Supported |

**Flash was removed from Chromium starting with version 88 (January 2021).** To load SWF files, we MUST use CefSharp 84 which requires .NET Framework.

---

## API Compatibility Issues & Fixes

### 1. GetValueOrDefault (Dictionary Extension)

**Problem:** `dictionary.GetValueOrDefault(key)` doesn't exist in .NET Framework

**Files Affected:**
- Multiple service files using Dictionary lookups

**Fix - Create Extension Method:**
```csharp
// Add to Helpers/NetFrameworkExtensions.cs
public static class DictionaryExtensions
{
    public static TValue GetValueOrDefault<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary, 
        TKey key, 
        TValue defaultValue = default(TValue))
    {
        TValue value;
        return dictionary.TryGetValue(key, out value) ? value : defaultValue;
    }
    
    public static TValue GetValueOrDefault<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary, 
        TKey key, 
        TValue defaultValue = default(TValue))
    {
        TValue value;
        return dictionary.TryGetValue(key, out value) ? value : defaultValue;
    }
}
```

---

### 2. TakeLast (LINQ Extension)

**Problem:** `collection.TakeLast(n)` doesn't exist in .NET Framework

**Files Affected:**
- Services that get last N items from collections

**Fix - Create Extension Method:**
```csharp
public static class EnumerableExtensions
{
    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
    {
        if (source == null) throw new ArgumentNullException("source");
        if (count <= 0) return Enumerable.Empty<T>();
        
        var list = source as IList<T> ?? source.ToList();
        var skipCount = Math.Max(0, list.Count - count);
        return list.Skip(skipCount);
    }
    
    public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
    {
        if (source == null) throw new ArgumentNullException("source");
        if (count <= 0) return source;
        
        var list = source as IList<T> ?? source.ToList();
        var takeCount = Math.Max(0, list.Count - count);
        return list.Take(takeCount);
    }
}
```

---

### 3. File.WriteAllTextAsync / ReadAllTextAsync

**Problem:** Async file methods don't exist in .NET Framework

**Files Affected:**
- SettingsManager.cs
- SessionManager.cs
- Any file that uses async file I/O

**Fix - Create Async Wrappers:**
```csharp
public static class FileExtensions
{
    public static Task<string> ReadAllTextAsync(string path)
    {
        return Task.Run(() => File.ReadAllText(path));
    }
    
    public static Task WriteAllTextAsync(string path, string contents)
    {
        return Task.Run(() => File.WriteAllText(path, contents));
    }
    
    public static Task<byte[]> ReadAllBytesAsync(string path)
    {
        return Task.Run(() => File.ReadAllBytes(path));
    }
    
    public static Task WriteAllBytesAsync(string path, byte[] bytes)
    {
        return Task.Run(() => File.WriteAllBytes(path, bytes));
    }
    
    public static Task<string[]> ReadAllLinesAsync(string path)
    {
        return Task.Run(() => File.ReadAllLines(path));
    }
    
    public static Task WriteAllLinesAsync(string path, IEnumerable<string> contents)
    {
        return Task.Run(() => File.WriteAllLines(path, contents));
    }
    
    public static Task AppendAllTextAsync(string path, string contents)
    {
        return Task.Run(() => File.AppendAllText(path, contents));
    }
}
```

**Usage Change:**
```csharp
// Before (.NET 6)
await File.WriteAllTextAsync(path, content);

// After (.NET Framework)
await FileExtensions.WriteAllTextAsync(path, content);
```

---

### 4. Math.Clamp

**Problem:** `Math.Clamp(value, min, max)` doesn't exist in .NET Framework

**Files Affected:**
- CombatSimulator.cs
- Any calculations with bounds checking

**Fix - Create Helper Method:**
```csharp
public static class MathExtensions
{
    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
    
    public static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
    
    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
    
    public static long Clamp(long value, long min, long max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
```

**Usage Change:**
```csharp
// Before (.NET 6)
var result = Math.Clamp(value, 0, 100);

// After (.NET Framework)
var result = MathExtensions.Clamp(value, 0, 100);
```

---

### 5. SocketsHttpHandler

**Problem:** `SocketsHttpHandler` doesn't exist in .NET Framework

**Files Affected:**
- HttpClient configurations
- Proxy settings

**Fix - Use HttpClientHandler Instead:**
```csharp
// Before (.NET 6)
var handler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    UseProxy = true,
    Proxy = new WebProxy("127.0.0.1", 8888)
};

// After (.NET Framework)
var handler = new HttpClientHandler
{
    UseProxy = true,
    Proxy = new WebProxy("127.0.0.1", 8888)
};
```

---

### 6. Task.WaitAsync

**Problem:** `task.WaitAsync(timeout)` doesn't exist in .NET Framework

**Files Affected:**
- Async operations with timeouts

**Fix - Create Extension Method:**
```csharp
public static class TaskExtensions
{
    public static async Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout)
    {
        using (var cts = new CancellationTokenSource())
        {
            var delayTask = Task.Delay(timeout, cts.Token);
            var completedTask = await Task.WhenAny(task, delayTask);
            
            if (completedTask == delayTask)
            {
                throw new TimeoutException("The operation timed out.");
            }
            
            cts.Cancel(); // Cancel the delay task
            return await task;
        }
    }
    
    public static async Task WaitAsync(this Task task, TimeSpan timeout)
    {
        using (var cts = new CancellationTokenSource())
        {
            var delayTask = Task.Delay(timeout, cts.Token);
            var completedTask = await Task.WhenAny(task, delayTask);
            
            if (completedTask == delayTask)
            {
                throw new TimeoutException("The operation timed out.");
            }
            
            cts.Cancel();
            await task;
        }
    }
    
    public static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (cancellationToken.Register(() => tcs.TrySetResult(true)))
        {
            if (task != await Task.WhenAny(task, tcs.Task))
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
        return await task;
    }
}
```

---

### 7. SHA256.HashData

**Problem:** `SHA256.HashData(bytes)` static method doesn't exist in .NET Framework

**Files Affected:**
- Hashing operations

**Fix - Use Traditional Approach:**
```csharp
public static class HashExtensions
{
    public static byte[] ComputeSHA256Hash(byte[] data)
    {
        using (var sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(data);
        }
    }
    
    public static string ComputeSHA256HashString(byte[] data)
    {
        var hash = ComputeSHA256Hash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
    
    public static string ComputeSHA256HashString(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return ComputeSHA256HashString(bytes);
    }
}
```

**Usage Change:**
```csharp
// Before (.NET 6)
var hash = SHA256.HashData(bytes);

// After (.NET Framework)
var hash = HashExtensions.ComputeSHA256Hash(bytes);
```

---

### 8. KeyValuePair Deconstruction

**Problem:** `foreach (var (key, value) in dictionary)` doesn't work in .NET Framework

**Files Affected:**
- Dictionary iterations throughout codebase

**Fix - Add Deconstruct Extension:**
```csharp
public static class KeyValuePairExtensions
{
    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> kvp, 
        out TKey key, 
        out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }
}
```

**Alternative - Use Traditional Foreach:**
```csharp
// Before (.NET 6)
foreach (var (key, value) in dictionary)
{
    // use key and value
}

// After (.NET Framework) - Option 1: With extension
foreach (var (key, value) in dictionary)  // Works with Deconstruct extension
{
    // use key and value
}

// After (.NET Framework) - Option 2: Traditional
foreach (var kvp in dictionary)
{
    var key = kvp.Key;
    var value = kvp.Value;
    // use key and value
}
```

---

## Additional .NET 6 Features to Replace

### 9. Nullable Reference Types

**Problem:** `string?`, `object?` syntax not available

**Fix:** Remove nullable annotations or use `[CanBeNull]` attributes
```csharp
// Before (.NET 6)
public string? Name { get; set; }

// After (.NET Framework)
public string Name { get; set; }  // Just remove the ?
```

---

### 10. File-Scoped Namespaces

**Problem:** `namespace Foo;` syntax not available

**Fix:** Use block-scoped namespaces
```csharp
// Before (.NET 6)
namespace SvonyBrowser;

public class MyClass { }

// After (.NET Framework)
namespace SvonyBrowser
{
    public class MyClass { }
}
```

---

### 11. Global Usings

**Problem:** `global using` not available

**Fix:** Add using statements to each file or use a shared file included via `<Compile Include="GlobalUsings.cs" />`

---

### 12. Record Types

**Problem:** `record` keyword not available

**Fix:** Convert to classes with equality overrides
```csharp
// Before (.NET 6)
public record Person(string Name, int Age);

// After (.NET Framework)
public class Person
{
    public string Name { get; }
    public int Age { get; }
    
    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }
    
    public override bool Equals(object obj)
    {
        return obj is Person other && Name == other.Name && Age == other.Age;
    }
    
    public override int GetHashCode()
    {
        return (Name?.GetHashCode() ?? 0) ^ Age.GetHashCode();
    }
}
```

---

### 13. Init-Only Properties

**Problem:** `init` accessor not available

**Fix:** Use `set` or constructor-only assignment
```csharp
// Before (.NET 6)
public string Name { get; init; }

// After (.NET Framework)
public string Name { get; set; }  // Or make it readonly with constructor
```

---

### 14. Pattern Matching Enhancements

**Problem:** Advanced pattern matching not available

**Fix:** Use traditional if/switch statements
```csharp
// Before (.NET 6)
if (obj is string { Length: > 5 } s)

// After (.NET Framework)
var s = obj as string;
if (s != null && s.Length > 5)
```

---

### 15. Target-Typed New

**Problem:** `new()` without type not available

**Fix:** Specify the type explicitly
```csharp
// Before (.NET 6)
List<string> items = new();

// After (.NET Framework)
List<string> items = new List<string>();
```

---

## Complete Extension Class

Create this file: `Helpers/NetFrameworkExtensions.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SvonyBrowser.Helpers
{
    /// <summary>
    /// Extension methods to provide .NET Core/6 API compatibility in .NET Framework 4.7.2
    /// </summary>
    public static class NetFrameworkExtensions
    {
        #region Dictionary Extensions
        
        public static TValue GetValueOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary, 
            TKey key, 
            TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
        
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, 
            TKey key, 
            TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
        
        #endregion
        
        #region Enumerable Extensions
        
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (count <= 0) return Enumerable.Empty<T>();
            
            var list = source as IList<T> ?? source.ToList();
            var skipCount = Math.Max(0, list.Count - count);
            return list.Skip(skipCount);
        }
        
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (count <= 0) return source;
            
            var list = source as IList<T> ?? source.ToList();
            var takeCount = Math.Max(0, list.Count - count);
            return list.Take(takeCount);
        }
        
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
        
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(source, comparer);
        }
        
        #endregion
        
        #region File Extensions
        
        public static Task<string> ReadAllTextAsync(string path)
        {
            return Task.Run(() => File.ReadAllText(path));
        }
        
        public static Task WriteAllTextAsync(string path, string contents)
        {
            return Task.Run(() => File.WriteAllText(path, contents));
        }
        
        public static Task<byte[]> ReadAllBytesAsync(string path)
        {
            return Task.Run(() => File.ReadAllBytes(path));
        }
        
        public static Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            return Task.Run(() => File.WriteAllBytes(path, bytes));
        }
        
        public static Task<string[]> ReadAllLinesAsync(string path)
        {
            return Task.Run(() => File.ReadAllLines(path));
        }
        
        public static Task WriteAllLinesAsync(string path, IEnumerable<string> contents)
        {
            return Task.Run(() => File.WriteAllLines(path, contents.ToArray()));
        }
        
        public static Task AppendAllTextAsync(string path, string contents)
        {
            return Task.Run(() => File.AppendAllText(path, contents));
        }
        
        #endregion
        
        #region Math Extensions
        
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        public static long Clamp(long value, long min, long max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        #endregion
        
        #region Task Extensions
        
        public static async Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                var delayTask = Task.Delay(timeout, cts.Token);
                var completedTask = await Task.WhenAny(task, delayTask);
                
                if (completedTask == delayTask)
                {
                    throw new TimeoutException("The operation timed out.");
                }
                
                cts.Cancel();
                return await task;
            }
        }
        
        public static async Task WaitAsync(this Task task, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                var delayTask = Task.Delay(timeout, cts.Token);
                var completedTask = await Task.WhenAny(task, delayTask);
                
                if (completedTask == delayTask)
                {
                    throw new TimeoutException("The operation timed out.");
                }
                
                cts.Cancel();
                await task;
            }
        }
        
        #endregion
        
        #region Hash Extensions
        
        public static byte[] ComputeSHA256Hash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }
        
        public static string ComputeSHA256HashString(byte[] data)
        {
            var hash = ComputeSHA256Hash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        
        public static string ComputeSHA256HashString(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return ComputeSHA256HashString(bytes);
        }
        
        #endregion
        
        #region KeyValuePair Extensions
        
        public static void Deconstruct<TKey, TValue>(
            this KeyValuePair<TKey, TValue> kvp, 
            out TKey key, 
            out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
        
        #endregion
        
        #region String Extensions
        
        public static bool Contains(this string source, string value, StringComparison comparison)
        {
            return source.IndexOf(value, comparison) >= 0;
        }
        
        public static string[] Split(this string source, char separator, StringSplitOptions options)
        {
            return source.Split(new[] { separator }, options);
        }
        
        public static string[] Split(this string source, string separator, StringSplitOptions options)
        {
            return source.Split(new[] { separator }, options);
        }
        
        #endregion
    }
}
```

---

## Migration Steps

### Phase 1: Create Extension Methods (1-2 hours)
1. Create `Helpers/NetFrameworkExtensions.cs` with all compatibility methods
2. Add to project file

### Phase 2: Update Project File (30 minutes)
1. Change target framework to `net472`
2. Update CefSharp packages to 84.4.10
3. Remove .NET 6 specific settings

### Phase 3: Fix Syntax Issues (4-8 hours)
1. Convert file-scoped namespaces to block-scoped
2. Remove nullable reference type annotations (`?`)
3. Replace `new()` with explicit type
4. Replace `init` with `set`
5. Fix pattern matching expressions

### Phase 4: Fix API Calls (4-8 hours)
1. Replace `File.WriteAllTextAsync` with `FileExtensions.WriteAllTextAsync`
2. Replace `Math.Clamp` with `MathExtensions.Clamp`
3. Replace `SHA256.HashData` with `HashExtensions.ComputeSHA256Hash`
4. Replace `SocketsHttpHandler` with `HttpClientHandler`
5. Replace `task.WaitAsync` with extension method

### Phase 5: Test and Fix (4-8 hours)
1. Build and fix remaining errors
2. Test Flash loading
3. Test all features

---

## Estimated Total Time: 15-30 hours

---

## Files to Modify

Based on grep analysis, these files need the most changes:

| File | Issues | Priority |
|------|--------|----------|
| Services/SettingsManager.cs | Async file I/O | High |
| Services/SessionManager.cs | Async file I/O | High |
| Services/GameStateEngine.cs | GetValueOrDefault, TakeLast | High |
| Services/CombatSimulator.cs | Math.Clamp | Medium |
| Services/LlmIntegrationService.cs | SocketsHttpHandler | Medium |
| Services/McpConnectionManager.cs | Task.WaitAsync | Medium |
| All *.cs files | Nullable types, file-scoped namespaces | Low |

---

## Alternative: Automated Migration Script

A PowerShell script could automate many of these changes:

```powershell
# Replace common patterns
Get-ChildItem -Path ".\SvonyBrowser" -Filter "*.cs" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    
    # Replace file-scoped namespace
    $content = $content -replace 'namespace (\w+(?:\.\w+)*);', 'namespace $1`n{'
    
    # Replace nullable types (simple cases)
    $content = $content -replace '\bstring\?\b', 'string'
    $content = $content -replace '\bobject\?\b', 'object'
    
    # Replace Math.Clamp
    $content = $content -replace 'Math\.Clamp\(', 'MathExtensions.Clamp('
    
    # Replace File async methods
    $content = $content -replace 'File\.WriteAllTextAsync\(', 'FileExtensions.WriteAllTextAsync('
    $content = $content -replace 'File\.ReadAllTextAsync\(', 'FileExtensions.ReadAllTextAsync('
    
    Set-Content $_.FullName $content
}
```

---

## Conclusion

This migration is complex but achievable. The key is:
1. Create the extension methods FIRST
2. Update the project file
3. Fix syntax issues systematically
4. Test incrementally

The result will be a Flash-compatible Svony Browser that can load AutoEvony.swf and EvonyClient.swf files.
