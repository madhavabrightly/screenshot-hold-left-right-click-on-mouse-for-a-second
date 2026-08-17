using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class Program
{
    private const int WH_MOUSE_LL = 14;

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP   = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP   = 0x0205;

    private static IntPtr hook = IntPtr.Zero;

    private static readonly LowLevelMouseProc HookProc =
        HookCallback;

    private static bool leftDown;
    private static bool rightDown;
    private static bool triggered;

    private static System.Windows.Forms.Timer? holdTimer;

    private delegate IntPtr LowLevelMouseProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll",
        CharSet = CharSet.Auto,
        SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelMouseProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll",
        CharSet = CharSet.Auto,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(
        IntPtr hhk);

    [DllImport("user32.dll",
        CharSet = CharSet.Auto)]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll",
        CharSet = CharSet.Auto,
        SetLastError = true)]
    private static extern IntPtr GetModuleHandle(
        string? lpModuleName);

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        holdTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };

        holdTimer.Tick += HoldTimer_Tick;

        hook = SetHook(HookProc);

        if (hook == IntPtr.Zero)
        {
            MessageBox.Show(
                "Failed to install mouse hook.\n\nError: " +
                Marshal.GetLastWin32Error(),
                "Mouse Screenshot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        Console.WriteLine(
            "Mouse Screenshot Service running.");

        Console.WriteLine(
            "Hold LEFT + RIGHT for 1 second.");

        Console.WriteLine(
            "Screenshot -> Clipboard");

        Console.WriteLine(
            "Press Ctrl+C to exit.");

        Application.Run();

        holdTimer.Stop();

        UnhookWindowsHookEx(hook);
    }

    private static IntPtr SetHook(
        LowLevelMouseProc callback)
    {
        using var process =
            System.Diagnostics.Process.GetCurrentProcess();

        using var module =
            process.MainModule;

        return SetWindowsHookEx(
            WH_MOUSE_LL,
            callback,
            GetModuleHandle(module?.ModuleName),
            0);
    }

    private static IntPtr HookCallback(
        int nCode,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int message = wParam.ToInt32();

            switch (message)
            {
                case WM_LBUTTONDOWN:
                    leftDown = true;
                    CheckCombination();
                    break;

                case WM_LBUTTONUP:
                    leftDown = false;
                    Reset();
                    break;

                case WM_RBUTTONDOWN:
                    rightDown = true;
                    CheckCombination();
                    break;

                case WM_RBUTTONUP:
                    rightDown = false;
                    Reset();
                    break;
            }
        }

        return CallNextHookEx(
            hook,
            nCode,
            wParam,
            lParam);
    }

    private static void CheckCombination()
    {
        if (leftDown &&
            rightDown &&
            !triggered)
        {
            holdTimer!.Stop();
            holdTimer.Start();
        }
    }

    private static void Reset()
    {
        holdTimer!.Stop();
        triggered = false;
    }

    private static void HoldTimer_Tick(
        object? sender,
        EventArgs e)
    {
        holdTimer!.Stop();

        if (leftDown &&
            rightDown &&
            !triggered)
        {
            triggered = true;

            CaptureFullDesktop();
        }
    }

    private static void CaptureFullDesktop()
    {
        Rectangle bounds =
            SystemInformation.VirtualScreen;

        using Bitmap bitmap =
            new Bitmap(
                bounds.Width,
                bounds.Height,
                PixelFormat.Format32bppArgb);

        using Graphics graphics =
            Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(
            bounds.X,
            bounds.Y,
            0,
            0,
            bounds.Size,
            CopyPixelOperation.SourceCopy);

        // Create an independent bitmap because
        // the original will be disposed.
        Bitmap clipboardBitmap =
            new Bitmap(bitmap);

        try
        {
            Clipboard.SetImage(clipboardBitmap);

            Console.WriteLine(
                "Screenshot copied to clipboard: " +
                DateTime.Now.ToString("HH:mm:ss"));
        }
        catch
        {
            clipboardBitmap.Dispose();
            throw;
        }
    }
}