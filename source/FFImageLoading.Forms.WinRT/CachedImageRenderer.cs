using FFImageLoading.Forms;
using FFImageLoading.Work;
using FFImageLoading.Extensions;
using System;
using System.ComponentModel;
using Windows.Graphics.Imaging;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using System.IO;

#if WINDOWS_UWP
using FFImageLoading.Forms.WinUWP;
using Xamarin.Forms.Platform.UWP;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;

#elif SILVERLIGHT
using FFImageLoading.Forms.WinSL;
using Xamarin.Forms.Platform.WinPhone;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#else
using FFImageLoading.Forms.WinRT;
using Xamarin.Forms.Platform.WinRT;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;
#endif

#if WINDOWS_UWP
[assembly: ExportRenderer(typeof(CachedImage), typeof(CachedImageRenderer))]
namespace FFImageLoading.Forms.WinUWP
#elif SILVERLIGHT
[assembly: Xamarin.Forms.ExportRenderer(typeof(CachedImage), typeof(CachedImageRenderer))]
namespace FFImageLoading.Forms.WinSL
#else
[assembly: ExportRenderer(typeof(CachedImage), typeof(CachedImageRenderer))]
namespace FFImageLoading.Forms.WinRT
#endif
{
    /// <summary>
    /// CachedImage Implementation
    /// </summary>
    public class CachedImageRenderer : ViewRenderer<CachedImage, Image>
    {
        private IScheduledWork _currentTask;

        /// <summary>
        ///   Used for registration with dependency service
        /// </summary>
        public static void Init()
        {
            CachedImage.InternalClearCache = new Action<FFImageLoading.Cache.CacheType>(ClearCache);
            CachedImage.InternalInvalidateCache = new Action<string, FFImageLoading.Cache.CacheType>(InvalidateCache);
        }

        private static void InvalidateCache(string key, Cache.CacheType cacheType)
        {
            ImageService.Invalidate(key, cacheType);
        }

        private static void ClearCache(Cache.CacheType cacheType)
        {
            switch (cacheType)
            {
                case Cache.CacheType.Memory:
                    ImageService.InvalidateMemoryCache();
                    break;
                case Cache.CacheType.Disk:
                    ImageService.InvalidateDiskCache();
                    break;
                case Cache.CacheType.All:
                    ImageService.InvalidateMemoryCache();
                    ImageService.InvalidateDiskCache();
                    break;
            }
        }

        private bool measured;

        public override Xamarin.Forms.SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            var bitmapSource = Control.Source as BitmapSource;

            if (bitmapSource == null)
                return new Xamarin.Forms.SizeRequest();

            measured = true;

            return new Xamarin.Forms.SizeRequest(new Xamarin.Forms.Size()
            {
                Width = bitmapSource.PixelWidth,
                Height = bitmapSource.PixelHeight
            });
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement == null)
                return;

            if (Control == null)
            {
                Image control = new Image()
                {
                    Stretch = GetStretch(Xamarin.Forms.Aspect.AspectFill)
                };
                control.ImageOpened += OnImageOpened;
                SetNativeControl(control);
            }

            if (e.OldElement != null)
            {
                e.OldElement.Cancelled -= Cancel;
            }
            if (e.NewElement != null)
            {
                e.NewElement.Cancelled += Cancel;
                e.NewElement.InternalGetImageAsJPG = new Func<int, int, int, Task<byte[]>>(GetImageAsJPG);
                e.NewElement.InternalGetImageAsPNG = new Func<int, int, int, Task<byte[]>>(GetImageAsPNG);
            }

            UpdateSource();
            UpdateAspect();
        }

#if SILVERLIGHT
// TODO unsubscribe event!!!
#else
        protected override void Dispose(bool disposing)
        {
            if (Control != null)
            {
                Control.ImageOpened -= OnImageOpened;
            }
            base.Dispose(disposing);
        }
#endif

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
            {
                UpdateSource();
            }
            else
            {
                if (!(e.PropertyName == CachedImage.AspectProperty.PropertyName))
                    return;

                UpdateAspect();
            }
        }

        private void OnImageOpened(object sender, RoutedEventArgs routedEventArgs)
        {
            if (measured)
            {
                ((Xamarin.Forms.IVisualElementController)Element).NativeSizeChanged();
            }
        }

        private async void UpdateSource()
        {
            ((Xamarin.Forms.IElementController)Element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, true);

            Xamarin.Forms.ImageSource source = Element.Source;

            Cancel(this, EventArgs.Empty);
            TaskParameter imageLoader = null;

            var ffSource = await ImageSourceBinding.GetImageSourceBinding(source).ConfigureAwait(false);

            if (ffSource == null)
            {
                if (Control != null)
                    Control.Source = null;

                ImageLoadingFinished(Element);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Url)
            {
                imageLoader = ImageService.LoadUrl(ffSource.Path, Element.CacheDuration);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.CompiledResource)
            {
                imageLoader = ImageService.LoadCompiledResource(ffSource.Path);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.ApplicationBundle)
            {
                imageLoader = ImageService.LoadFileFromApplicationBundle(ffSource.Path);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Filepath)
            {
                imageLoader = ImageService.LoadFile(ffSource.Path);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Stream)
            {
                imageLoader = ImageService.LoadStream(ffSource.Stream);
            }

            if (imageLoader != null)
            {
				// CustomKeyFactory
				if (Element.CacheKeyFactory != null)
				{
					imageLoader.CacheKey(Element.CacheKeyFactory.GetKey(source));
				}

                // LoadingPlaceholder
                if (Element.LoadingPlaceholder != null)
                {
                    var placeholderSource = await ImageSourceBinding.GetImageSourceBinding(Element.LoadingPlaceholder).ConfigureAwait(false);
                    if (placeholderSource != null)
                        imageLoader.LoadingPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
                }

                // ErrorPlaceholder
                if (Element.ErrorPlaceholder != null)
                {
                    var placeholderSource = await ImageSourceBinding.GetImageSourceBinding(Element.ErrorPlaceholder).ConfigureAwait(false);
                    if (placeholderSource != null)
                        imageLoader.ErrorPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
                }

                // Downsample
                if (Element.DownsampleToViewSize && (Element.Width > 0 || Element.Height > 0))
                {
                    if (Element.Height > Element.Width)
                    {
                        imageLoader.DownSample(height: Element.Height.PointsToPixels());
                    }
                    else
                    {
                        imageLoader.DownSample(width: Element.Width.PointsToPixels());
                    }
                }
                else if (Element.DownsampleToViewSize && (Element.WidthRequest > 0 || Element.HeightRequest > 0))
                {
                    if (Element.HeightRequest > Element.WidthRequest)
                    {
                        imageLoader.DownSample(height: Element.HeightRequest.PointsToPixels());
                    }
                    else
                    {
                        imageLoader.DownSample(width: Element.WidthRequest.PointsToPixels());
                    }
                }
                else if ((int)Element.DownsampleHeight != 0 || (int)Element.DownsampleWidth != 0)
                {
                    if (Element.DownsampleHeight > Element.DownsampleWidth)
                    {
                        imageLoader.DownSample(height: Element.DownsampleUseDipUnits
                            ? Element.DownsampleHeight.PointsToPixels() : (int)Element.DownsampleHeight);
                    }
                    else
                    {
                        imageLoader.DownSample(width: Element.DownsampleUseDipUnits
                            ? Element.DownsampleWidth.PointsToPixels() : (int)Element.DownsampleWidth);
                    }
                }

                // RetryCount
                if (Element.RetryCount > 0)
                {
                    imageLoader.Retry(Element.RetryCount, Element.RetryDelay);
                }

                // TransparencyChannel
                if (Element.TransparencyEnabled.HasValue)
                    imageLoader.TransparencyChannel(Element.TransparencyEnabled.Value);

                // FadeAnimation
                if (Element.FadeAnimationEnabled.HasValue)
                    imageLoader.FadeAnimation(Element.FadeAnimationEnabled.Value);

                // TransformPlaceholders
                if (Element.TransformPlaceholders.HasValue)
                    imageLoader.TransformPlaceholders(Element.TransformPlaceholders.Value);

                // Transformations
                if (Element.Transformations != null && Element.Transformations.Count > 0)
                {
                    imageLoader.Transform(Element.Transformations);
                }

                var element = Element;

                imageLoader.Finish((work) =>
                {
                    element.OnFinish(new CachedImageEvents.FinishEventArgs(work));
                    ImageLoadingFinished(element);
                });

                imageLoader.Success((imageSize, loadingResult) =>
                    element.OnSuccess(new CachedImageEvents.SuccessEventArgs(imageSize, loadingResult)));

                imageLoader.Error((exception) =>
                    element.OnError(new CachedImageEvents.ErrorEventArgs(exception)));

                _currentTask = imageLoader.Into(Control);
            }
        }

        private void UpdateAspect()
        {
            Control.Stretch = GetStretch(Element.Aspect);
        }

        private static Stretch GetStretch(Xamarin.Forms.Aspect aspect)
        {
            switch (aspect)
            {
                case Xamarin.Forms.Aspect.AspectFill:
                    return Stretch.UniformToFill;
                case Xamarin.Forms.Aspect.Fill:
                    return Stretch.Fill;
                default:
                    return Stretch.Uniform;
            }
        }

        private void ImageLoadingFinished(CachedImage element)
        {
            if (element != null)
            {
                ((Xamarin.Forms.IElementController)element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, false);
                ((Xamarin.Forms.IVisualElementController)element).NativeSizeChanged();
                element.InvalidateViewMeasure();
            }
        }

        private void Cancel(object sender, EventArgs args)
        {
            if (_currentTask != null && !_currentTask.IsCancelled)
            {
                _currentTask.Cancel();
            }
        }

        private Task<byte[]> GetImageAsJPG(int quality, int desiredWidth = 0, int desiredHeight = 0)
        {
            return GetImageAsByte(BitmapEncoder.JpegEncoderId, quality, desiredWidth, desiredHeight);
        }

        private Task<byte[]> GetImageAsPNG(int quality, int desiredWidth = 0, int desiredHeight = 0)
        {
            return GetImageAsByte(BitmapEncoder.PngEncoderId, quality, desiredWidth, desiredHeight);
        }

        private async Task<byte[]> GetImageAsByte(Guid format, int quality, int desiredWidth, int desiredHeight)
        {
            if (Control == null || Control.Source == null)
                return null;

            var bitmap = Control.Source as WriteableBitmap;

            if (bitmap == null)
                return null;

            byte[] pixels = null;
            uint pixelsWidth = (uint)bitmap.PixelWidth;
            uint pixelsHeight = (uint)bitmap.PixelHeight;

            if (desiredWidth != 0 || desiredHeight != 0)
            {
                double widthRatio = (double)desiredWidth / (double)bitmap.PixelWidth;
                double heightRatio = (double)desiredHeight / (double)bitmap.PixelHeight;

                double scaleRatio = Math.Min(widthRatio, heightRatio);

                if (desiredWidth == 0)
                    scaleRatio = heightRatio;

                if (desiredHeight == 0)
                    scaleRatio = widthRatio;

                uint aspectWidth = (uint)((double)bitmap.PixelWidth * scaleRatio);
                uint aspectHeight = (uint)((double)bitmap.PixelHeight * scaleRatio);

                using (var tempStream = new InMemoryRandomAccessStream())
                {
                    byte[] tempPixels = await GetBytesFromBitmapAsync(bitmap);

                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, tempStream);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                        pixelsWidth, pixelsHeight, 96, 96, tempPixels);
                    await encoder.FlushAsync();
                    tempStream.Seek(0);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(tempStream);
                    BitmapTransform transform = new BitmapTransform()
                    {
                        ScaledWidth = aspectWidth,
                        ScaledHeight = aspectHeight,
                        InterpolationMode = BitmapInterpolationMode.Linear
                    };
                    PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        transform,
                        ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.DoNotColorManage);

                    pixels = pixelData.DetachPixelData();
                    pixelsWidth = aspectWidth;
                    pixelsHeight = aspectHeight;
                }
            }
            else
            {
                pixels = await GetBytesFromBitmapAsync(bitmap);
            }

            using (var stream = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(format, stream);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                    pixelsWidth, pixelsHeight, 96, 96, pixels);
                await encoder.FlushAsync();
                stream.Seek(0);

                var bytes = new byte[stream.Size];
                await stream.ReadAsync(bytes.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);

                return bytes;
            }
        }

        private async Task<byte[]> GetBytesFromBitmapAsync(WriteableBitmap bitmap)
        {
#if SILVERLIGHT
            return await Task.FromResult(bitmap.ToByteArray());
#else
            byte[] tempPixels;
            using (var sourceStream = bitmap.PixelBuffer.AsStream())
            {
                tempPixels = new byte[sourceStream.Length];
				await sourceStream.ReadAsync(tempPixels, 0, tempPixels.Length).ConfigureAwait(false);
            }

            return tempPixels;
#endif
        }
    }
}
