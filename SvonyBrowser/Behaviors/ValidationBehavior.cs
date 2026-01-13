using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SvonyBrowser.Services;

namespace SvonyBrowser.Behaviors
{
    /// <summary>
    /// Attached behavior for real-time TextBox validation.
    /// Provides visual feedback and tooltips for validation errors.
    /// </summary>
    public static class ValidationBehavior
    {
        #region Attached Properties

        /// <summary>
        /// Validation type for the TextBox.
        /// </summary>
        public static readonly DependencyProperty ValidationTypeProperty =
            DependencyProperty.RegisterAttached(
                "ValidationType",
                typeof(ValidationType),
                typeof(ValidationBehavior),
                new PropertyMetadata(ValidationType.None, OnValidationTypeChanged));

        public static ValidationType GetValidationType(DependencyObject obj)
        {
            return (ValidationType)obj.GetValue(ValidationTypeProperty);
        }

        public static void SetValidationType(DependencyObject obj, ValidationType value)
        {
            obj.SetValue(ValidationTypeProperty, value);
        }

        /// <summary>
        /// Custom field name for validation messages.
        /// </summary>
        public static readonly DependencyProperty FieldNameProperty =
            DependencyProperty.RegisterAttached(
                "FieldName",
                typeof(string),
                typeof(ValidationBehavior),
                new PropertyMetadata("Field"));

        public static string GetFieldName(DependencyObject obj)
        {
            return (string)obj.GetValue(FieldNameProperty);
        }

        public static void SetFieldName(DependencyObject obj, string value)
        {
            obj.SetValue(FieldNameProperty, value);
        }

        /// <summary>
        /// Whether the field is required.
        /// </summary>
        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.RegisterAttached(
                "IsRequired",
                typeof(bool),
                typeof(ValidationBehavior),
                new PropertyMetadata(false));

        public static bool GetIsRequired(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsRequiredProperty);
        }

        public static void SetIsRequired(DependencyObject obj, bool value)
        {
            obj.SetValue(IsRequiredProperty, value);
        }

        /// <summary>
        /// Minimum value for range validation.
        /// </summary>
        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.RegisterAttached(
                "MinValue",
                typeof(double),
                typeof(ValidationBehavior),
                new PropertyMetadata(double.MinValue));

        public static double GetMinValue(DependencyObject obj)
        {
            return (double)obj.GetValue(MinValueProperty);
        }

        public static void SetMinValue(DependencyObject obj, double value)
        {
            obj.SetValue(MinValueProperty, value);
        }

        /// <summary>
        /// Maximum value for range validation.
        /// </summary>
        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.RegisterAttached(
                "MaxValue",
                typeof(double),
                typeof(ValidationBehavior),
                new PropertyMetadata(double.MaxValue));

        public static double GetMaxValue(DependencyObject obj)
        {
            return (double)obj.GetValue(MaxValueProperty);
        }

        public static void SetMaxValue(DependencyObject obj, double value)
        {
            obj.SetValue(MaxValueProperty, value);
        }

        /// <summary>
        /// Allowed file extensions for file path validation.
        /// </summary>
        public static readonly DependencyProperty AllowedExtensionsProperty =
            DependencyProperty.RegisterAttached(
                "AllowedExtensions",
                typeof(string),
                typeof(ValidationBehavior),
                new PropertyMetadata(null));

        public static string GetAllowedExtensions(DependencyObject obj)
        {
            return (string)obj.GetValue(AllowedExtensionsProperty);
        }

        public static void SetAllowedExtensions(DependencyObject obj, string value)
        {
            obj.SetValue(AllowedExtensionsProperty, value);
        }

        /// <summary>
        /// Whether the current value is valid.
        /// </summary>
        public static readonly DependencyProperty IsValidProperty =
            DependencyProperty.RegisterAttached(
                "IsValid",
                typeof(bool),
                typeof(ValidationBehavior),
                new PropertyMetadata(true));

        public static bool GetIsValid(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsValidProperty);
        }

        public static void SetIsValid(DependencyObject obj, bool value)
        {
            obj.SetValue(IsValidProperty, value);
        }

        /// <summary>
        /// Current validation message.
        /// </summary>
        public static readonly DependencyProperty ValidationMessageProperty =
            DependencyProperty.RegisterAttached(
                "ValidationMessage",
                typeof(string),
                typeof(ValidationBehavior),
                new PropertyMetadata(string.Empty));

        public static string GetValidationMessage(DependencyObject obj)
        {
            return (string)obj.GetValue(ValidationMessageProperty);
        }

        public static void SetValidationMessage(DependencyObject obj, string value)
        {
            obj.SetValue(ValidationMessageProperty, value);
        }

        #endregion

        #region Event Handlers

        private static void OnValidationTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                var newType = (ValidationType)e.NewValue;
                var oldType = (ValidationType)e.OldValue;

                if (oldType != ValidationType.None)
                {
                    textBox.TextChanged -= TextBox_TextChanged;
                    textBox.LostFocus -= TextBox_LostFocus;
                }

                if (newType != ValidationType.None)
                {
                    textBox.TextChanged += TextBox_TextChanged;
                    textBox.LostFocus += TextBox_LostFocus;
                }
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Debounce validation for real-time feedback
                ValidateTextBox(textBox, showVisualFeedback: false);
            }
        }

        private static void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Full validation with visual feedback on focus lost
                ValidateTextBox(textBox, showVisualFeedback: true);
            }
        }

        #endregion

        #region Validation Logic

        private static void ValidateTextBox(TextBox textBox, bool showVisualFeedback)
        {
            var validationType = GetValidationType(textBox);
            var fieldName = GetFieldName(textBox);
            var isRequired = GetIsRequired(textBox);
            var value = textBox.Text;

            ValidationResult result;

            // Check required first
            if (isRequired && string.IsNullOrWhiteSpace(value))
            {
                result = ValidationResult.Error(fieldName, $"{fieldName} is required.");
            }
            else if (string.IsNullOrWhiteSpace(value) && !isRequired)
            {
                result = ValidationResult.Success(fieldName);
            }
            else
            {
                result = ValidateByType(validationType, value, textBox);
            }

            // Update attached properties
            SetIsValid(textBox, result.IsValid);
            SetValidationMessage(textBox, result.Message);

            // Apply visual feedback
            if (showVisualFeedback)
            {
                ApplyVisualFeedback(textBox, result);
            }
        }

        private static ValidationResult ValidateByType(ValidationType type, string value, TextBox textBox)
        {
            var fieldName = GetFieldName(textBox);
            var validator = ValidationService.Instance;

            switch (type)
            {
                case ValidationType.Url:
                    return validator.ValidateUrl(value, fieldName);

                case ValidationType.Port:
                    return validator.ValidatePort(value, fieldName);

                case ValidationType.IpAddress:
                    return validator.ValidateIpAddress(value, fieldName);

                case ValidationType.Hostname:
                    return validator.ValidateHostname(value, fieldName);

                case ValidationType.Integer:
                    var minInt = (int)GetMinValue(textBox);
                    var maxInt = (int)GetMaxValue(textBox);
                    return validator.ValidateIntRange(value, minInt, maxInt, fieldName);

                case ValidationType.Decimal:
                    var minDec = GetMinValue(textBox);
                    var maxDec = GetMaxValue(textBox);
                    return validator.ValidateDecimalRange(value, minDec, maxDec, fieldName);

                case ValidationType.FilePath:
                    var extensions = GetAllowedExtensions(textBox)?.Split(',');
                    return validator.ValidateFilePath(value, fieldName, extensions);

                case ValidationType.DirectoryPath:
                    return validator.ValidateDirectoryPath(value, fieldName);

                case ValidationType.WebhookUrl:
                    return validator.ValidateWebhookUrl(value, fieldName);

                case ValidationType.ApiKey:
                    return validator.ValidateApiKey(value, fieldName);

                case ValidationType.ServerAddress:
                    return validator.ValidateServerAddress(value, fieldName);

                default:
                    return ValidationResult.Success(fieldName);
            }
        }

        private static void ApplyVisualFeedback(TextBox textBox, ValidationResult result)
        {
            // Store original values if not already stored
            if (textBox.Tag == null)
            {
                textBox.Tag = new OriginalStyle
                {
                    BorderBrush = textBox.BorderBrush,
                    ToolTip = textBox.ToolTip
                };
            }

            var original = textBox.Tag as OriginalStyle;

            if (result.IsValid && result.Severity == ValidationSeverity.None)
            {
                // Valid - restore original style
                textBox.BorderBrush = original?.BorderBrush ?? SystemColors.ControlDarkBrush;
                textBox.ToolTip = original?.ToolTip;
            }
            else if (result.Severity == ValidationSeverity.Warning)
            {
                // Warning - yellow border
                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                textBox.ToolTip = result.Message;
            }
            else
            {
                // Error - red border
                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                textBox.ToolTip = result.Message;
            }
        }

        #endregion

        #region Helper Classes

        private class OriginalStyle
        {
            public Brush BorderBrush { get; set; }
            public object ToolTip { get; set; }
        }

        #endregion
    }

    /// <summary>
    /// Types of validation available.
    /// </summary>
    public enum ValidationType
    {
        None,
        Url,
        Port,
        IpAddress,
        Hostname,
        Integer,
        Decimal,
        FilePath,
        DirectoryPath,
        WebhookUrl,
        ApiKey,
        ServerAddress
    }
}
