using GaneshaDx.Environment;
using GaneshaDx.Resources.ContentDataTypes.Polygons;
using GaneshaDx.Resources;
using Microsoft.Xna.Framework.Graphics;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Vector3 = System.Numerics.Vector3;
using Color = Microsoft.Xna.Framework.Color;
using GaneshaDx.Resources.ContentDataTypes.Palettes;
using System.Runtime.Intrinsics;

namespace GaneshaDx.Common {
	internal class GlbImporter {

		public static void Import(string filePath) {
			var importedModel = SharpGLTF.Schema2.ModelRoot.Load(filePath);
			foreach (MeshType meshtype in Enum.GetValues(typeof(MeshType))) {
				CurrentMapState.DeleteAllPolygons(meshtype); //TODO: ask the user first
			}

			var translation = Vector3.Zero;
			var rotationMatrix = Matrix4x4.Identity;
			float scaleFactor = 1;
			var breakLoop = false;

			if (importedModel.LogicalNodes.Count > 0) {
				for (int i = 0; i < importedModel.LogicalNodes.Count; i++) {
					var name = importedModel.LogicalNodes[i].Name;
					if (name != null && name.ToLower() == "terrain_reference") {
						if (importedModel.LogicalNodes[i].Mesh != null) {
							foreach (var primitive in importedModel.LogicalNodes[i].Mesh.Primitives) {
								breakLoop = true;
								var meshPoints = primitive.EvaluatePoints().ToList();
								var meshLines = primitive.EvaluateLines().ToList();
								var meshTriangles = primitive.EvaluateTriangles().ToList();

								HashSet<Vector3> pointsHash = new(); // Use the hash to enforce uniqueness. A quad will have 6 points with 2 dupes. 

								foreach (var (A, Material) in meshPoints) {
									pointsHash.Add(A.GetGeometry().GetPosition());
								}
								foreach (var (A, B, Material) in meshLines) {
									pointsHash.Add(A.GetGeometry().GetPosition());
									pointsHash.Add(B.GetGeometry().GetPosition());
								}
								foreach (var (A, B, C, Material) in meshTriangles) {
									pointsHash.Add(A.GetGeometry().GetPosition());
									pointsHash.Add(B.GetGeometry().GetPosition());
									pointsHash.Add(C.GetGeometry().GetPosition());
								}
								List<Vector3> points = pointsHash.ToList();

								// importedModel.LogicalNodes[i].LocalTransform is a way to tell an internal object to render somewhere else in the model.
								// Like if you wanted to put a bunch of boxes in, but didn't want to store all of those identical points
								var terrainLocalMatrix = importedModel.LogicalNodes[i].LocalTransform.GetDecomposed().Matrix;

								for (int j = 0; j < points.Count; j++) {
									points[j] = Vector3.Transform(points[j], terrainLocalMatrix);
								}

								if (points.Count != 2 && points.Count != 3 && points.Count != 4) {
									OverlayConsole.AddMessage($"Failed to apply terrain reference, requires a line, a tri, or a quad");
									break;
								}
								(translation, rotationMatrix, scaleFactor) = TerrainReference.GetTerrainReferenceTransformation(points);
								break;
							}
						}
					}
					if (breakLoop) { break; }
				}
			}

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
						TextureImport.ImportPalette(importedPalette, i, "main");
						TextureImport.ImportTexture(QuantizedTexture, true);
					}
					else { //Further textures assumed to be alt colors of the main texture. Use the palette key to quickly grab another palette
						   //TODO: odds are one of of these is a normal map. Do something with it
						importedPalette = TextureImport.GetPaletteWithSourcePixels(importedTexture, paletteSourcePixels);
						TextureImport.ImportPalette(importedPalette, i, "main");
					}

					//stream = File.Create(filePath+".ImportedTexture"+i+".png");
					//importedTexture.SaveAsPng(stream, importedTexture.Width, importedTexture.Height);
					//stream.Dispose();
				}
			}

			if (importedModel.LogicalNodes.Count > 0) { //TODO: GLB files can't have quads, would need to add new import/export for obj or something
				var TriCount = 0;
				for (int i = 0; i < importedModel.LogicalNodes.Count; i++) {
					var name = importedModel.LogicalNodes[i].Name;
					if (name != null && name.ToLower() == "terrain_reference") { continue; } //don't render the terrain reference shape
					if (importedModel.LogicalNodes[i].Mesh != null) {
						foreach (var primitive in importedModel.LogicalNodes[i].Mesh.Primitives) {
							var triangles = primitive.EvaluateTriangles().ToList();
							foreach (var (A, B, C, Material) in triangles) {
								TriCount++;
								//importedModel.LogicalNodes[i].LocalTransform is a way to tell a glb's internal object to render somewhere else in the model.
								//TODO: handle objects that don't contain points, but reference another object in the imported glb
								var localMatrix = importedModel.LogicalNodes[i].LocalTransform.GetDecomposed().Matrix;
								//Apply Terrain Reference and local transformations
								var vector1 = Vector3.Transform(Vector3.Transform(B.GetGeometry().GetPosition() + translation, rotationMatrix) * scaleFactor, localMatrix);
								var vector2 = Vector3.Transform(Vector3.Transform(A.GetGeometry().GetPosition() + translation, rotationMatrix) * scaleFactor, localMatrix);
								var vector3 = Vector3.Transform(Vector3.Transform(C.GetGeometry().GetPosition() + translation, rotationMatrix) * scaleFactor, localMatrix);
								List<Vertex> verticesToBuild = new() {
									//round all vertex at final step, because FFT doesn't deal in floats
									new Vertex(new (MathF.Round(vector1.X), MathF.Round(vector1.Y), MathF.Round(vector1.Z)), Color.Red, true),
									new Vertex(new (MathF.Round(vector2.X), MathF.Round(vector2.Y), MathF.Round(vector2.Z)), Color.Green, true),
									new Vertex(new (MathF.Round(vector3.X), MathF.Round(vector3.Y), MathF.Round(vector3.Z)), Color.Blue, true),
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
								List<Microsoft.Xna.Framework.Vector2> uvs = new() {//TODO: FFT probably breaks if a tri/quad crosses multiple pages, add a safety check to rebuild the tri safely
									new Microsoft.Xna.Framework.Vector2(MathF.Round(B.GetMaterial().GetTexCoord(0).X*256),
																		MathF.Round(B.GetMaterial().GetTexCoord(0).Y*1024 - offset)),
									new Microsoft.Xna.Framework.Vector2(MathF.Round(A.GetMaterial().GetTexCoord(0).X*256),
																		MathF.Round(A.GetMaterial().GetTexCoord(0).Y*1024 - offset)),
									new Microsoft.Xna.Framework.Vector2(MathF.Round(C.GetMaterial().GetTexCoord(0).X*256),
																		MathF.Round(C.GetMaterial().GetTexCoord(0).Y*1024 - offset))
								};
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
			}
		}

	}
}
