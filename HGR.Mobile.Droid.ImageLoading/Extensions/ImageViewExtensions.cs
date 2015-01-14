using Android.Widget;
using HGR.Mobile.Droid.ImageLoading.Work;

namespace HGR.Mobile.Droid.ImageLoading.Extensions
{
	public static class ImageViewExtensions
	{
		/// <summary>
		/// Retrieve the currently active work task (if any) associated with this imageView.
		/// </summary>
		/// <param name="imageView"></param>
		/// <returns></returns>
		public static ImageLoaderTask GetImageLoaderTask(this ImageView imageView)
		{
			if (imageView == null)
				return null;

			var drawable = imageView.Drawable;

			var asyncDrawable = drawable as AsyncDrawable;
			if (asyncDrawable != null)
			{
				return asyncDrawable.GetImageLoaderTask();
			}

			return null;
		}
	}
}