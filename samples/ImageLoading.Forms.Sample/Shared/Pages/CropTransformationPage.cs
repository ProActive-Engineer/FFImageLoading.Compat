﻿using System;
using Xamarin.Forms;
using FFImageLoading.Forms.Sample.PageModels;
using DLToolkit.PageFactory;
using System.Diagnostics;

namespace FFImageLoading.Forms.Sample.Pages
{
    public class CropTransformationPage : ContentPage, IBasePage<CropTransformationPageModel>
	{
		public CropTransformationPage()
		{
			Title = "CropTransformation Demo";

			var cachedImage = new CachedImage() {
				WidthRequest = 300f,
				HeightRequest = 300f,
				DownsampleToViewSize = true,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,
				CacheDuration = TimeSpan.FromDays(30),
				FadeAnimationEnabled = false,
			};

			cachedImage.SetBinding<CropTransformationPageModel>(CachedImage.TransformationsProperty, v => v.Transformations);
			cachedImage.SetBinding<CropTransformationPageModel>(CachedImage.LoadingPlaceholderProperty, v => v.LoadingImagePath);
			cachedImage.SetBinding<CropTransformationPageModel>(CachedImage.ErrorPlaceholderProperty, v => v.ErrorImagePath);
			cachedImage.SetBinding<CropTransformationPageModel>(CachedImage.SourceProperty, v => v.ImagePath);

			var imagePath = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
                HorizontalTextAlignment = TextAlignment.Center,
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label))
			};
            imagePath.SetBinding<CropTransformationPageModel>(Label.TextProperty, v => v.ImagePath);


			var pinchGesture = new PinchGestureRecognizer ();
			pinchGesture.PinchUpdated += (object sender, PinchGestureUpdatedEventArgs e) => {
				if (e.Status == GestureStatus.Started ||
					e.Status == GestureStatus.Running ||
					e.Status == GestureStatus.Completed) {
					this.GetPageModel().PinchImage(e);
				}
			};

			var panGesture = new PanGestureRecognizer ();
			panGesture.PanUpdated += (object sender, PanUpdatedEventArgs e) => {
				if (e.StatusType == GestureStatus.Started ||
					e.StatusType == GestureStatus.Running ||
					e.StatusType == GestureStatus.Completed) {
					this.GetPageModel().PanImage(e);
				}
			};

			cachedImage.GestureRecognizers.Add (pinchGesture);
			cachedImage.GestureRecognizers.Add (panGesture);

			Content = new ScrollView() {
				Content = new StackLayout { 
					Children = {
						imagePath,
						cachedImage
					}
				}
			};
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();

            this.GetPageModel()
                .FreeResources();
		}
	}
}


