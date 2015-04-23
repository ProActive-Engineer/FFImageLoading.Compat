﻿using System;
using System.Threading.Tasks;
using System.IO;

namespace FFImageLoading.IO
{
    internal static class FileStore
    {
		public static Stream GetInputStream(string path)
		{
			return new FileStream(path, FileMode.Open, FileAccess.Read);
		}

		public static Stream GetOutputStream(string path)
		{
			return new FileStream(path, FileMode.Create, FileAccess.Write);
		}

        public static async Task<byte[]> ReadBytes(string path)
        {
			using (var fs = GetInputStream(path)) {
                using (var memory = new MemoryStream()) {
                    await fs.CopyToAsync(memory).ConfigureAwait(false);
                    return memory.ToArray();
                }
            }
        }

        public static async Task WriteBytes(string path, byte[] data)
        {
            using (var fs = GetOutputStream(path)) {
                using (var memory = new MemoryStream(data)) {
                    await memory.CopyToAsync(fs);
                }
            }
        }
    }
}

