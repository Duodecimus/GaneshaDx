using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GaneshaDx.Common;
using GaneshaDx.Environment;
using GaneshaDx.Rendering;
using GaneshaDx.Resources.ContentDataTypes.Palettes;
using GaneshaDx.Resources.ContentDataTypes.Polygons;
using GaneshaDx.Resources.GnsData;
using GaneshaDx.Resources.ResourceContent;
using GaneshaDx.UserInterface;
using GaneshaDx.UserInterface.GuiDefinitions;
using GaneshaDx.UserInterface.GuiForms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpGLTF.Schema2;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace GaneshaDx.Resources;

public static class MapData {
	public static bool MapIsLoaded;
	public static double TimeSinceLastSave;

	private static string _mapFolder;
	public static string MapName { get; private set; }

	public static Gns Gns;
	public static List<MapResource> AllResources;
	public static List<MapResource> MeshResources;
	public static List<MapResource> TextureResources;

	public static void ReloadCurrentMap() {
		LoadMapDataFromFullPath(_mapFolder + "\\" + MapName + ".gns");
	}

	public static void LoadMapDataFromFullPath(string gnsPath) {
		List<string> pathSegments = gnsPath.Split('\\').ToList();
		string fileName = pathSegments.Last();
		List<string> fileSegments = fileName.Split('.').ToList();

		if (fileSegments.Last().ToLower() != "gns") {
			return;
		}

		fileSegments.RemoveAt(fileSegments.Count - 1);
		string mapName = string.Join(".", fileSegments);

		pathSegments.RemoveAt(pathSegments.Count - 1);
		string folder = string.Join("\\", pathSegments);

		Configuration.Properties.LoadFolder = folder;
		Configuration.SaveConfiguration();

		LoadMapDataFromFiles(folder, mapName);
	}

	public static void LoadMapDataFromFiles(string mapFolder, string mapName) {
		MapIsLoaded = false;
		AllResources = new List<MapResource>();
		MeshResources = new List<MapResource>();
		TextureResources = new List<MapResource>();
		MapName = mapName;
		_mapFolder = mapFolder;

		List<byte> gnsData = File.ReadAllBytes(_mapFolder + "\\" + MapName + ".GNS").ToList();
		Gns = new Gns(gnsData);

		ProcessAllResources();
		SetResourceFileData(_mapFolder + "\\" + MapName);

		if (AllResourcesLoaded()) {
			Stage.Window.Title = "GaneshaDx - " + MapName;
			MapIsLoaded = true;
			TimeSinceLastSave = Stage.GameTime.TotalGameTime.TotalSeconds;
			CurrentMapState.SetState(MapArrangementState.Primary, MapTime.Day, MapWeather.None);
			ResetEditorState();
			MeshAnimationController.PlayAnimations();
		}
	}

	private static void ResetEditorState() {
		Selection.SelectedPolygons.Clear();
		Selection.SelectedTerrainTiles.Clear();
		Gui.SelectedTab = RightPanelTab.Map;
		Gui.ShowMeshAnimationsWindow = false;
		Gui.ShowDebugAnimatedMeshWindow = false;
		GuiPanelTerrain.ResizeTerrainMode = false;
		GuiPanelMeshSelector.SelectedMesh = MeshType.PrimaryMesh;
		StageCamera.Reset();
	}

	private static void ProcessAllResources() {
		for (int index = 0; index < Gns.RawData.Count; index++) {
			List<byte> resourceRawData = new();

			int lengthOfResource = 20;

			while (index + lengthOfResource > Gns.RawData.Count) {
				lengthOfResource--;
			}

			for (int resourceIndex = 0; resourceIndex < lengthOfResource; resourceIndex++) {
				resourceRawData.Add(Gns.RawData[index + resourceIndex]);
			}

			MapResource mapResource = new(resourceRawData);

			if (mapResource.IsMesh || mapResource.IsTexture) {
				AllResources.Add(mapResource);
			}

			index += lengthOfResource - 1;
		}

		AllResources = AllResources.OrderBy(resource => resource.FileSector).ToList();

		foreach (MapResource resource in AllResources) {
			if (resource.IsMesh) {
				MeshResources.Add(resource);
			}

			if (resource.IsTexture) {
				TextureResources.Add(resource);
			}
		}
	}

	private static void SetResourceFileData(string mapRoot) {
		List<int> fileSectors = new();
		List<List<byte>> resourceFileData = new();
		List<int> resourceXFiles = new();

		foreach (MapResource resource in AllResources) {
			if (!fileSectors.Contains(resource.FileSector)) {
				fileSectors.Add(resource.FileSector);
			} else {
				OverlayConsole.AddMessage("Some Mismatch happened.. investigate this");
			}
		}

		int xFileIndex = 0;

		while (resourceFileData.Count < fileSectors.Count) {
			string xFileName = mapRoot + "." + xFileIndex;

			if (File.Exists(xFileName)) {
				resourceFileData.Add(File.ReadAllBytes(xFileName).ToList());
				resourceXFiles.Add(xFileIndex);
			}

			xFileIndex++;

			if (xFileIndex > 200) {
				Console.WriteLine("An XFile with more than 200??");
				break;
			}
		}

		if (resourceXFiles.Count == AllResources.Count) {
			for (int index = 0; index < AllResources.Count; index++) {
				AllResources[index].SetResourceData(resourceXFiles[index], resourceFileData[index]);
			}
		}
	}

	private static bool AllResourcesLoaded() {
		foreach (MapResource x in AllResources) {
			if (x.XFile == -1) {
				OverlayConsole.AddMessage("Could not locate all Resource files");
				return false;
			}
		}

		return true;
	}

	public static void ExportGlb(string filePath) {
		GlbExporter.Export(filePath);
		OverlayConsole.AddMessage("Map Exported as " + filePath);
	}

	public static void ImportGlb(string filePath) {
		Stopwatch stopwatch = Stopwatch.StartNew();
		var importedModel = SharpGLTF.Schema2.ModelRoot.Load(filePath);
		foreach (MeshType meshtype in Enum.GetValues(typeof(MeshType))) {
			CurrentMapState.DeleteAllPolygons(meshtype); //TODO: ask the user first
		}

		const float ScaleFactor = 1; //TODO: ask the user on import how many terrain tiles wide the import is, then resize during import

		if (importedModel.LogicalImages.Count > 0) {
			List<Tuple<Color, Tuple<int, int>>> paletteSourcePixels = new();
			Palette importedPalette;
			for (int i = 0; i < importedModel.LogicalImages.Count; i++) {
				var texture = importedModel.LogicalImages[i];
				Stream stream = texture.Content.Open();
				Texture2D importedTexture = Texture2D.FromStream(Stage.GraphicsDevice, stream);
				stream.Dispose();
				//TODO: Ask the user if they want to import the texture or palettes
				if (i == 0) { //Assume the first texture found is the main texture, import it and create palette key
					var QuantizedTexture = Quantizer.QuantizeToPaletteAndListPixels(importedTexture, out importedPalette, out paletteSourcePixels);
					ImportPalette(importedPalette, i, "main");
					ImportTexture(QuantizedTexture, true);
				}
				else { //Further textures assumed to be alt colors of the main texture. Use the palette key to quickly grab another palette
					   //TODO: odds are one of of these is a normal map. Do something with it
					importedPalette = GetPaletteWithSourcePixels(importedTexture, paletteSourcePixels);
					ImportPalette(importedPalette, i, "main");
				}

				//stream = File.Create(filePath+".ImportedTexture"+i+".png");
				//importedTexture.SaveAsPng(stream, importedTexture.Width, importedTexture.Height);
				//stream.Dispose();
			}
		}

		if (importedModel.LogicalMeshes.Count > 0) { //TODO: GLB files can't have quads, would need to add new import/export for obj or something
			var TriCount = 0;
			for (int i = 0; i < importedModel.LogicalMeshes.Count; i++) {
				foreach (var primitive in importedModel.LogicalMeshes[i].Primitives) {
					var triangles = primitive.EvaluateTriangles().ToList();
					foreach (var (A, B, C, Material) in triangles) {
						TriCount++;
						List<Vertex> verticesToBuild = new() {
							new Vertex(B.GetGeometry().GetPosition()*ScaleFactor, Color.Red, true),
							new Vertex(A.GetGeometry().GetPosition()*ScaleFactor, Color.Green, true),
							new Vertex(C.GetGeometry().GetPosition()*ScaleFactor, Color.Blue, true),
						};
						var offset = 0;
						var yTest = (B.GetMaterial().GetTexCoord(0).Y * 1024);
						offset = yTest switch { //determine which page the tri belongs on
							> 256 and <= 512 => 256,
							> 512 and <= 768 => 512,
							> 768 and <= 1024 => 768,
							//<= 256
							_ => 0,
						};
						List<Vector2> uvs = new() {//TODO: FFT probably breaks if a tri/quad crosses multiple pages, add a safety check to rebuild the tri safely
							new Vector2(MathF.Round(B.GetMaterial().GetTexCoord(0).X*256),
										MathF.Round(B.GetMaterial().GetTexCoord(0).Y*1024 - offset)),
							new Vector2(MathF.Round(A.GetMaterial().GetTexCoord(0).X*256),
										MathF.Round(A.GetMaterial().GetTexCoord(0).Y*1024 - offset)),
							new Vector2(MathF.Round(C.GetMaterial().GetTexCoord(0).X*256),
										MathF.Round(C.GetMaterial().GetTexCoord(0).Y*1024 - offset))
						};
						//TODO: Scan all imported points and get an idea of how big the model will be.
						//		Offset model to have 0,0,0 to the bottom left of the model.
						//		Implement function to upscale the model, ask the user how many terrain tiles wide it should be
						Polygon poly = CurrentMapState.CreatePolygon(verticesToBuild, uvs, MeshType.PrimaryMesh);
						poly.TexturePage = offset switch {
							256 => 1,
							512 => 2,
							768 => 3,
							//0
							_ => 0,
						};
						poly.PaletteId = Material.LogicalIndex; //Set the tri to be a palette id, this matches the import order of textures 
					}
				}
			}
		}
		stopwatch.Stop();
		OverlayConsole.AddMessage($"GLB imported in {stopwatch.ElapsedMilliseconds} ms");
	}

	//Use the palette key to quickly grab the equivalent colors that were chosen as palette colors in the base image
	public static Palette GetPaletteWithSourcePixels(Texture2D importedTexture, List<Tuple<Color, Tuple<int, int>>> paletteSourcePixels) { 
		var palette = new Palette();
		Color[] colors = new Color[importedTexture.Width * importedTexture.Height];
		importedTexture.GetData(colors);
		foreach(Tuple<Color, Tuple<int, int>> entry in paletteSourcePixels) {
			var x = entry.Item2.Item1;
			var y = entry.Item2.Item2;

			var originalPixel = colors[y * importedTexture.Width + x];
			if(originalPixel.A == 0) {
				palette.Colors.Add(new PaletteColor(0, 0, 0, true));
			}
			else {
				palette.Colors.Add(new PaletteColor((originalPixel.R+3)/8, (originalPixel.G+3)/8, (originalPixel.B+3)/8, false));
			}
		}
		return palette;
	}

	//Resizes a Texture2D to a new size. GaneshaDX operates on 256x1024 textures

	public static Texture2D ResizeTexture(Texture2D texture2D,int targetX,int targetY) {

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
	
	public static void ImportTexture(string filePath) {
		Texture2D importedTexture = Texture2D.FromFile(Stage.GraphicsDevice, filePath);
		ImportTexture(importedTexture, false);
	}

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

		foreach (MapResource textureResource in TextureResources) {
			if (
				textureResource.MapArrangementState == CurrentMapState.StateData.MapArrangementState &&
				textureResource.MapTime == CurrentMapState.StateData.MapTime &&
				textureResource.MapWeather == CurrentMapState.StateData.MapWeather
			) {
				TextureResourceData textureResourceData = (TextureResourceData) textureResource.ResourceData;
				textureResourceData.Texture = resizedTexture;
				CurrentMapState.StateData.Texture = resizedTexture;
				break;
			}
		}
	}

	public static void ExportTexture(string filePath) {
		Stream stream = File.Create(filePath);
		Texture2D texture = CurrentMapState.StateData.Texture;
		texture.SaveAsPng(stream, texture.Width, texture.Height);
		OverlayConsole.AddMessage("Texture Exported as " + filePath);
		stream.Dispose();
	}

	public static void ImportPalette(string file, int paletteId, string paletteType) {
		List<byte> paletteData = File.ReadAllBytes(file).ToList();
		const int totalColors = 16;
		Palette sourcePalette = new();

		for (int colorIndex = 0; colorIndex < totalColors * 3; colorIndex += 3) {
			int red = (int)Math.Floor(paletteData[colorIndex] / 8f);
			int green = (int)Math.Floor(paletteData[colorIndex + 1] / 8f);
			int blue = (int)Math.Floor(paletteData[colorIndex + 2] / 8f);
			PaletteColor color = new(red, green, blue, false);
			sourcePalette.Colors.Add(color);
		}
		ImportPalette(sourcePalette, paletteId, paletteType);
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

	public static void ExportPalette(string filePath, int paletteId, string paletteType) {
		List<byte> actData = new();

		if (paletteId == -1) {
			for (int i = 0; i < 16; i++) {
				actData.Add((byte) (i * 17));
				actData.Add((byte) (i * 17));
				actData.Add((byte) (i * 17));
			}
		} else {
			List<PaletteColor> sourcePalette = paletteType == "main"
				? CurrentMapState.StateData.Palettes[paletteId].Colors
				: CurrentMapState.StateData.PaletteAnimationFrames[paletteId].Colors;

			foreach (PaletteColor color in sourcePalette) {
				actData.Add((byte) (color.Red * 8));
				actData.Add((byte) (color.Green * 8));
				actData.Add((byte) (color.Blue * 8));
			}
		}

		while (actData.Count < 256 * 3) {
			actData.Add(0);
		}

		Stream stream = File.Create(filePath);
		stream.Write(actData.ToArray());
		stream.Dispose();
	}

	public static void ExportUvMap(string filePath) {
		Stream stream = File.Create(filePath);
		Texture2D texture = GuiWindowTextureElement.GetUvMapTexture();
		texture.SaveAsPng(stream, texture.Width, texture.Height);
		OverlayConsole.AddMessage("Uv Map Exported as " + filePath);
		stream.Dispose();
	}

	public static void SaveMap(bool isAutoSave = false) {
		string mapFolder = _mapFolder;
		string backupNotation = "";

		if (isAutoSave) {
			mapFolder += "\\gdx_autosave\\";
			DateTime time = DateTime.Now;
			backupNotation = " (" +
			                 time.Date.Year + "-" +
			                 time.Date.DayOfYear + "--" +
			                 time.Hour + "-" +
			                 time.Minute +
			                 ")";

			if (!Directory.Exists(mapFolder)) {
				Directory.CreateDirectory(mapFolder);
			}
		}

		string mapRoot = mapFolder + "\\" + MapName;

		Stream gnsStream = File.Create(mapRoot + backupNotation + ".GNS");
		gnsStream.Write(Gns.RawData.ToArray());
		gnsStream.Dispose();

		foreach (MapResource textureResource in TextureResources) {
			TextureResourceData data = (TextureResourceData) textureResource.ResourceData;
			data.RebuildRawData();

			Stream stream = File.Create(mapRoot + backupNotation + "." + textureResource.XFile);
			stream.Write(data.RawData.ToArray());
			stream.Dispose();
		}

		foreach (MapResource mehResource in MeshResources) {
			MeshResourceData data = (MeshResourceData) mehResource.ResourceData;
			data.RebuildRawData();

			Stream stream = File.Create(mapRoot + backupNotation + "." + mehResource.XFile);
			stream.Write(data.RawData.ToArray());
			stream.Dispose();
		}

		TimeSinceLastSave = Stage.GameTime.TotalGameTime.TotalSeconds;
		Stage.Window.Title = "GaneshaDx - " + MapName;

		OverlayConsole.AddMessage(isAutoSave ? "Map Backed up to \\gdx_autosave\\" +  MapName + backupNotation : "Map Saved");
	}

	public static void SaveMapAs(string newFolder, string mapName) {
		_mapFolder = newFolder;
		MapName = mapName;
		SaveMap();
	}
}