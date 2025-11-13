using System;
using System.Reflection;

namespace TrayControl
{
    public sealed class VersionAttribute : Attribute
    {
        // Existing code (if any)...
        
        public static string GetAppTitleWithVersion()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var titleAttr = asm.GetCustomAttribute<AssemblyTitleAttribute>();
            var title = titleAttr?.Title ?? asm.GetName().Name;
            var version = asm.GetName().Version?.ToString() ?? "1.0.0.0";
            return $"{title} v{version}";
        }
    }
}