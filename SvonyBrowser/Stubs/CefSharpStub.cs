#if !WINDOWS
// Stub classes for CefSharp to allow compilation on non-Windows platforms
// These are only used during build verification and are not functional

namespace CefSharp.Wpf
{
    public class ChromiumWebBrowser : System.Windows.Controls.Control
    {
        public string Address { get; set; } = "";
        public event EventHandler<FrameLoadEndEventArgs> FrameLoadEnd;
        public event EventHandler<LoadErrorEventArgs> LoadError;
        public event EventHandler<AddressChangedEventArgs> AddressChanged;
        public event EventHandler<TitleChangedEventArgs> TitleChanged;
        public event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged;
        
        public IBrowser? GetBrowser() => null;
        public void Load(string url) { }
        public bool CanGoBack => false;
        public bool CanGoForward => false;
        public void Back() { }
        public void Forward() { }
        public void Reload() { }
        public void Stop() { }
        public IRequestContext? RequestContext { get; set; }
    }
    
    public interface IBrowser
    {
        IFrame? MainFrame { get; }
        void CloseBrowser(bool forceClose);
    }
    
    public interface IFrame
    {
        bool IsMain { get; }
        void ExecuteJavaScriptAsync(string script);
    }
    
    public interface IRequestContext
    {
        void ClearCertificateExceptions(ICompletionCallback? callback);
    }
    
    public interface ICompletionCallback { }
}

namespace CefSharp
{
    public class FrameLoadEndEventArgs : EventArgs
    {
        public IFrame? Frame { get; set; }
        public string Url { get; set; } = "";
    }
    
    public interface IFrame
    {
        bool IsMain { get; }
        void ExecuteJavaScriptAsync(string script);
    }
    
    public class LoadErrorEventArgs : EventArgs
    {
        public CefErrorCode ErrorCode { get; set; }
        public string ErrorText { get; set; } = "";
        public string FailedUrl { get; set; } = "";
    }
    
    public class AddressChangedEventArgs : EventArgs
    {
        public string Address { get; set; } = "";
    }
    
    public class TitleChangedEventArgs : EventArgs
    {
        public string Title { get; set; } = "";
    }
    
    public class LoadingStateChangedEventArgs : EventArgs
    {
        public bool IsLoading { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
    }
    
    public enum CefErrorCode
    {
        None = 0,
        Aborted = -3,
        Failed = -2
    }
    
    public static class Cef
    {
        public static bool IsInitialized => false;
        public static void Initialize(CefSettings settings) { }
        public static void Shutdown() { }
        public static void EnableHighDPISupport() { }
    }
    
    public class CefSettings
    {
        public string CachePath { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public string LogFile { get; set; } = "";
        public LogSeverity LogSeverity { get; set; }
        public bool PersistSessionCookies { get; set; }
        public bool PersistUserPreferences { get; set; }
        public bool IgnoreCertificateErrors { get; set; }
        public CefCommandLineArgs CefCommandLineArgs { get; } = new CefCommandLineArgs();
    }
    
    public class CefCommandLineArgs : Dictionary<string, string>
    {
        public void Add(string key) => base.Add(key, "");
    }
    
    public enum LogSeverity
    {
        Disable = 0,
        Default = 1,
        Verbose = 2,
        Info = 3,
        Warning = 4,
        Error = 5,
        Fatal = 6
    }
}
#endif
