using GaneshaDx.Resources.ResourceContent;
using GaneshaDx.Resources;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using GaneshaDx.Resources.ContentDataTypes.Palettes;
using GaneshaDx.Environment;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace GaneshaDx.Common {
	internal class TextureImport {
		/// <summary>
		/// Given a Texture2D, pulls the color data into a palette, sets to greyscale, and resizes to 256x1024
		/// </summary>
		/// <param name="importedTexture"></param>
		/// <param name="Quantized">if true, the image has already been quantized and greyscaled, just resize and import</param>
		public static void ImportTexture(Texture2D importedTexture, Boolean Quantized = true) {
			Texture2D resizedTexture;
			if (!Quantized) {
				var quantizedTexture = Quantizer.QuantizeToPaletteAndListPixels(importedTexture, out Palette importedPalette, out _);
				ImportPalette(importedPalette, 0, "main"); //TODO: ask user where to import the palette, or to ignore it
				resizedTexture = ResizeTexture(quantizedTexture, 256, 1024);
			}
			else {
				resizedTexture = ResizeTexture(importedTexture, 256, 1024);
			}

			foreach (MapResource textureResource in MapData.TextureResources) {
				if (
					textureResource.MapArrangementState == CurrentMapState.StateData.MapArrangementState &&
					textureResource.MapTime == CurrentMapState.StateData.MapTime &&
					textureResource.MapWeather == CurrentMapState.StateData.MapWeather
				) {
					TextureResourceData textureResourceData = (TextureResourceData)textureResource.ResourceData;
					textureResourceData.Texture = resizedTexture;
					CurrentMapState.StateData.Texture = resizedTexture;
					break;
				}
			}
		}

		/// <summary>
		/// Use the palette key to quickly grab the equivalent colors that were chosen as palette colors in the base image
		/// </summary>
		/// <param name="importedTexture"></param>
		/// <param name="paletteSourcePixels"></param>
		/// <returns></returns>
		public static Palette GetPaletteWithSourcePixels(Texture2D importedTexture, List<Tuple<Color, Tuple<int, int>>> paletteSourcePixels) {
			var palette = new Palette();
			Color[] colors = new Color[importedTexture.Width * importedTexture.Height];
			importedTexture.GetData(colors);
			foreach (Tuple<Color, Tuple<int, int>> entry in paletteSourcePixels) {
				var x = entry.Item2.Item1;
				var y = entry.Item2.Item2;

				var originalPixel = colors[y * importedTexture.Width + x];
				if (originalPixel.A == 0) {
					palette.Colors.Add(new PaletteColor(0, 0, 0, true));
				}
				else {
					palette.Colors.Add(new PaletteColor((originalPixel.R + 3) / 8, (originalPixel.G + 3) / 8, (originalPixel.B + 3) / 8, false));
				}
			}
			return palette;
		}

		public static void ImportPalette(Palette sourcePalette, int paletteId, string paletteType) {
			List<PaletteColor> targetPalette = paletteType == "main"
				? CurrentMapState.StateData.Palettes[paletteId].Colors
				: CurrentMapState.StateData.PaletteAnimationFrames[paletteId].Colors;

			for (int colorIndex = 0; colorIndex < sourcePalette.Colors.Count; colorIndex++) {
				targetPalette[colorIndex].Red = sourcePalette.Colors[colorIndex].Red;
				targetPalette[colorIndex].Green = sourcePalette.Colors[colorIndex].Green;
				targetPalette[colorIndex].Blue = sourcePalette.Colors[colorIndex].Blue;
			}
		}

		/// <summary>
		/// Resizes a Texture2D to a new size. GaneshaDX operates on 256x1024 textures
		/// </summary>
		/// <param name="texture2D"></param>
		/// <param name="targetX"></param>
		/// <param name="targetY"></param>
		/// <returns></returns>
		//TODO: handle the case where the source texture is smaller in dimensions than 256x1024, padding the image instead of stretching it
		public static Texture2D ResizeTexture(Texture2D texture2D, int targetX, int targetY) {

			RenderTarget2D renderTarget = new(Stage.GraphicsDevice, targetX, targetY);

			Stage.GraphicsDevice.SetRenderTarget(renderTarget);
			Stage.GraphicsDevice.Clear(Color.Transparent);

			SpriteBatch batch = new(Stage.GraphicsDevice);
			batch.Begin();
			batch.Draw(texture2D, new Rectangle(0, 0, targetX, targetY), Color.White);
			batch.End();

			Stage.GraphicsDevice.SetRenderTarget(null);

			return (Texture2D)renderTarget;
		}
	}
}
