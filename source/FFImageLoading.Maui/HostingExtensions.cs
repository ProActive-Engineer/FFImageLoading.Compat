﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FFImageLoading.Maui
{
	public static class HostingExtensions
	{
		public static IImageSourceServiceCollection UseFFImageLoading(this IImageSourceServiceCollection imageSourceServices)
		{

#if IOS || MACCATALYST || ANDROID
			imageSourceServices.RemoveAll<FileImageSource>();
			imageSourceServices.RemoveAll<StreamImageSource>();
			imageSourceServices.RemoveAll<UriImageSource>();

			imageSourceServices.AddService<FileImageSource>(svcs => new Platform.FileImageSourceService());
			imageSourceServices.AddService<StreamImageSource>(svcs => new Platform.StreamImageSourceService());
			imageSourceServices.AddService<UriImageSource>(svcs => new Platform.UriImageSourceService());
#endif

			return imageSourceServices;
		}

		public static MauiAppBuilder RegisterFFImageLoadingHandlers(this MauiAppBuilder mauiAppBuilder)
		{
#if IOS || MACCATALYST
			FFImageLoading.Maui.Platform.CachedImageHandler.Init();
#endif
			mauiAppBuilder.ConfigureMauiHandlers(c =>
			{
				c.AddHandler(typeof(FFImageLoading.Maui.CachedImage), typeof(FFImageLoading.Maui.Platform.CachedImageHandler));
			});



			return mauiAppBuilder;
		}
	}
}
