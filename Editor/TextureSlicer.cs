using System;
using UnityEngine;

namespace fd.OnionRing
{
	public class TextureSlicer
	{
		public static Texture2D Slice(Texture2D texture, 
			RectInt bounds, Vector4 border)
		{
			var pixels = texture.GetPixels();
			int width = texture.width;
			int height = texture.height;

			return GenerateSlicedTexture(pixels, width, height, bounds, border);
		}

		private static Texture2D GenerateSlicedTexture(Color[] srcPixels, int width, int height,
			RectInt bounds, Vector4 border)
		{
			int left = (int)border.x;
			int bottom = (int)border.y;
			int right = (int)border.z;
			int top = (int)border.w;

			if (right == 0)
			{
				right = bounds.width - left;
			}
			if (top == 0)
			{
				top = bounds.height - bottom;
			}

			int outputWidth = left + right;
			int outputHeight = top + bottom;
			
			var dstPixels = new Color[outputWidth * outputHeight];
			var corners = new (int srcX, int srcY, int srcWidth, int srcHeight, int dstX, int dstY)[]
			{
				// Bottom left
				(srcX: bounds.x, srcY: bounds.y, 
					srcWidth: left, srcHeight: bottom, 
					dstX: 0, dstY: 0),
				// Bottom right
				(srcX: bounds.x + bounds.width - right, srcY: bounds.y, 
					srcWidth: right, srcHeight: bottom, 
					dstX: left, dstY: 0),
				// Top Left
				(srcX: bounds.x, srcY: bounds.y + bounds.height - top, 
					srcWidth: left, srcHeight: top, 
					dstX: 0, dstY: bottom),
				// Top right
				(srcX: bounds.x + bounds.width - right, srcY: bounds.y + bounds.height - top,
					srcWidth: right, srcHeight: top, 
					dstX: left, dstY: bottom),
			};

			for (int i = 0; i < corners.Length; i++)
			{
				(int srcX, int srcY, int scrWidth, int srcHeight, int dstX, int dstY) = corners[i];
				for (int row = 0; row < srcHeight; row++)
				{
					int srcStart = (srcY + row) * width + srcX;
					int dstStart = (dstY + row) * outputWidth + dstX;
					Array.Copy(srcPixels, srcStart, 
						dstPixels, dstStart, 
						scrWidth);
				}
			}

			var output = new Texture2D(outputWidth, outputHeight);
			output.SetPixels(dstPixels);
			return output;
		}
	}
}
