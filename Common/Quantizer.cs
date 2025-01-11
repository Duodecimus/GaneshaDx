using System;
using System.Collections.Generic;
using System.Linq;
using Color = Microsoft.Xna.Framework.Color;
using GaneshaDx.Environment;
using GaneshaDx.Resources.ContentDataTypes.Palettes;
using Microsoft.Xna.Framework.Graphics;

namespace GaneshaDx.Common;
public class Quantizer {

	public static Texture2D QuantizeToPaletteAndListPixels(Texture2D importedTexture, out Palette importedPalette, out List<Tuple<Color, Tuple<int, int>>> paletteSourcePixels) {
		//Initialize the lists
		importedPalette = new Palette();
		var ListGreyscale = new List<int> { 0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255 };

		//Extract all pixels from the original texture
		Color[] originalPixels = new Color[importedTexture.Width * importedTexture.Height];
		importedTexture.GetData(originalPixels);

		//Convert the colors to a list of Ganesha valid RGB values
		List<Color> colorList = originalPixels.Select(c =>
		new Color(((int)c.R + 3) / 8 * 8, ((int)c.G + 3) / 8 * 8, ((int)c.B + 3) / 8 * 8)).ToList();
		//Check and see if the image is already quantized
		List<Color> uniqueColors = GetUniqueColors(colorList);
		if (uniqueColors.Count <= 16) {
			//Find the source pixels, so other textures can find them in the same order
			paletteSourcePixels = GetPalettePixelsFromQuantizedTexture(colorList, uniqueColors, importedTexture.Width);
		}
		else {
			//Quantize the colors to 16 colors
			//TODO: add some kind of 'in-progress' animation, as all of these appear to cause the program to freeze for multiple seconds.
			//paletteSourcePixels = QuantizeColorsKMeans(colorList, 16, importedTexture.Width); //160441 ms on a 2048x2048x texture
			//paletteSourcePixels = QuantizeColorsUniform(colorList, 16, importedTexture.Width); //8427 ms
			paletteSourcePixels = QuantizeColorsMedianCut(colorList, 16, importedTexture.Width); //14714 ms
			//paletteSourcePixels = QuantizeColorsByFrequency(colorList, 16, importedTexture.Width); //8278 ms
		}

		List<Color> quantizedColors = new();

		//pull out the colors for better list searching, and create the palette
		foreach (Tuple<Color, Tuple<int, int>> Color in paletteSourcePixels) {
			quantizedColors.Add(Color.Item1);
			if (Color.Item1.A == 0) {
				importedPalette.Colors.Add(new PaletteColor(0, 0, 0, true)); //actually transparent pixels must be black
			}
			else {
				importedPalette.Colors.Add(new PaletteColor(Color.Item1.R / 8, Color.Item1.G / 8, Color.Item1.B / 8, false));
			}
		}

		// Create a map of coordinates for each quantized color
		// Replace original colors with the closest color in the quantized palette
		//List<Color> quantizedPixels = new();
		List<Color> greyscalePixels = new();

		HashSet<Color> addedColors = new();

		// Loop through each pixel
		for (int y = 0; y < importedTexture.Height; y++) {
			for (int x = 0; x < importedTexture.Width; x++) {
				// Get the color of the current pixel
				Color originalColor = originalPixels[y * importedTexture.Width + x];
				//Get the palette color it is closest to
				Color closestColor = FindClosestColor(originalColor, quantizedColors);
				//Get the index in the palette of that color
				int index = quantizedColors.FindIndex(pixel => pixel.Equals(closestColor));
				//Use the index to instead change to greyscale
				//quantizedPixels.Add(closestColor);
				greyscalePixels.Add(new Color(ListGreyscale[index], ListGreyscale[index], ListGreyscale[index]));
			}
		}

		Texture2D greyscaleTexture = new(Stage.GraphicsDevice, importedTexture.Width, importedTexture.Height);
		greyscaleTexture.SetData(greyscalePixels.ToArray());

		//Stream stream = File.Create("greyscaleTexture.png");
		//greyscaleTexture.SaveAsPng(stream, importedTexture.Width, importedTexture.Height);
		//stream.Dispose();

		return greyscaleTexture;
	}
	public static List<Tuple<Color, Tuple<int, int>>> GetPalettePixelsFromQuantizedTexture(List<Color> colors, List<Color> UniqueColors, int width) {
		List<Tuple<Color, Tuple<int, int>>> uniqueColorsWithCoordinates = new();

		for (int i = 0; i < colors.Count; i++) {
			if (UniqueColors.Count == 0) { break; }
			for (int j = 0; j < UniqueColors.Count; j++) {
				if (colors[i] == UniqueColors[j]) {
					int x = i % width; // X-coordinate in the image
					int y = i / width; // Y-coordinate in the image
					uniqueColorsWithCoordinates.Add(new Tuple<Color, Tuple<int, int>>(colors[i], new Tuple<int, int>(x, y)));
					UniqueColors.RemoveAt(j);
					break;
				}
			}
		}

		while (uniqueColorsWithCoordinates.Count < 16) {
			uniqueColorsWithCoordinates.Add(uniqueColorsWithCoordinates.Last());
		}

		return uniqueColorsWithCoordinates;
	}
	private static List<Color> GetUniqueColors(List<Color> colors) {
		List<Color> uniqueColors = colors
			.GroupBy(c => new { c.R, c.G, c.B, c.A })
			.Select(g => g.First())
			.ToList();

		return uniqueColors;
	}

	//Find the closest color from the quantized palette
	private static Color FindClosestColor(Color targetColor, List<Color> palette) {
		return palette.OrderBy(p => Math.Pow(targetColor.R - p.R, 2) +
									Math.Pow(targetColor.G - p.G, 2) +
									Math.Pow(targetColor.B - p.B, 2))
									.First();
	}

	public static List<Tuple<Color, Tuple<int, int>>> QuantizeColorsByFrequency(List<Color> colors, int numColors, int width) {
		// Calculate the bin size for each color channel
		int step = 256 / numColors;

		// Dictionary to store color frequencies and their first occurrences
		Dictionary<Color, Tuple<int, int>> firstOccurrence = new();
		Dictionary<Color, int> colorFrequency = new();

		// Process all colors in the input image
		for (int i = 0; i < colors.Count; i++) {
			Color color = colors[i];

			// Quantize each channel (R, G, B)
			int r = (color.R / step) * step;
			int g = (color.G / step) * step;
			int b = (color.B / step) * step;

			// Create the quantized color from RGB values
			Color quantizedColor = new(r, g, b);  // Microsoft.Xna.Framework.Color constructor

			// Track the first occurrence of each quantized color
			if (!firstOccurrence.ContainsKey(quantizedColor)) {
				int x = i % width; // X-coordinate in the image
				int y = i / width; // Y-coordinate in the image
				firstOccurrence[quantizedColor] = new Tuple<int, int>(x, y);
			}

			// Increment the frequency count for this quantized color
			if (!colorFrequency.ContainsKey(quantizedColor)) {
				colorFrequency[quantizedColor] = 1;
			}
			else {
				colorFrequency[quantizedColor]++;
			}
		}

		// Sort the colors by frequency (descending order) and select the top `numColors`
		var mostFrequentColors = colorFrequency
			.OrderByDescending(c => c.Value)  // Order by frequency in descending order
			.Take(numColors)  // Take the top `numColors` most frequent colors
			.Select(c => c.Key)  // Select only the color (discard the frequency)
			.ToList();

		// Create the final list of quantized colors with their first occurrences
		List<Tuple<Color, Tuple<int, int>>> quantizedColorsWithCoordinates = new();
		foreach (var color in firstOccurrence.Where(c => mostFrequentColors.Contains(c.Key))) {
			quantizedColorsWithCoordinates.Add(new Tuple<Color, Tuple<int, int>>(color.Key, color.Value));
		}

		return quantizedColorsWithCoordinates;
	}

	public static List<Tuple<Color, Tuple<int, int>>> QuantizeColorsUniform(List<Color> colors, int numColors, int width) {
		// Calculate the bin size for each color channel
		int step = 256 / numColors;

		List<Color> quantizedColors = new();
		Dictionary<Color, Tuple<int, int>> firstOccurrence = new();

		for (int i = 0; i < colors.Count; i++) {
			Color color = colors[i];

			// Quantize each channel (R, G, B)
			int r = (color.R / step) * step;
			int g = (color.G / step) * step;
			int b = (color.B / step) * step;

			// Create the quantized color from RGB values
			Color quantizedColor = new(r, g, b);  // Microsoft.Xna.Framework.Color constructor

			// Store the first occurrence of the quantized color
			if (!firstOccurrence.ContainsKey(quantizedColor)) {
				int x = i % width; // X-coordinate in the image
				int y = i / width; // Y-coordinate in the image
				firstOccurrence[quantizedColor] = new Tuple<int, int>(x, y);
			}

			// Only store unique quantized colors
			if (!quantizedColors.Contains(quantizedColor)) {
				quantizedColors.Add(quantizedColor);
			}
		}

		// Reduce to numColors distinct colors
		quantizedColors = quantizedColors.Take(numColors).ToList();

		// Create the final list of quantized colors with their first occurrences
		List<Tuple<Color, Tuple<int, int>>> quantizedColorsWithCoordinates = new();
		foreach (var color in firstOccurrence.Where(c => quantizedColors.Contains(c.Key))) {
			quantizedColorsWithCoordinates.Add(new Tuple<Color, Tuple<int, int>>(color.Key, color.Value));
		}

		return quantizedColorsWithCoordinates;
	}

	public static List<Tuple<Color, Tuple<int, int>>> QuantizeColorsMedianCut(List<Color> colors, int numColors, int width) {

		// Apply the Median Cut Algorithm to reduce colors
		var quantizedColors = MedianCut(colors, numColors);

		// Track the first occurrence of each quantized color
		List<Tuple<Color, Tuple<int, int>>> quantizedColorsWithCoordinates = new();
		Dictionary<Color, Tuple<int, int>> firstOccurrence = new();

		for (int i = 0; i < colors.Count; i++) {
			Color color = colors[i];
			Tuple<int, int, int> colorTuple = Tuple.Create((int)color.R, (int)color.G, (int)color.B);

			// Find the closest quantized color
			var closestQuantizedColor = quantizedColors
				.OrderBy(c => GetSquaredDistance(c.R, c.G, c.B, color.R, color.G, color.B))
				.First();

			Color quantizedColor = new(closestQuantizedColor.R, closestQuantizedColor.G, closestQuantizedColor.B);

			// Track the first occurrence of each quantized color
			if (!firstOccurrence.ContainsKey(quantizedColor)) {
				int x = i % width; // X-coordinate in the image
				int y = i / width; // Y-coordinate in the image
				firstOccurrence[quantizedColor] = new Tuple<int, int>(x, y);
			}
		}

		// Create the final list of quantized colors with their first occurrences
		foreach (var color in firstOccurrence) {
			quantizedColorsWithCoordinates.Add(new Tuple<Color, Tuple<int, int>>(color.Key, color.Value));
		}

		return quantizedColorsWithCoordinates;
	}

	// Median Cut Algorithm to reduce colors
	private static List<Color> MedianCut(List<Color> colors, int numColors) {
		// Create a list of colors sorted by RGB channels
		List<Color> sortedColors = colors.OrderBy(c => c.R).ThenBy(c => c.G).ThenBy(c => c.B).ToList();

		List<List<Color>> colorRegions = new() {
			sortedColors
		};

		while (colorRegions.Count < numColors) {
			// Split the largest region by median in the dimension with the largest range
			List<List<Color>> newRegions = new();

			foreach (var region in colorRegions) {
				if (newRegions.Count < numColors) {
					var (splitRegion1, splitRegion2) = SplitRegionByMedian(region);

					// Handle case where the split regions are identical (e.g., pure black)
					if (splitRegion1.Count == 0 || splitRegion2.Count == 0) {
						newRegions.Add(region);
						continue; // Skip the invalid split
					}

					newRegions.Add(splitRegion1);
					newRegions.Add(splitRegion2);
				}
			}

			colorRegions = newRegions;

			// Early exit if we reach enough colors
			if (colorRegions.Count >= numColors)
				break;
		}

		// Ensure exactly numColors regions are present (handle edge case where we exceed numColors)
		if (colorRegions.Count > numColors) {
			colorRegions = colorRegions.Take(numColors).ToList();
		}

		// Calculate the average color for each region
		List<Color> quantizedColors = colorRegions.Select(region =>
			CalculateCentroid(region)).ToList();

		return quantizedColors;
	}

	// Split a region by median based on the largest range in RGB channels
	private static Tuple<List<Color>, List<Color>> SplitRegionByMedian(List<Color> region) {
		// Find the largest color range (R, G, or B)
		int rangeR = region.Max(c => c.R) - region.Min(c => c.R);
		int rangeG = region.Max(c => c.G) - region.Min(c => c.G);
		int rangeB = region.Max(c => c.B) - region.Min(c => c.B);

		int dimension = rangeR > rangeG ? (rangeR > rangeB ? 0 : 2) : (rangeG > rangeB ? 1 : 2);

		// Sort the region by the chosen dimension
		var sortedRegion = dimension switch {
			0 => region.OrderBy(c => c.R).ToList(),
			1 => region.OrderBy(c => c.G).ToList(),
			_ => region.OrderBy(c => c.B).ToList(),
		};

		// Split the region by median
		int medianIndex = sortedRegion.Count / 2;
		var region1 = sortedRegion.Take(medianIndex).ToList();
		var region2 = sortedRegion.Skip(medianIndex).ToList();

		// Special handling: if regions are identical, don't split
		if (AreRegionsIdentical(region1, region2)) {
			return Tuple.Create(region1, new List<Color>());
		}

		return Tuple.Create(region1, region2);
	}
	private static bool AreRegionsIdentical(List<Color> region1, List<Color> region2) {
		// Check if the two regions are nearly identical (e.g., both pure black)
		return region1.All(c => c.Equals(region2.First())) || region2.All(c => c.Equals(region1.First()));
	}

	// Optimized to use squared distance, avoiding Math.Sqrt
	private static double GetSquaredDistance(int r1, int g1, int b1, int r2, int g2, int b2) {
		return Math.Pow(r1 - r2, 2) + Math.Pow(g1 - g2, 2) + Math.Pow(b1 - b2, 2);
	}

	// Optimized centroid calculation using direct summing
	private static Color CalculateCentroid(List<Color> colors) {
		int count = colors.Count;
		int r = colors.Sum(c => c.R) / count;
		int g = colors.Sum(c => c.G) / count;
		int b = colors.Sum(c => c.B) / count;
		return new Color(r, g, b);
	}

	//KMeans-based quantization function
	//TODO: this funtion is real slow, fix that 
	public static List<Tuple<Color, Tuple<int, int>>> QuantizeColorsKMeans(List<Color> colors, int numColors, int width) {
		//Kmeans does a bunch of averaging, which will often produce multiple palette colors that Ganesha's 'must be a multiple of 8' will just round into the same color.
		//so just divide all colors by 8 before sending them in, then bump them back up on the way out
		List<Color> colorList = colors.Select(c =>
		new Color(((int)c.R) / 8, ((int)c.G) / 8, ((int)c.B) / 8)).ToList();

		//Apply KMeans clustering to get the centroids (the quantized colors)
		var centroids = KMeans(colorList, numColors);

		//Find the closest centroid for each color and track the first occurrence
		List<Tuple<Color, Tuple<int, int>>> quantizedColorsWithCoordinates = new();
		Dictionary<Color, Tuple<int, int>> firstOccurrence = new();

		for (int i = 0; i < colorList.Count; i++) {
			Color color = colorList[i];
			Tuple<int, int, int> colorTuple = Tuple.Create((int)color.R, (int)color.G, (int)color.B);

			//Find the closest centroid for the current color
			var closestCentroid = centroids
				.OrderBy(c => GetSquaredDistance(c.R, c.G, c.B, color.R, color.G, color.B))
				.First();

			Color quantizedColor = new(closestCentroid.R, closestCentroid.G, closestCentroid.B);

			//Track the first occurrence of each color
			if (!firstOccurrence.ContainsKey(quantizedColor)) {
				int x = i % width; //X-coordinate in the image
				int y = i / width; //Y-coordinate in the image
				firstOccurrence[quantizedColor] = new Tuple<int, int>(x, y);
			}
		}

		//Create the final list of quantized colors with their first occurrences
		foreach (var color in firstOccurrence) {
			quantizedColorsWithCoordinates.Add(new Tuple<Color, Tuple<int, int>>(color.Key, color.Value));
		}

		List<Tuple<Color, Tuple<int, int>>> result = quantizedColorsWithCoordinates.Select(c =>
		new Tuple<Color, Tuple<int, int>>(new Color((int)c.Item1.R * 8, (int)c.Item1.G * 8, (int)c.Item1.B * 8), c.Item2)).ToList();


		//List<Color> colorList = originalPixels.Select(c =>
		//new Color(((int)c.R + 3) / 8 * 8, ((int)c.G + 3) / 8 * 8, ((int)c.B + 3) / 8 * 8)).ToList();

		return result;
	}

	//KMeans algorithm to find the centroids
	private static List<Color> KMeans(List<Color> colors, int k) {
		//Initialize random centroids from the colors
		Random rand = new();
		var centroids = colors.OrderBy(x => rand.Next()).Take(k).ToList();

		bool centroidsChanged;
		List<int> labels = new(new int[colors.Count]);

		do {
			centroidsChanged = false;

			//Assign each color to the nearest centroid
			for (int i = 0; i < colors.Count; i++) {
				int closestCentroidIndex = FindClosestCentroid(colors[i], centroids);
				if (labels[i] != closestCentroidIndex) {
					labels[i] = closestCentroidIndex;
					centroidsChanged = true;
				}
			}

			//Recalculate centroids
			for (int i = 0; i < k; i++) {
				var assignedColors = colors.Where((c, index) => labels[index] == i).ToList();
				if (assignedColors.Count > 0) {
					var newCentroid = CalculateCentroid(assignedColors);
					centroids[i] = newCentroid;
				}
			}
		} while (centroidsChanged);

		return centroids;
	}

	//Find the closest centroid to a given color
	private static int FindClosestCentroid(Color color, List<Color> centroids) {
		return centroids
			.Select((centroid, index) => new { Centroid = centroid, Index = index, Distance = GetDistance(centroid.R, centroid.G, centroid.B, color.R, color.G, color.B) })
			.OrderBy(x => x.Distance)
			.First().Index;
	}

	//Calculate the Euclidean distance between two RGB colors
	private static double GetDistance(int r1, int g1, int b1, int r2, int g2, int b2) {
		return Math.Sqrt(Math.Pow(r1 - r2, 2) + Math.Pow(g1 - g2, 2) + Math.Pow(b1 - b2, 2));
	}


}
