using System.Runtime.InteropServices;

namespace Inventory.API.Helper
{
    public class PDFHelper
    {
        public static class CustomAssemblyLoadContext
        {
            public static void LoadNativeLibrary()
            {
                // Architecture folder: x64 ya x86
                var architectureFolder = (IntPtr.Size == 8) ? "x64" : "x86";
                // OS ke hisaab se extension (.dll Windows ke liye aur .so Linux ke liye)
                var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll" : ".so";
                // Path calculate karein
                var libName = "libwkhtmltox" + extension;

                // 1. PRIORITY: Check system-wide Linux path first (For Docker)
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var systemPaths = new[] { "/usr/lib/libwkhtmltox.so", "/usr/lib/x86_64-linux-gnu/libwkhtmltox.so", "/usr/local/lib/libwkhtmltox.so" };
                    foreach (var path in systemPaths)
                    {
                        if (File.Exists(path))
                        {
                            NativeLibrary.Load(path);
                            return;
                        }
                    }
                }

                // 2. Fallback: Check local vclibs folder
                var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vclibs", architectureFolder, libName);
                if (File.Exists(dllPath))
                {
                    NativeLibrary.Load(dllPath);
                    return;
                }

                // 3. Last Resort: Try loading without path
                try
                {
                    NativeLibrary.Load(libName);
                    return;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Bhai, 'libwkhtmltox{extension}' file nahi mili.\n" +
                        $"Code checked: /usr/lib/, /app/vclibs/x64/, and system paths.\n" +
                        $"System Error: {ex.Message}");
                }
            }
        }
    }

}

