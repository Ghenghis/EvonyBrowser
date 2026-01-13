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
    /// These methods allow code written for .NET 6 to compile and run on .NET Framework.
    /// </summary>
    public static class NetFrameworkExtensions
    {
        #region Dictionary Extensions
        
        /// <summary>
        /// Gets the value associated with the specified key, or default if not found.
        /// Equivalent to .NET Core's Dictionary.GetValueOrDefault()
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary, 
            TKey key, 
            TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
        
        /// <summary>
        /// Gets the value associated with the specified key, or default if not found.
        /// Equivalent to .NET Core's IDictionary.GetValueOrDefault()
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, 
            TKey key, 
            TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
        
        /// <summary>
        /// Gets the value associated with the specified key, or default if not found.
        /// For ConcurrentDictionary compatibility.
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(
            this System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue> dictionary, 
            TKey key, 
            TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
        
        #endregion
        
        #region Enumerable Extensions
        
        /// <summary>
        /// Returns the last N elements from the sequence.
        /// Equivalent to .NET Core's Enumerable.TakeLast()
        /// </summary>
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (count <= 0) return Enumerable.Empty<T>();
            
            var list = source as IList<T> ?? source.ToList();
            var skipCount = Math.Max(0, list.Count - count);
            return list.Skip(skipCount);
        }
        
        /// <summary>
        /// Returns all elements except the last N.
        /// Equivalent to .NET Core's Enumerable.SkipLast()
        /// </summary>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (count <= 0) return source;
            
            var list = source as IList<T> ?? source.ToList();
            var takeCount = Math.Max(0, list.Count - count);
            return list.Take(takeCount);
        }
        
        /// <summary>
        /// Creates a HashSet from an IEnumerable.
        /// Equivalent to .NET Core's Enumerable.ToHashSet()
        /// </summary>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
        
        /// <summary>
        /// Creates a HashSet from an IEnumerable with a custom comparer.
        /// Equivalent to .NET Core's Enumerable.ToHashSet()
        /// </summary>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(source, comparer);
        }
        
        /// <summary>
        /// Appends an element to the end of a sequence.
        /// Equivalent to .NET Core's Enumerable.Append()
        /// </summary>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T element)
        {
            if (source == null) throw new ArgumentNullException("source");
            foreach (var item in source)
            {
                yield return item;
            }
            yield return element;
        }
        
        /// <summary>
        /// Prepends an element to the beginning of a sequence.
        /// Equivalent to .NET Core's Enumerable.Prepend()
        /// </summary>
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, T element)
        {
            if (source == null) throw new ArgumentNullException("source");
            yield return element;
            foreach (var item in source)
            {
                yield return item;
            }
        }
        
        #endregion
        
        #region File Extensions
        
        /// <summary>
        /// Asynchronously reads all text from a file.
        /// Equivalent to .NET Core's File.ReadAllTextAsync()
        /// </summary>
        public static Task<string> ReadAllTextAsync(string path)
        {
            return Task.Run(() => File.ReadAllText(path));
        }
        
        /// <summary>
        /// Asynchronously reads all text from a file with encoding.
        /// </summary>
        public static Task<string> ReadAllTextAsync(string path, Encoding encoding)
        {
            return Task.Run(() => File.ReadAllText(path, encoding));
        }
        
        /// <summary>
        /// Asynchronously writes text to a file.
        /// Equivalent to .NET Core's File.WriteAllTextAsync()
        /// </summary>
        public static Task WriteAllTextAsync(string path, string contents)
        {
            return Task.Run(() => File.WriteAllText(path, contents));
        }
        
        /// <summary>
        /// Asynchronously writes text to a file with encoding.
        /// </summary>
        public static Task WriteAllTextAsync(string path, string contents, Encoding encoding)
        {
            return Task.Run(() => File.WriteAllText(path, contents, encoding));
        }
        
        /// <summary>
        /// Asynchronously reads all bytes from a file.
        /// Equivalent to .NET Core's File.ReadAllBytesAsync()
        /// </summary>
        public static Task<byte[]> ReadAllBytesAsync(string path)
        {
            return Task.Run(() => File.ReadAllBytes(path));
        }
        
        /// <summary>
        /// Asynchronously writes bytes to a file.
        /// Equivalent to .NET Core's File.WriteAllBytesAsync()
        /// </summary>
        public static Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            return Task.Run(() => File.WriteAllBytes(path, bytes));
        }
        
        /// <summary>
        /// Asynchronously reads all lines from a file.
        /// Equivalent to .NET Core's File.ReadAllLinesAsync()
        /// </summary>
        public static Task<string[]> ReadAllLinesAsync(string path)
        {
            return Task.Run(() => File.ReadAllLines(path));
        }
        
        /// <summary>
        /// Asynchronously writes lines to a file.
        /// Equivalent to .NET Core's File.WriteAllLinesAsync()
        /// </summary>
        public static Task WriteAllLinesAsync(string path, IEnumerable<string> contents)
        {
            return Task.Run(() => File.WriteAllLines(path, contents.ToArray()));
        }
        
        /// <summary>
        /// Asynchronously appends text to a file.
        /// Equivalent to .NET Core's File.AppendAllTextAsync()
        /// </summary>
        public static Task AppendAllTextAsync(string path, string contents)
        {
            return Task.Run(() => File.AppendAllText(path, contents));
        }
        
        #endregion
        
        #region Math Extensions
        
        /// <summary>
        /// Clamps a value between min and max.
        /// Equivalent to .NET Core's Math.Clamp()
        /// </summary>
        public static int Clamp(int value, int min, int max)
        {
            if (min > max)
                throw new ArgumentException("min must be less than or equal to max");
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        /// <summary>
        /// Clamps a value between min and max.
        /// Equivalent to .NET Core's Math.Clamp()
        /// </summary>
        public static double Clamp(double value, double min, double max)
        {
            if (min > max)
                throw new ArgumentException("min must be less than or equal to max");
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        /// <summary>
        /// Clamps a value between min and max.
        /// Equivalent to .NET Core's Math.Clamp()
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            if (min > max)
                throw new ArgumentException("min must be less than or equal to max");
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        /// <summary>
        /// Clamps a value between min and max.
        /// Equivalent to .NET Core's Math.Clamp()
        /// </summary>
        public static long Clamp(long value, long min, long max)
        {
            if (min > max)
                throw new ArgumentException("min must be less than or equal to max");
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        /// <summary>
        /// Clamps a value between min and max.
        /// </summary>
        public static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (min > max)
                throw new ArgumentException("min must be less than or equal to max");
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        #endregion
        
        #region Task Extensions
        
        /// <summary>
        /// Waits for a task to complete with a timeout.
        /// Equivalent to .NET 6's Task.WaitAsync()
        /// </summary>
        public static async Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                var delayTask = Task.Delay(timeout, cts.Token);
                var completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);
                
                if (completedTask == delayTask)
                {
                    throw new TimeoutException("The operation timed out.");
                }
                
                cts.Cancel();
                return await task.ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// Waits for a task to complete with a timeout.
        /// Equivalent to .NET 6's Task.WaitAsync()
        /// </summary>
        public static async Task WaitAsync(this Task task, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                var delayTask = Task.Delay(timeout, cts.Token);
                var completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);
                
                if (completedTask == delayTask)
                {
                    throw new TimeoutException("The operation timed out.");
                }
                
                cts.Cancel();
                await task.ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// Waits for a task to complete with cancellation support.
        /// Equivalent to .NET 6's Task.WaitAsync()
        /// </summary>
        public static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            return await task.ConfigureAwait(false);
        }
        
        #endregion
        
        #region Hash Extensions
        
        /// <summary>
        /// Computes SHA256 hash of byte array.
        /// Equivalent to .NET 5+'s SHA256.HashData()
        /// </summary>
        public static byte[] ComputeSHA256Hash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }
        
        /// <summary>
        /// Computes SHA256 hash and returns as hex string.
        /// </summary>
        public static string ComputeSHA256HashString(byte[] data)
        {
            var hash = ComputeSHA256Hash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        
        /// <summary>
        /// Computes SHA256 hash of string and returns as hex string.
        /// </summary>
        public static string ComputeSHA256HashString(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return ComputeSHA256HashString(bytes);
        }
        
        /// <summary>
        /// Computes MD5 hash of byte array.
        /// </summary>
        public static byte[] ComputeMD5Hash(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }
        
        #endregion
        
        #region KeyValuePair Extensions
        
        /// <summary>
        /// Deconstructs a KeyValuePair for use in foreach loops.
        /// Allows: foreach (var (key, value) in dictionary)
        /// </summary>
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
        
        /// <summary>
        /// Determines whether a string contains a substring using specified comparison.
        /// Equivalent to .NET Core's String.Contains(string, StringComparison)
        /// </summary>
        public static bool Contains(this string source, string value, StringComparison comparison)
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.IndexOf(value, comparison) >= 0;
        }
        
        /// <summary>
        /// Splits a string using a single character separator with options.
        /// </summary>
        public static string[] Split(this string source, char separator, StringSplitOptions options)
        {
            return source.Split(new[] { separator }, options);
        }
        
        /// <summary>
        /// Splits a string using a string separator with options.
        /// </summary>
        public static string[] Split(this string source, string separator, StringSplitOptions options)
        {
            return source.Split(new[] { separator }, options);
        }
        
        /// <summary>
        /// Returns a new string with all leading and trailing white-space characters removed.
        /// Handles null strings safely.
        /// </summary>
        public static string TrimSafe(this string source)
        {
            return source == null ? string.Empty : source.Trim();
        }
        
        #endregion
        
        #region Tuple Extensions
        
        /// <summary>
        /// Deconstructs a Tuple for use in assignments.
        /// </summary>
        public static void Deconstruct<T1, T2>(this Tuple<T1, T2> tuple, out T1 item1, out T2 item2)
        {
            item1 = tuple.Item1;
            item2 = tuple.Item2;
        }
        
        /// <summary>
        /// Deconstructs a Tuple for use in assignments.
        /// </summary>
        public static void Deconstruct<T1, T2, T3>(this Tuple<T1, T2, T3> tuple, out T1 item1, out T2 item2, out T3 item3)
        {
            item1 = tuple.Item1;
            item2 = tuple.Item2;
            item3 = tuple.Item3;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Static class providing Math.Clamp equivalent functionality.
    /// Use: MathEx.Clamp(value, min, max)
    /// </summary>
    public static class MathEx
    {
        public static int Clamp(int value, int min, int max) => NetFrameworkExtensions.Clamp(value, min, max);
        public static double Clamp(double value, double min, double max) => NetFrameworkExtensions.Clamp(value, min, max);
        public static float Clamp(float value, float min, float max) => NetFrameworkExtensions.Clamp(value, min, max);
        public static long Clamp(long value, long min, long max) => NetFrameworkExtensions.Clamp(value, min, max);
        public static decimal Clamp(decimal value, decimal min, decimal max) => NetFrameworkExtensions.Clamp(value, min, max);
    }
    
    /// <summary>
    /// Static class providing File async methods.
    /// Use: FileEx.ReadAllTextAsync(path) instead of File.ReadAllTextAsync(path)
    /// </summary>
    public static class FileEx
    {
        public static Task<string> ReadAllTextAsync(string path) => NetFrameworkExtensions.ReadAllTextAsync(path);
        public static Task<string> ReadAllTextAsync(string path, Encoding encoding) => NetFrameworkExtensions.ReadAllTextAsync(path, encoding);
        public static Task WriteAllTextAsync(string path, string contents) => NetFrameworkExtensions.WriteAllTextAsync(path, contents);
        public static Task WriteAllTextAsync(string path, string contents, Encoding encoding) => NetFrameworkExtensions.WriteAllTextAsync(path, contents, encoding);
        public static Task<byte[]> ReadAllBytesAsync(string path) => NetFrameworkExtensions.ReadAllBytesAsync(path);
        public static Task WriteAllBytesAsync(string path, byte[] bytes) => NetFrameworkExtensions.WriteAllBytesAsync(path, bytes);
        public static Task<string[]> ReadAllLinesAsync(string path) => NetFrameworkExtensions.ReadAllLinesAsync(path);
        public static Task WriteAllLinesAsync(string path, IEnumerable<string> contents) => NetFrameworkExtensions.WriteAllLinesAsync(path, contents);
        public static Task AppendAllTextAsync(string path, string contents) => NetFrameworkExtensions.AppendAllTextAsync(path, contents);
        
        // CancellationToken overloads for API compatibility
        public static Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ReadAllTextAsync(path);
        }
        
        public static Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return WriteAllTextAsync(path, contents);
        }
    }
    
    /// <summary>
    /// Static class providing SHA256.HashData equivalent functionality.
    /// Use: HashEx.SHA256(data) instead of SHA256.HashData(data)
    /// </summary>
    public static class HashEx
    {
        public static byte[] SHA256(byte[] data) => NetFrameworkExtensions.ComputeSHA256Hash(data);
        public static string SHA256String(byte[] data) => NetFrameworkExtensions.ComputeSHA256HashString(data);
        public static string SHA256String(string input) => NetFrameworkExtensions.ComputeSHA256HashString(input);
        public static byte[] MD5(byte[] data) => NetFrameworkExtensions.ComputeMD5Hash(data);
    }
}
