﻿using System;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Work;
using FFImageLoading.Config;
using FFImageLoading.Helpers;
using FFImageLoading.Extensions;

#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
using WebPCodec = WebP.Mac.WebPCodec;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
//using WebPCodec = WebP.Touch.WebPCodec;
#endif

namespace FFImageLoading.Decoders
{
    public class WebPDecoder : IDecoder<PImage>
    {
        //WebPCodec _decoder;
		public WebPDecoder(IImageService<PImage> imageService)
		{
			this.imageService = imageService;
		}

		protected readonly IImageService<PImage> imageService;

        public Task<IDecodedImage<PImage>> DecodeAsync(Stream stream, string path, Work.ImageSource source, ImageInformation imageInformation, TaskParameter parameters)
        {
            //if (_decoder == null)
            //    _decoder = new WebPCodec();

            var result = new DecodedImage<PImage>();
           // result.Image = _decoder.Decode(stream);

            var downsampleWidth = parameters.DownSampleSize?.Item1 ?? 0;
            var downsampleHeight = parameters.DownSampleSize?.Item2 ?? 0;
            // TODO allowUpscale
            // bool allowUpscale = parameters.AllowUpscale ?? Configuration.AllowUpscale;

            if (parameters.DownSampleUseDipUnits)
            {
                downsampleWidth = imageService.DpToPixels(downsampleWidth, parameters.Scale);
                downsampleHeight = imageService.DpToPixels(downsampleHeight, parameters.Scale);
            }

            if (downsampleWidth != 0 || downsampleHeight != 0)
            {
                var interpolationMode = parameters.DownSampleInterpolationMode == InterpolationMode.Default ? imageService.Configuration.DownsampleInterpolationMode : parameters.DownSampleInterpolationMode;
                var old = result.Image;
                result.Image = old.ResizeUIImage(downsampleWidth, downsampleHeight, interpolationMode);
                old.TryDispose();
            }

            return Task.FromResult<IDecodedImage<PImage>>(result);
        }
    }
}
