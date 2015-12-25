﻿using FFImageLoading.Work;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Extensions
{
    public static class ImageExtensions
    {
        public unsafe async static Task<WriteableBitmap> ToBitmapImageAsync(this BitmapHolder holder)
        {
            if (holder == null || holder.Pixels == null)
                return null;

            var writeableBitmap = new WriteableBitmap(holder.Width, holder.Height);

            using (var stream = writeableBitmap.PixelBuffer.AsStream())
            {
                int length = holder.Pixels.Length;

                var buffer = new byte[length * 4];
                fixed (int* srcPtr = holder.Pixels)
                {
                    var b = 0;
                    for (var i = 0; i < length; i++, b += 4)
                    {
                        var p = srcPtr[i];
                        buffer[b + 3] = (byte)((p >> 24) & 0xff);
                        buffer[b + 2] = (byte)((p >> 16) & 0xff);
                        buffer[b + 1] = (byte)((p >> 8) & 0xff);
                        buffer[b + 0] = (byte)(p & 0xff);
                    }
                    stream.Write(buffer, 0, length * 4);
                }

            }
            writeableBitmap.Invalidate();

            return writeableBitmap;
        }

        public async static Task<WriteableBitmap> ToBitmapImageAsync(this byte[] imageBytes, Tuple<int, int> downscale, InterpolationMode mode)
        {
            if (imageBytes == null)
                return null;

            IRandomAccessStream image = imageBytes.AsBuffer().AsStream().AsRandomAccessStream();

            if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
            {
				image = await image.ResizeImage((uint)downscale.Item1, (uint)downscale.Item2, mode).ConfigureAwait(false);
            }

            using (image)
            {
				BitmapDecoder decoder = await BitmapDecoder.CreateAsync(image).ConfigureAwait(false);

                image.Seek(0);

                WriteableBitmap bitmap = null;

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () =>
                {
                    bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    await bitmap.SetSourceAsync(image);
				}).ConfigureAwait(false);

                return bitmap;
            }
        }

        public async static Task<BitmapHolder> ToBitmapHolderAsync(this byte[] imageBytes, Tuple<int, int> downscale, InterpolationMode mode)
        {
            if (imageBytes == null)
                return null;

            IRandomAccessStream image = imageBytes.AsBuffer().AsStream().AsRandomAccessStream();

            if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
            {
				image = await image.ResizeImage((uint)downscale.Item1, (uint)downscale.Item2, mode).ConfigureAwait(false);
            }

            using (image)
            {
				BitmapDecoder decoder = await BitmapDecoder.CreateAsync(image).ConfigureAwait(false);
				PixelDataProvider pixelDataProvider = await decoder.GetPixelDataAsync().ConfigureAwait(false);

                var bytes = pixelDataProvider.DetachPixelData();
                int[] array = new int[decoder.PixelWidth * decoder.PixelHeight];
                CopyPixels(bytes, array);

                return new BitmapHolder(array, (int)decoder.PixelWidth, (int)decoder.PixelHeight);
            }
        }

        private static unsafe void CopyPixels(byte[] data, int[] pixels)
        {
            int length = pixels.Length;

            fixed (byte* srcPtr = data)
            {
                fixed (int* dstPtr = pixels)
                {
                    for (var i = 0; i < length; i++)
                    {
                        dstPtr[i] = (srcPtr[i * 4 + 3] << 24)
                                  | (srcPtr[i * 4 + 2] << 16)
                                  | (srcPtr[i * 4 + 1] << 8)
                                  | srcPtr[i * 4 + 0];
                    }
                }
            }
        }

        public static async Task<IRandomAccessStream> ResizeImage(this IRandomAccessStream imageStream, uint width, uint height, InterpolationMode mode)
        {
            IRandomAccessStream resizedStream = imageStream;
			var decoder = await BitmapDecoder.CreateAsync(imageStream).ConfigureAwait(false);
            if (decoder.OrientedPixelHeight > height || decoder.OrientedPixelWidth > width)
            {
                using (imageStream)
                {
                    resizedStream = new InMemoryRandomAccessStream();
					BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder).ConfigureAwait(false);
                    double widthRatio = (double)width / decoder.OrientedPixelWidth;
                    double heightRatio = (double)height / decoder.OrientedPixelHeight;

                    double scaleRatio = Math.Min(widthRatio, heightRatio);

                    if (width == 0)
                        scaleRatio = heightRatio;

                    if (height == 0)
                        scaleRatio = widthRatio;

                    uint aspectHeight = (uint)Math.Floor(decoder.OrientedPixelHeight * scaleRatio);
                    uint aspectWidth = (uint)Math.Floor(decoder.OrientedPixelWidth * scaleRatio);

                    // it's okay. Windows's BitmapInterpolationMode has the same numerical values as ours InterpolationMode.
                    encoder.BitmapTransform.InterpolationMode = (BitmapInterpolationMode)mode;
                    encoder.BitmapTransform.ScaledHeight = aspectHeight;
                    encoder.BitmapTransform.ScaledWidth = aspectWidth;

					await encoder.FlushAsync().ConfigureAwait(false);
                    resizedStream.Seek(0);
                }
            }

            return resizedStream;
        }
    }
}
