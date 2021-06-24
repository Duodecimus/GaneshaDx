﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using GaneshaDx.Common;
using GaneshaDx.Resources;
using GaneshaDx.Resources.ContentDataTypes.Terrains;
using GaneshaDx.UserInterface.GuiDefinitions;
using ImGuiNET;

namespace GaneshaDx.UserInterface.GuiForms {
	public static class GuiPanelTerrain {
		private static bool _resizeTerrainMode;
		private static int _resizeXSize;
		private static int _resizeZSize;

		public static void Render() {
			if (Selection.SelectedTerrainTiles.Count > 0 && !_resizeTerrainMode) {
				RenderTerrainTileProperties();
			}

			RenderTerrainRenderOptions();
			RenderResizeTerrainPanel();
		}

		private static void RenderTerrainTileProperties() {
			TerrainTile terrainTile = Selection.SelectedTerrainTiles[0];
			TerrainTile companionTile = CurrentMapState.StateData.Terrain.GetCompanionTerrainTile(terrainTile);

			List<TerrainTile> tiles = new List<TerrainTile>();
			if (terrainTile.Level == 0) {
				tiles.Add(terrainTile);
				tiles.Add(companionTile);
			} else {
				tiles.Add(companionTile);
				tiles.Add(terrainTile);
			}

			List<string> slopeTypes = Enum.GetNames(typeof(TerrainSlopeType)).ToList();
			for (int slopeTypeIndex = 0; slopeTypeIndex < slopeTypes.Count; slopeTypeIndex++) {
				slopeTypes[slopeTypeIndex] = Regex.Replace(slopeTypes[slopeTypeIndex], "(\\B[A-Z])", " $1");
			}

			List<string> surfaceTypes = Enum.GetNames(typeof(TerrainSurfaceType)).ToList();
			for (int surfaceTypeIndex = 0; surfaceTypeIndex < surfaceTypes.Count; surfaceTypeIndex++) {
				surfaceTypes[surfaceTypeIndex] =
					Regex.Replace(surfaceTypes[surfaceTypeIndex], "(\\B[A-Z])", " $1");
			}

			GuiStyle.SetElementStyle(ElementStyle.Header);
			ImGui.PushStyleColor(ImGuiCol.Text, GuiStyle.ColorPalette[ColorName.Lightest]);

			GuiStyle.AddSpace(5);
			string positionText = terrainTile.IndexX + "x     " + terrainTile.IndexZ + "z";
			float positionTextWidth = ImGui.CalcTextSize(positionText).X;
			ImGui.SetCursorPosX((GuiStyle.RightPanelWidth - positionTextWidth) * 0.5f);
			ImGui.Text(positionText);
			GuiStyle.AddSpace(5);

			GuiStyle.SetNewUiToDefaultStyle();

			for (int tileIndex = 0; tileIndex < 2; tileIndex++) {
				GuiStyle.SetNewUiToDefaultStyle();
				GuiStyle.SetElementStyle(ElementStyle.Header);

				if (ImGui.CollapsingHeader("Level " + tileIndex + " Properties", ImGuiTreeNodeFlags.DefaultOpen)) {
					GuiStyle.SetNewUiToDefaultStyle();
					ImGui.Indent();

					ImGui.Columns(2, "TerrainPropertiesColumns", false);
					ImGui.SetColumnWidth(0, GuiStyle.LabelWidth);
					ImGui.SetColumnWidth(1, GuiStyle.WidgetWidth + 10);

					GuiStyle.AddSpace();
					ImGui.Text("Height");
					ImGui.NextColumn();

					GuiStyle.AddSpace();
					ImGui.SetNextItemWidth(GuiStyle.WidgetWidth);
					int beforeHeight = tiles[tileIndex].Height;
					ImGui.InputInt("Height" + tileIndex, ref tiles[tileIndex].Height, 1);
					tiles[tileIndex].Height = Utilities.Clamp(tiles[tileIndex].Height, 0, 63);

					if (beforeHeight != tiles[tileIndex].Height) {
						int difference = tiles[tileIndex].Height - beforeHeight;
						foreach (TerrainTile otherTerrainTile in Selection.SelectedTerrainTiles) {
							if (otherTerrainTile != tiles[tileIndex] &&
							    otherTerrainTile.Level == tiles[tileIndex].Level
							) {
								otherTerrainTile.Height += difference;
								otherTerrainTile.Height = Utilities.Clamp(otherTerrainTile.Height, 0, 63);
							}
						}
					}

					ImGui.NextColumn();

					ImGui.Text("Depth");
					ImGui.NextColumn();

					ImGui.SetNextItemWidth(GuiStyle.WidgetWidth);
					int beforeDepth = tiles[tileIndex].Depth;
					ImGui.InputInt("Depth" + tileIndex, ref tiles[tileIndex].Depth, 1);
					tiles[tileIndex].Depth = Utilities.Clamp(tiles[tileIndex].Depth, 0, 63);

					if (beforeDepth != tiles[tileIndex].Depth) {
						foreach (TerrainTile otherTerrainTile in Selection.SelectedTerrainTiles) {
							if (otherTerrainTile != tiles[tileIndex] &&
							    otherTerrainTile.Level == tiles[tileIndex].Level
							) {
								otherTerrainTile.Depth = tiles[tileIndex].Depth;
							}
						}
					}

					ImGui.NextColumn();

					GuiStyle.AddSpace();
					ImGui.Text("Slope Type");
					ImGui.NextColumn();

					GuiStyle.AddSpace();
					int comboSelection = (int) tiles[tileIndex].SlopeType;
					TerrainSlopeType beforeSlopeType = tiles[tileIndex].SlopeType;
					ImGui.PushItemWidth(GuiStyle.WidgetWidth);
					ImGui.Combo("SlopeType" + tileIndex, ref comboSelection, slopeTypes.ToArray(), slopeTypes.Count);
					ImGui.PopItemWidth();
					tiles[tileIndex].SlopeType = (TerrainSlopeType) comboSelection;

					if (beforeSlopeType != tiles[tileIndex].SlopeType) {
						foreach (TerrainTile otherTerrainTile in Selection.SelectedTerrainTiles) {
							if (otherTerrainTile != tiles[tileIndex] &&
							    otherTerrainTile.Level == tiles[tileIndex].Level
							) {
								otherTerrainTile.SlopeType = tiles[tileIndex].SlopeType;
							}
						}
					}

					ImGui.NextColumn();

					ImGui.Text("Slope Height");
					ImGui.NextColumn();

					ImGui.SetNextItemWidth(GuiStyle.WidgetWidth);
					int beforeSlopeHeight = tiles[tileIndex].SlopeHeight;
					ImGui.InputInt("SlopeHeight" + tileIndex, ref tiles[tileIndex].SlopeHeight, 1);
					tiles[tileIndex].SlopeHeight = Utilities.Min(tiles[tileIndex].SlopeHeight, 0);
					if (beforeSlopeHeight != tiles[tileIndex].SlopeHeight) {
						foreach (TerrainTile otherTerrainTile in Selection.SelectedTerrainTiles) {
							if (otherTerrainTile != tiles[tileIndex] &&
							    otherTerrainTile.Level == tiles[tileIndex].Level
							) {
								otherTerrainTile.SlopeHeight = tiles[tileIndex].SlopeHeight;
							}
						}
					}

					ImGui.NextColumn();

					GuiStyle.AddSpace();
					ImGui.Text("Surface Type");
					ImGui.NextColumn();

					GuiStyle.AddSpace();
					comboSelection = (int) tiles[tileIndex].SurfaceType;
					TerrainSurfaceType beforeSurfaceType = tiles[tileIndex].SurfaceType;
					ImGui.PushItemWidth(GuiStyle.WidgetWidth);

					ImGui.Combo(
						"SurfaceType" + tileIndex,
						ref comboSelection,
						surfaceTypes.ToArray(),
						surfaceTypes.Count
					);
					ImGui.PopItemWidth();
					tiles[tileIndex].SurfaceType = (TerrainSurfaceType) comboSelection;
					if (beforeSurfaceType != tiles[tileIndex].SurfaceType) {
						foreach (TerrainTile otherTerrainTile in Selection.SelectedTerrainTiles) {
							if (otherTerrainTile != tiles[tileIndex] &&
							    otherTerrainTile.Level == tiles[tileIndex].Level
							) {
								otherTerrainTile.SurfaceType = tiles[tileIndex].SurfaceType;
							}
						}
					}

					ImGui.NextColumn();

					GuiStyle.AddSpace();
					ImGui.Text("Impassable");
					ImGui.NextColumn();

					GuiStyle.AddSpace();
					ImGui.PushItemWidth(GuiStyle.CheckBoxWidth);
					bool beforeImpassable = tiles[tileIndex].Impassable;
					ImGui.Checkbox("##impassable" + tileIndex, ref tiles[tileIndex].Impassable);
					if (beforeImpassable != tiles[tileIndex].Impassable) {
						foreach (TerrainTile otherTerrainTile in Selection.SelectedTerrainTiles) {
							if (otherTerrainTile != tiles[tileIndex] &&
							    otherTerrainTile.Level == tiles[tileIndex].Level
							) {
								otherTerrainTile.Impassable = tiles[tileIndex].Impassable;
							}
						}
					}

					ImGui.NextColumn();

					ImGui.Text("Unselectable");
					ImGui.NextColumn();

					ImGui.PushItemWidth(GuiStyle.CheckBoxWidth);
					bool beforeUnselectable = tiles[tileIndex].Unselectable;
					ImGui.Checkbox("###unselectable" + tileIndex, ref tiles[tileIndex].Unselectable);
					if (beforeUnselectable != tiles[tileIndex].Unselectable) {
						foreach (TerrainTile otherTerrainTile in Selection.SelectedTerrainTiles) {
							if (otherTerrainTile != tiles[tileIndex] &&
							    otherTerrainTile.Level == tiles[tileIndex].Level
							) {
								otherTerrainTile.Unselectable = tiles[tileIndex].Unselectable;
							}
						}
					}

					ImGui.NextColumn();

					ImGui.Columns(1);
					ImGui.Unindent();
					GuiStyle.AddSpace();
				}
			}
		}

		private static void RenderTerrainRenderOptions() {
			GuiStyle.SetNewUiToDefaultStyle();
			GuiStyle.SetElementStyle(ElementStyle.Header);

			if (ImGui.CollapsingHeader("Render Options", ImGuiTreeNodeFlags.DefaultOpen)) {
				GuiStyle.SetNewUiToDefaultStyle();
				ImGui.Indent();

				ImGui.Columns(2, "TerrainOptionsColumns", false);
				ImGui.SetColumnWidth(0, GuiStyle.LabelWidth);
				ImGui.SetColumnWidth(1, GuiStyle.WidgetWidth + 10);

				ImGui.Text("Polygon Transparency");
				ImGui.NextColumn();

				ImGui.PushItemWidth(GuiStyle.WidgetWidth);
				int beforeTransparency = Configuration.Properties.TerrainTransparencyForPolygonEditing;
				ImGui.SliderInt(
					"###polyTransparency",
					ref Configuration.Properties.TerrainTransparencyForPolygonEditing,
					0, 100
				);

				if (beforeTransparency != Configuration.Properties.TerrainTransparencyForPolygonEditing) {
					Configuration.SaveConfiguration();
				}

				ImGui.NextColumn();

				ImGui.Columns(1);
				ImGui.Unindent();
				GuiStyle.AddSpace();
			}
		}

		private static void RenderResizeTerrainPanel() {
			GuiStyle.SetNewUiToDefaultStyle();
			GuiStyle.SetElementStyle(ElementStyle.Header);

			if (ImGui.CollapsingHeader("Resize Terrain", ImGuiTreeNodeFlags.DefaultOpen)) {
				GuiStyle.SetNewUiToDefaultStyle();
				ImGui.Indent();

				if (!_resizeTerrainMode) {
					if (ImGui.Button("Resize Terrain###ToResizeTerrainMode")) {
						_resizeTerrainMode = true;
						_resizeXSize = CurrentMapState.StateData.Terrain.SizeX;
						_resizeZSize = CurrentMapState.StateData.Terrain.SizeZ;
					}
				} else {
					ImGui.Columns(2, "ResizeTerrainColumns", false);
					ImGui.SetColumnWidth(0, GuiStyle.LabelWidth);
					ImGui.SetColumnWidth(1, GuiStyle.WidgetWidth + 10);

					ImGui.Text("X");
					ImGui.NextColumn();

					ImGui.SetNextItemWidth(GuiStyle.WidgetWidth);
					ImGui.InputInt("ResizeTerrainX", ref _resizeXSize);
					_resizeXSize = Utilities.Clamp(
						_resizeXSize,
						1,
						(int) Math.Floor(256f / _resizeZSize)
					);

					ImGui.NextColumn();

					ImGui.Text("Z");
					ImGui.NextColumn();

					ImGui.SetNextItemWidth(GuiStyle.WidgetWidth);
					ImGui.InputInt("ResizeTerrainZ", ref _resizeZSize);
					_resizeZSize = Utilities.Clamp(
						_resizeZSize,
						1,
						(int) Math.Floor(256f / _resizeXSize)
					);

					ImGui.NextColumn();

					GuiStyle.AddSpace();
					if (ImGui.Button("Cancel")) {
						_resizeTerrainMode = false;
					}

					ImGui.NextColumn();

					GuiStyle.AddSpace();
					if (ImGui.Button("Resize", new Vector2(GuiStyle.WidgetWidth, 20))) {
						_resizeTerrainMode = false;
						Selection.SelectedTerrainTiles.Clear();
						CurrentMapState.StateData.Terrain.ResizeTerrain(_resizeXSize, _resizeZSize);
					}

					ImGui.Columns(1);
				}

				ImGui.Unindent();
			}
		}
	}
}