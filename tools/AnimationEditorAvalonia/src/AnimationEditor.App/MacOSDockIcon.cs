using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AnimationEditor.App;

/// <summary>
/// Sets the macOS Dock icon directly via Objective-C runtime P/Invoke.
/// Avalonia's macOS backend does not call NSApplication.SharedApplication
/// .ApplicationIconImage when WindowDecorations="None" is used, so we do it
/// ourselves. Call <see cref="Set"/> inside AppBuilder.AfterSetup (after the
/// platform backend has loaded AppKit) to show the correct icon from first launch.
/// </summary>
internal static class MacOSDockIcon
{
    // Three P/Invoke signatures against the same objc_msgSend entry point:
    // 0-arg → IntPtr, 1-arg → IntPtr, 1-arg → void.
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSend(IntPtr self, IntPtr op);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSend1(IntPtr self, IntPtr op, IntPtr a1);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void MsgSendVoid1(IntPtr self, IntPtr op, IntPtr a1);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string name);

    /// <summary>
    /// Loads <paramref name="icnsPath"/> as an NSImage and assigns it to
    /// NSApplication.sharedApplication.applicationIconImage. Safe to call on
    /// non-macOS platforms (returns immediately) and when the file is absent.
    /// </summary>
    public static void Set(string icnsPath)
    {
        if (!OperatingSystem.IsMacOS() || !File.Exists(icnsPath))
            return;

        // AppKit must be loaded before we can look up NSApplication / NSImage.
        NativeLibrary.TryLoad(
            "/System/Library/Frameworks/AppKit.framework/AppKit", out _);

        var pathStr = ToNSString(icnsPath);
        if (pathStr == IntPtr.Zero)
            return;

        var image = MsgSend1(
            MsgSend(objc_getClass("NSImage"), sel_registerName("alloc")),
            sel_registerName("initWithContentsOfFile:"),
            pathStr);

        if (image == IntPtr.Zero)
            return;

        var sharedApp = MsgSend(
            objc_getClass("NSApplication"),
            sel_registerName("sharedApplication"));

        MsgSendVoid1(sharedApp, sel_registerName("setApplicationIconImage:"), image);
    }

    private static IntPtr ToNSString(string value)
    {
        var ptr = Marshal.StringToCoTaskMemUTF8(value);
        try
        {
            return MsgSend1(
                objc_getClass("NSString"),
                sel_registerName("stringWithUTF8String:"),
                ptr);
        }
        finally
        {
            Marshal.FreeCoTaskMem(ptr);
        }
    }
}
