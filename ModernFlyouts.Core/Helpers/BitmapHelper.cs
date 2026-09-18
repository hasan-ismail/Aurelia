using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ModernFlyouts.Core.Helpers
{
    public class BitmapHelper
    {
        public static bool TryCreateBitmapImageFromStream(Stream stream, out BitmapSource bitmap)
        {
            bitmap = null;

            if (stream == null || !stream.CanRead)
            {
                return false;
            }

            try
            {
                if (stream.CanSeek)
                {
                    if (stream.Length == 0)
                    {
                        return false;
                    }

                    // Rewind to the start of the stream. This used to be Seek(0, SeekOrigin.Current),
                    // which seeks zero bytes from the *current* position and therefore does nothing.
                    // Whenever the stream had already been read from - a previous decode attempt, or
                    // the same thumbnail stream being handed out twice - the decoder started midway
                    // through the image data and produced a garbled thumbnail.
                    stream.Seek(0, SeekOrigin.Begin);
                }

                var frame = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

                // OnLoad has already decoded the image, so freezing it makes the result safe to share
                // with the UI thread and lets WPF skip per-render copies.
                if (frame.CanFreeze)
                {
                    frame.Freeze();
                }

                bitmap = frame;
                return true;
            }
            catch (Exception)
            {
                // A truncated or malformed thumbnail from a media app must not take the flyout with it.
                bitmap = null;
                return false;
            }
        }
    }
}
