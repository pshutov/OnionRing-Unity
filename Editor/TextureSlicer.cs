using UnityEngine;

namespace fd.OnionRing
{
	public class TextureSlicer
	{
		public static Texture2D Slice(Texture2D texture, int xStart, int xEnd, int yStart, int yEnd)
		{
			var pixels = texture.GetPixels();
			int width = texture.width;
			int height = texture.height;

			return Slice(pixels, width, height, xStart, xEnd, yStart, yEnd);
		}

		private static Texture2D Slice(Color[] originalPixels, int width, int height, int xStart, int xEnd,
			int yStart,
			int yEnd)
		{
			if (xEnd - xStart < 2)
			{
				xStart = 0;
				xEnd = 0;
			}

			if (yEnd - yStart < 2)
			{
				yStart = 0;
				yEnd = 0;
			}

			var output = GenerateSlicedTexture(originalPixels, width, height, xStart, xEnd, yStart, yEnd);

			return output;
		}

		private static Texture2D GenerateSlicedTexture(Color[] originalPixels, int width, int height, int xStart,
			int xEnd,
			int yStart, int yEnd)
		{
			int outputWidth = width - (xEnd - xStart);
			int outputHeight = height - (yEnd - yStart);
			var outputPixels = new Color[outputWidth * outputHeight];
			for (int x = 0, originalX = 0; x < outputWidth; ++x, ++originalX)
			{
				if (originalX == xStart) originalX += (xEnd - xStart);
				for (int y = 0, originalY = 0; y < outputHeight; ++y, ++originalY)
				{
					if (originalY == yStart) originalY += (yEnd - yStart);
					outputPixels[y * outputWidth + x] = originalPixels[originalY * width + originalX];
				}
			}

			var output = new Texture2D(outputWidth, outputHeight);
			output.SetPixels(outputPixels);
			return output;
		}
	}
}
