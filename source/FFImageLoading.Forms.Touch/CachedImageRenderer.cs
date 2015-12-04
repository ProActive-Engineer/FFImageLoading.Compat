﻿using CoreGraphics;
using System;
using System.ComponentModel;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using FFImageLoading.Work;
using FFImageLoading;
using Foundation;
using FFImageLoading.Forms;
using FFImageLoading.Forms.Touch;
using System.Collections.Generic;
using FFImageLoading.Extensions;
using System.Threading.Tasks;

[assembly:ExportRenderer(typeof (CachedImage), typeof (CachedImageRenderer))]
namespace FFImageLoading.Forms.Touch
{
	/// <summary>
	/// CachedImage Implementation
	/// </summary>
	[Preserve(AllMembers = true)]
	public class CachedImageRenderer : ViewRenderer<CachedImage, UIImageView>
	{
		private bool _isDisposed;
		private IScheduledWork _currentTask;

		/// <summary>
		///   Used for registration with dependency service
		/// </summary>
		public static new void Init()
		{
			// needed because of this STUPID linker issue: https://bugzilla.xamarin.com/show_bug.cgi?id=31076
			var dummy = new FFImageLoading.Forms.Touch.CachedImageRenderer();

            CachedImage.CacheCleared += CachedImageCacheCleared;
            CachedImage.CacheInvalidated += CachedImageCacheInvalidated;
        }

        private static void CachedImageCacheInvalidated(object sender, CachedImageEvents.CacheInvalidatedEventArgs e)
        {
            ImageService.Invalidate(e.Key, e.CacheType);
        }

        private static void CachedImageCacheCleared(object sender, CachedImageEvents.CacheClearedEventArgs e)
        {
            switch (e.CacheType)
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

        protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
			{
				return;
			}

			UIImage image;
			if (disposing && base.Control != null && (image = base.Control.Image) != null)
			{
				image.Dispose();
			}

			_isDisposed = true;
			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
		{
			if (Control == null)
			{
				SetNativeControl(new UIImageView(CGRect.Empty) {
					ContentMode = UIViewContentMode.ScaleAspectFit,
					ClipsToBounds = true
				});
			}
			if (e.OldElement != null)
			{
				e.OldElement.Cancelled -= Cancel;
			}
			if (e.NewElement != null)
			{
				SetAspect();
				SetImage(e.OldElement);
				SetOpacity();

				e.NewElement.Cancelled += Cancel;
				e.NewElement.InternalGetImageAsJPG = new Func<int, int, int, Task<byte[]>>(GetImageAsJPG);
				e.NewElement.InternalGetImageAsPNG = new Func<int, int, int, Task<byte[]>>(GetImageAsPNG);
			}
			base.OnElementChanged(e);
		}
			
		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
			{
				SetImage(null);
			}
			if (e.PropertyName == CachedImage.IsOpaqueProperty.PropertyName)
			{
				SetOpacity();
			}
			if (e.PropertyName == CachedImage.AspectProperty.PropertyName)
			{
				SetAspect();
			}
		}

		private void SetAspect()
		{
			Control.ContentMode = Element.Aspect.ToUIViewContentMode();
		}

		private void SetOpacity()
		{
			Control.Opaque = Element.IsOpaque;
		}

		private void SetImage(CachedImage oldElement = null)
		{
			Xamarin.Forms.ImageSource source = base.Element.Source; 
			if (oldElement != null)
			{
				Xamarin.Forms.ImageSource source2 = oldElement.Source;
				if (object.Equals(source2, source))
				{
					return;
				}
				if (source2 is FileImageSource && source is FileImageSource && ((FileImageSource)source2).File == ((FileImageSource)source).File)
				{
					return;
				}
			}

			((IElementController)Element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, true);

			TaskParameter imageLoader = null;

			var ffSource = ImageSourceBinding.GetImageSourceBinding(source);

			if (ffSource == null)
			{
				if (Control != null)
					Control.Image = null;
				
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
				// LoadingPlaceholder
				if (Element.LoadingPlaceholder != null)
				{
					var placeholderSource = ImageSourceBinding.GetImageSourceBinding(Element.LoadingPlaceholder);
					if (placeholderSource != null)
						imageLoader.LoadingPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
				}

				// ErrorPlaceholder
				if (Element.ErrorPlaceholder != null)
				{
					var placeholderSource = ImageSourceBinding.GetImageSourceBinding(Element.ErrorPlaceholder);
					if (placeholderSource != null)
						imageLoader.ErrorPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
				}

				// Downsample
				if ((int)Element.DownsampleHeight != 0 || (int)Element.DownsampleWidth != 0)
				{
                    if (Element.DownsampleHeight > Element.DownsampleWidth)
                    {
                        imageLoader.DownSample(height: (int)Element.DownsampleHeight);
                    }
                    else
                    {
                        imageLoader.DownSample(width: (int)Element.DownsampleWidth);
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

				// Transformations
				if (Element.Transformations != null)
				{
					imageLoader.Transform(Element.Transformations);
				}

				imageLoader.Finish((work) => ImageLoadingFinished(Element));
				_currentTask = imageLoader.Into(Control);	
			}
		}

		private void ImageLoadingFinished(CachedImage element)
		{
			if (element != null && !_isDisposed)
			{
				((IElementController)element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, false);
				((IVisualElementController)element).NativeSizeChanged();
				element.InvalidateViewMeasure();
			}
		}

		private void Cancel(object sender, EventArgs args)
		{
			if (_currentTask != null && !_currentTask.IsCancelled) {
				_currentTask.Cancel ();
			}
		}
			
		private Task<byte[]> GetImageAsJPG(int quality, int desiredWidth = 0, int desiredHeight = 0)
		{
			return GetImageAsByte(false, quality, desiredWidth, desiredHeight);
		}

		private Task<byte[]> GetImageAsPNG(int quality, int desiredWidth = 0, int desiredHeight = 0)
		{
			return GetImageAsByte(true, quality, desiredWidth, desiredHeight);
		}

		private async Task<byte[]> GetImageAsByte(bool usePNG, int quality, int desiredWidth, int desiredHeight)
		{
			if (Control == null || Control.Image == null)
				return null;

			UIImage image = Control.Image;

			if (desiredWidth != 0 || desiredHeight != 0)
			{
				image = image.ResizeUIImage((double)desiredWidth, (double)desiredHeight);
			}

			NSData imageData = usePNG ? image.AsPNG() : image.AsJPEG((nfloat)quality / 100f);

			if (imageData == null || imageData.Length == 0)
				return null;

			var encoded = imageData.ToArray();
			imageData.Dispose();

			if (desiredWidth != 0 || desiredHeight != 0)
			{
				image.Dispose();
			}

			return encoded;
		}
	}
}

