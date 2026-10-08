using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("RightLayout")]
[assembly: AssemblyProduct("RightLayout")]
[assembly: AssemblyDescription("Switches Windows to the input method of whichever keyboard you type on")]
[assembly: AssemblyCopyright("Copyright © 2026 Koushik. MIT License.")]
[assembly: AssemblyVersion("4.7.0.0")]
[assembly: AssemblyFileVersion("4.7.0.0")]
[assembly: ComVisible(false)]

// Load the Windows DLLs this app calls (cfgmgr32, dwmapi, uxtheme, ...) only from System32, never
// from the exe's folder, where a planted DLL with the same name would otherwise be picked first
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
