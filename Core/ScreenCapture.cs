using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Core;

/// <summary>
/// Provides screen capture functionality using Win32 API
/// </summary>
public class ScreenCapture
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
        IntPtr hdcSource, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int SRCCOPY = 0x00CC0020;

    /// <summary>
    /// Captures the entire desktop screen
    /// </summary>
    public Bitmap CaptureScreen()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);

        IntPtr hDesk = GetDesktopWindow();
        IntPtr hSrce = GetWindowDC(hDesk);
        IntPtr hDest = CreateCompatibleDC(hSrce);
        IntPtr hBmp = CreateCompatibleBitmap(hSrce, width, height);
        IntPtr hOldBmp = SelectObject(hDest, hBmp);

        BitBlt(hDest, 0, 0, width, height, hSrce, 0, 0, SRCCOPY);

        SelectObject(hDest, hOldBmp);
        
        var bitmap = Bitmap.FromHbitmap(hBmp);
        
        DeleteObject(hBmp);
        DeleteDC(hDest);
        ReleaseDC(hDesk, hSrce);

        return bitmap;
    }

    /// <summary>
    /// Captures a specific region of the screen
    /// </summary>
    public Bitmap CaptureRegion(int x, int y, int width, int height)
    {
        IntPtr hDesk = GetDesktopWindow();
        IntPtr hSrce = GetWindowDC(hDesk);
        IntPtr hDest = CreateCompatibleDC(hSrce);
        IntPtr hBmp = CreateCompatibleBitmap(hSrce, width, height);
        IntPtr hOldBmp = SelectObject(hDest, hBmp);

        BitBlt(hDest, 0, 0, width, height, hSrce, x, y, SRCCOPY);

        SelectObject(hDest, hOldBmp);
        
        var bitmap = Bitmap.FromHbitmap(hBmp);
        
        DeleteObject(hBmp);
        DeleteDC(hDest);
        ReleaseDC(hDesk, hSrce);

        return bitmap;
    }

    /// <summary>
    /// Gets the screen dimensions
    /// </summary>
    public (int Width, int Height) GetScreenDimensions()
    {
        return (GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));
    }
}
