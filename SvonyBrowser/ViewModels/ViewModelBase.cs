using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SvonyBrowser.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels implementing INotifyPropertyChanged.
    /// Provides thread-safe property change notification and helper methods.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed. Auto-populated by CallerMemberName.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets a property value and raises PropertyChanged if the value changed.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">New value to set.</param>
        /// <param name="propertyName">Name of the property. Auto-populated by CallerMemberName.</param>
        /// <returns>True if the value changed, false otherwise.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Sets a property value, raises PropertyChanged, and executes a callback if the value changed.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">New value to set.</param>
        /// <param name="onChanged">Callback to execute after the property changes.</param>
        /// <param name="propertyName">Name of the property. Auto-populated by CallerMemberName.</param>
        /// <returns>True if the value changed, false otherwise.</returns>
        protected bool SetProperty<T>(ref T field, T value, Action onChanged, [CallerMemberName] string propertyName = null)
        {
            if (!SetProperty(ref field, value, propertyName))
                return false;

            onChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Raises PropertyChanged for multiple properties at once.
        /// </summary>
        /// <param name="propertyNames">Names of properties that changed.</param>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                OnPropertyChanged(name);
            }
        }

        /// <summary>
        /// Disposes the ViewModel and clears event handlers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override this method to dispose managed resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Clear event handlers to prevent memory leaks
                PropertyChanged = null;
            }

            _disposed = true;
        }
    }
}
