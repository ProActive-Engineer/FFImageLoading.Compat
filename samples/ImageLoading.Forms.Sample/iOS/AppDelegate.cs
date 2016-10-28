﻿using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace FFImageLoading.Forms.Sample.iOS
{
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			FFImageLoading.Forms.Touch.CachedImageRenderer.Init();

			global::Xamarin.Forms.Forms.Init();

			//var config = new FFImageLoading.Config.Configuration()
			//{
			//	VerboseLogging = false,
			//	VerbosePerformanceLogging = false,
			//	VerboseMemoryCacheLogging = false,
			//	VerboseLoadingCancelledLogging = false,
			//	Logger = new CustomLogger(),
			//};
			//ImageService.Instance.Initialize(config);
			//ImageService.Instance.Initialize();
			LoadApplication(new App());

			return base.FinishedLaunching(app, options);
		}

		public class CustomLogger : FFImageLoading.Helpers.IMiniLogger
		{
			public void Debug(string message)
			{
				Console.WriteLine(message);
			}

			public void Error(string errorMessage)
			{
				Console.WriteLine(errorMessage);
			}

			public void Error(string errorMessage, Exception ex)
			{
				Error(errorMessage + System.Environment.NewLine + ex.ToString());
			}
		}
	}
}

