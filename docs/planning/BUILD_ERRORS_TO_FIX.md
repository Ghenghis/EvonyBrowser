# Build Errors to Fix

## Duplicate Class Definitions (CS0101)
- [ ] CircuitBreaker defined in both FailsafeManager.cs AND CircuitBreaker.cs
- [ ] CircuitBreakerOpenException defined in both FailsafeManager.cs AND CircuitBreaker.cs  
- [ ] MemoryStatistics defined in both FailsafeManager.cs AND MemoryGuard.cs

## Missing Type References (CS0246)
- [ ] FrameLoadEndEventArgs not found in MainWindow.xaml.cs line 290
- [ ] FrameLoadEndEventArgs not found in MainWindow.xaml.cs line 303
- [ ] LoadErrorEventArgs not found in MainWindow.xaml.cs line 315

## Accessibility Issues (CS0053)
- [ ] CircuitBreaker.CircuitState is less accessible than CircuitBreaker.State

## Duplicate Constructor (CS0111)
- [ ] CircuitBreakerOpenException has duplicate constructor

## Solution
1. Delete CircuitBreaker.cs, MemoryGuard.cs, DebugService.cs from GitHub (already done locally)
2. Fix FailsafeManager.cs - make CircuitState public
3. Fix MainWindow.xaml.cs - use correct CefSharp event args types
