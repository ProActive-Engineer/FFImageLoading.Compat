﻿using System;

namespace FFImageLoading
{
    public class PlatformPerformance : IPlatformPerformance
    {
        public static IPlatformPerformance Create(bool verbose)
        {
            if (!verbose)
                return new EmptyPlatformPerformance();

            return new PlatformPerformance();
        }

        public PlatformPerformance()
        {
        }

        public int GetCurrentManagedThreadId()
        {
            return 0;
        }

        public int GetCurrentSystemThreadId()
        {
            return 0;
        }

        public string GetMemoryInfo()
        {
            return "[PERFORMANCE] Memory - not implemented";
        }
    }
}

