using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace SvonyBrowser.Services
{
    /// <summary>
    /// Centralized validation service for Svony Browser.
    /// Provides input validation with real-time feedback.
    /// </summary>
    public sealed class ValidationService : INotifyPropertyChanged
    {
        #region Singleton

        private static readonly Lazy<ValidationService> _lazyInstance =
            new Lazy<ValidationService>(() => new ValidationService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ValidationService Instance => _lazyInstance.Value;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<ValidationEventArgs> ValidationFailed;
        public event EventHandler<ValidationEventArgs> ValidationPassed;

        #endregion

        #region Validation Rules

        /// <summary>
        /// Validates a URL string.
        /// </summary>
        public ValidationResult ValidateUrl(string value, string fieldName = "URL")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Error(fieldName, "URL is required.");
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return ValidationResult.Error(fieldName, "Please enter a valid URL (e.g., http://example.com).");
            }

            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return ValidationResult.Error(fieldName, "URL must start with http:// or https://.");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates a port number.
        /// </summary>
        public ValidationResult ValidatePort(string value, string fieldName = "Port")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Error(fieldName, "Port is required.");
            }

            if (!int.TryParse(value, out var port))
            {
                return ValidationResult.Error(fieldName, "Port must be a number.");
            }

            if (port < 1 || port > 65535)
            {
                return ValidationResult.Error(fieldName, "Port must be between 1 and 65535.");
            }

            // Warn about privileged ports
            if (port < 1024)
            {
                return ValidationResult.Warning(fieldName, "Ports below 1024 may require administrator privileges.");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates an IP address.
        /// </summary>
        public ValidationResult ValidateIpAddress(string value, string fieldName = "IP Address")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Error(fieldName, "IP address is required.");
            }

            if (value == "localhost")
            {
                return ValidationResult.Success(fieldName);
            }

            if (!IPAddress.TryParse(value, out _))
            {
                return ValidationResult.Error(fieldName, "Please enter a valid IP address (e.g., 192.168.1.1).");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates a hostname.
        /// </summary>
        public ValidationResult ValidateHostname(string value, string fieldName = "Hostname")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Error(fieldName, "Hostname is required.");
            }

            // Check for valid hostname pattern
            var hostnamePattern = @"^([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])*\.?$|^localhost$";
            if (!Regex.IsMatch(value, hostnamePattern))
            {
                return ValidationResult.Error(fieldName, "Please enter a valid hostname (e.g., example.com).");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates an integer within a range.
        /// </summary>
        public ValidationResult ValidateIntRange(string value, int min, int max, string fieldName = "Value")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Error(fieldName, $"{fieldName} is required.");
            }

            if (!int.TryParse(value, out var intValue))
            {
                return ValidationResult.Error(fieldName, $"{fieldName} must be a whole number.");
            }

            if (intValue < min || intValue > max)
            {
                return ValidationResult.Error(fieldName, $"{fieldName} must be between {min} and {max}.");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates a decimal within a range.
        /// </summary>
        public ValidationResult ValidateDecimalRange(string value, double min, double max, string fieldName = "Value")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Error(fieldName, $"{fieldName} is required.");
            }

            if (!double.TryParse(value, out var doubleValue))
            {
                return ValidationResult.Error(fieldName, $"{fieldName} must be a number.");
            }

            if (doubleValue < min || doubleValue > max)
            {
                return ValidationResult.Error(fieldName, $"{fieldName} must be between {min} and {max}.");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates a file path.
        /// </summary>
        public ValidationResult ValidateFilePath(string value, string fieldName = "File Path", string[] allowedExtensions = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Error(fieldName, "File path is required.");
            }

            // Check for invalid characters
            var invalidChars = System.IO.Path.GetInvalidPathChars();
            if (value.Any(c => invalidChars.Contains(c)))
            {
                return ValidationResult.Error(fieldName, "File path contains invalid characters.");
            }

            // Check extension if specified
            if (allowedExtensions != null && allowedExtensions.Length > 0)
            {
                var ext = System.IO.Path.GetExtension(value)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    return ValidationResult.Error(fieldName, $"File must have one of these extensions: {string.Join(", ", allowedExtensions)}");
                }
            }

            // Check if file exists
            if (!System.IO.File.Exists(value))
            {
                return ValidationResult.Warning(fieldName, "File does not exist at the specified path.");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates a directory path.
        /// </summary>
        public ValidationResult ValidateDirectoryPath(string value, string fieldName = "Directory Path")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Error(fieldName, "Directory path is required.");
            }

            // Check for invalid characters
            var invalidChars = System.IO.Path.GetInvalidPathChars();
            if (value.Any(c => invalidChars.Contains(c)))
            {
                return ValidationResult.Error(fieldName, "Directory path contains invalid characters.");
            }

            // Check if directory exists
            if (!System.IO.Directory.Exists(value))
            {
                return ValidationResult.Warning(fieldName, "Directory does not exist at the specified path.");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates a webhook URL.
        /// </summary>
        public ValidationResult ValidateWebhookUrl(string value, string fieldName = "Webhook URL")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Success(fieldName); // Optional field
            }

            var urlResult = ValidateUrl(value, fieldName);
            if (!urlResult.IsValid)
            {
                return urlResult;
            }

            // Check for common webhook patterns
            if (value.Contains("discord.com/api/webhooks") ||
                value.Contains("hooks.slack.com") ||
                value.Contains("api.telegram.org"))
            {
                return ValidationResult.Success(fieldName);
            }

            return ValidationResult.Warning(fieldName, "URL doesn't match common webhook patterns. Please verify it's correct.");
        }

        /// <summary>
        /// Validates an API key.
        /// </summary>
        public ValidationResult ValidateApiKey(string value, string fieldName = "API Key", int minLength = 10)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Success(fieldName); // Optional field
            }

            if (value.Length < minLength)
            {
                return ValidationResult.Error(fieldName, $"API key seems too short (minimum {minLength} characters).");
            }

            // Check for placeholder values
            var placeholders = new[] { "your-api-key", "api-key-here", "xxx", "test", "demo" };
            if (placeholders.Any(p => value.ToLowerInvariant().Contains(p)))
            {
                return ValidationResult.Warning(fieldName, "This looks like a placeholder. Please enter your actual API key.");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates a server address (hostname:port or IP:port).
        /// </summary>
        public ValidationResult ValidateServerAddress(string value, string fieldName = "Server Address")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Error(fieldName, "Server address is required.");
            }

            var parts = value.Split(':');
            if (parts.Length != 2)
            {
                return ValidationResult.Error(fieldName, "Server address must be in format hostname:port or IP:port.");
            }

            var hostResult = ValidateHostname(parts[0], "Host");
            if (!hostResult.IsValid)
            {
                // Try as IP
                hostResult = ValidateIpAddress(parts[0], "Host");
                if (!hostResult.IsValid)
                {
                    return ValidationResult.Error(fieldName, "Invalid hostname or IP address.");
                }
            }

            var portResult = ValidatePort(parts[1], "Port");
            if (!portResult.IsValid)
            {
                return portResult;
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates required text.
        /// </summary>
        public ValidationResult ValidateRequired(string value, string fieldName = "Field")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Error(fieldName, $"{fieldName} is required.");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates text length.
        /// </summary>
        public ValidationResult ValidateLength(string value, int minLength, int maxLength, string fieldName = "Field")
        {
            if (string.IsNullOrEmpty(value))
            {
                if (minLength > 0)
                {
                    return ValidationResult.Error(fieldName, $"{fieldName} is required.");
                }
                return ValidationResult.Success(fieldName);
            }

            if (value.Length < minLength)
            {
                return ValidationResult.Error(fieldName, $"{fieldName} must be at least {minLength} characters.");
            }

            if (value.Length > maxLength)
            {
                return ValidationResult.Error(fieldName, $"{fieldName} must be no more than {maxLength} characters.");
            }

            return ValidationResult.Success(fieldName);
        }

        /// <summary>
        /// Validates with a custom regex pattern.
        /// </summary>
        public ValidationResult ValidatePattern(string value, string pattern, string fieldName = "Field", string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Success(fieldName); // Optional by default
            }

            if (!Regex.IsMatch(value, pattern))
            {
                return ValidationResult.Error(fieldName, errorMessage ?? $"{fieldName} format is invalid.");
            }

            return ValidationResult.Success(fieldName);
        }

        #endregion

        #region Batch Validation

        /// <summary>
        /// Validates multiple fields and returns all results.
        /// </summary>
        public IReadOnlyList<ValidationResult> ValidateAll(params (Func<ValidationResult> validator, string fieldName)[] validations)
        {
            var results = new List<ValidationResult>();

            foreach (var (validator, fieldName) in validations)
            {
                try
                {
                    var result = validator();
                    results.Add(result);

                    if (result.IsValid)
                    {
                        ValidationPassed?.Invoke(this, new ValidationEventArgs(result));
                    }
                    else
                    {
                        ValidationFailed?.Invoke(this, new ValidationEventArgs(result));
                    }
                }
                catch (Exception ex)
                {
                    results.Add(ValidationResult.Error(fieldName, $"Validation error: {ex.Message}"));
                }
            }

            return results;
        }

        /// <summary>
        /// Checks if all validations passed.
        /// </summary>
        public bool IsAllValid(IEnumerable<ValidationResult> results)
        {
            return results.All(r => r.IsValid);
        }

        /// <summary>
        /// Gets all error messages from validation results.
        /// </summary>
        public IReadOnlyList<string> GetErrorMessages(IEnumerable<ValidationResult> results)
        {
            return results
                .Where(r => !r.IsValid && r.Severity == ValidationSeverity.Error)
                .Select(r => r.Message)
                .ToList();
        }

        /// <summary>
        /// Gets all warning messages from validation results.
        /// </summary>
        public IReadOnlyList<string> GetWarningMessages(IEnumerable<ValidationResult> results)
        {
            return results
                .Where(r => r.Severity == ValidationSeverity.Warning)
                .Select(r => r.Message)
                .ToList();
        }

        #endregion

        #region Private Methods

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Result of a validation check.
    /// </summary>
    public class ValidationResult
    {
        public string FieldName { get; private set; }
        public bool IsValid { get; private set; }
        public string Message { get; private set; }
        public ValidationSeverity Severity { get; private set; }

        private ValidationResult() { }

        public static ValidationResult Success(string fieldName)
        {
            return new ValidationResult
            {
                FieldName = fieldName,
                IsValid = true,
                Message = "",
                Severity = ValidationSeverity.None
            };
        }

        public static ValidationResult Error(string fieldName, string message)
        {
            return new ValidationResult
            {
                FieldName = fieldName,
                IsValid = false,
                Message = message,
                Severity = ValidationSeverity.Error
            };
        }

        public static ValidationResult Warning(string fieldName, string message)
        {
            return new ValidationResult
            {
                FieldName = fieldName,
                IsValid = true, // Warnings don't block submission
                Message = message,
                Severity = ValidationSeverity.Warning
            };
        }
    }

    /// <summary>
    /// Severity level of validation result.
    /// </summary>
    public enum ValidationSeverity
    {
        None,
        Warning,
        Error
    }

    /// <summary>
    /// Event args for validation events.
    /// </summary>
    public class ValidationEventArgs : EventArgs
    {
        public ValidationResult Result { get; }

        public ValidationEventArgs(ValidationResult result)
        {
            Result = result;
        }
    }

    #endregion
}
