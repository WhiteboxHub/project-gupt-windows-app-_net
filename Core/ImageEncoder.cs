using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Core;

/// <summary>
/// Provides image encoding/decoding functionality for screen frames
/// </summary>
public class ImageEncoder
{
    /// <summary>
    /// Encodes a bitmap to JPEG bytes with specified quality
    /// </summary>
    public byte[] EncodeToJpeg(Bitmap bitmap, int quality = 70)
    {
        var encoder = GetEncoder(ImageFormat.Jpeg);
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

        using var ms = new MemoryStream();
        bitmap.Save(ms, encoder, encoderParams);
        return ms.ToArray();
    }

    /// <summary>
    /// Encodes a bitmap to PNG bytes
    /// </summary>
    public byte[] EncodeToPng(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// Encodes a bitmap to JPEG bytes using MemoryStream (cross-platform compatible)
    /// </summary>
    public byte[] EncodeToJpegStream(Bitmap bitmap, int quality = 70)
    {
        using var ms = new MemoryStream();
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
        
        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        bitmap.Save(ms, jpegEncoder, encoderParams);
        return ms.ToArray();
    }

    /// <summary>
    /// Converts byte array to bitmap
    /// </summary>
    public Bitmap DecodeFromBytes(byte[] data)
    {
        using var ms = new MemoryStream(data);
        return new Bitmap(ms);
    }

    /// <summary>
    /// Compares two bitmaps and returns diff regions
    /// </summary>
    public List<Rectangle> FindChangedRegions(Bitmap oldImage, Bitmap newImage, int threshold = 10)
    {
        var regions = new List<Rectangle>();
        
        if (oldImage.Width != newImage.Width || oldImage.Height != newImage.Height)
        {
            regions.Add(new Rectangle(0, 0, newImage.Width, newImage.Height));
            return regions;
        }

        int blockSize = 32;
        int width = newImage.Width;
        int height = newImage.Height;

        for (int y = 0; y < height; y += blockSize)
        {
            for (int x = 0; x < width; x += blockSize)
            {
                int bw = Math.Min(blockSize, width - x);
                int bh = Math.Min(blockSize, height - y);

                if (HasSignificantDifference(oldImage, newImage, x, y, bw, bh, threshold))
                {
                    regions.Add(new Rectangle(x, y, bw, bh));
                }
            }
        }

        return regions;
    }

    private bool HasSignificantDifference(Bitmap img1, Bitmap img2, int x, int y, int w, int h, int threshold)
    {
        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                var c1 = img1.GetPixel(x + px, y + py);
                var c2 = img2.GetPixel(x + px, y + py);

                int diff = Math.Abs(c1.R - c2.R) + Math.Abs(c1.G - c2.G) + Math.Abs(c1.B - c2.B);
                if (diff > threshold)
                    return true;
            }
        }
        return false;
    }

    private ImageCodecInfo GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        foreach (var codec in codecs)
        {
            if (codec.FormatID == format.Guid)
                return codec;
        }
        throw new Exception("JPEG encoder not found");
    }
}
