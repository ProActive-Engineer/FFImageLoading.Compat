﻿using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
	public class RotateTransformation : ITransformation
	{
		private const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform (iOS/Android) version";

		public RotateTransformation(double degrees) : this(degrees, false, false)
		{
		}

		public RotateTransformation(double degrees, bool ccw) : this(degrees, ccw, false)
		{
		}

		public RotateTransformation(double degrees, bool ccw, bool resize)
		{
			throw new Exception(DoNotReference);
		}

		public string Key
		{
			get
			{
				throw new Exception(DoNotReference);
			}
		}

		public IBitmap Transform(IBitmap source)
		{
			throw new Exception(DoNotReference);
		}
	}
}

