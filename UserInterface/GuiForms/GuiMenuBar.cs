﻿using System.Diagnostics;
using GaneshaDx.Common;
using GaneshaDx.Environment;
using GaneshaDx.Resources;
using GaneshaDx.UserInterface.GuiDefinitions;
using GaneshaDx.UserInterface.Widgets;
using ImGuiNET;

namespace GaneshaDx.UserInterface.GuiForms {
	public static class GuiMenuBar {
		public static void Render() {
			GuiStyle.SetNewUiToDefaultStyle();
			ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[2]);

			if (ImGui.BeginMainMenuBar()) {
				if (ImGui.BeginMenu("File")) {
					if (ImGui.MenuItem("New", "Ctrl+Shift + N", false, false)) { }

					if (ImGui.MenuItem("Open", "Ctrl + O")) {
						MyraGui.OpenOpenFileDialog();
					}

					if (ImGui.MenuItem("Save", "Ctrl + S", false, MapData.MapIsLoaded)) {
						MapData.SaveMap();
					}

					if (ImGui.MenuItem("Save As", "Ctrl + Shift + S", false, false)) { }

					ImGui.Separator();

					ImGui.MenuItem("Preferences", "P", ref Gui.ShowPreferencesWindow, MapData.MapIsLoaded);

					ImGui.Separator();

					if (ImGui.MenuItem("Quit", "Ctrl + Q")) {
						Stage.Ganesha.Exit();
					}

					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu("Edit")) {
					bool canCopyOrPasteVertexPositions = Gui.Widget == WidgetSelectionMode.PolygonVertexTranslate &&
					                                     Selection.SelectedPolygons.Count == 1;

					if (ImGui.MenuItem("Store Vertex", "S", false, MapData.MapIsLoaded && canCopyOrPasteVertexPositions)) {
						TransformWidget.SelectSnappingVertex();
					}

					if (ImGui.MenuItem("Snap Stored Vertex", "D", false, MapData.MapIsLoaded && canCopyOrPasteVertexPositions)) {
						TransformWidget.SnapSnappingVertex();
					}

					ImGui.Separator();
					
					ImGui.MenuItem("Add Polygon", "Ctrl + N", ref Gui.ShowAddPolygonWindow, MapData.MapIsLoaded);

					if (ImGui.MenuItem("Clone Selection", "Ctrl + D", false, MapData.MapIsLoaded)) {
						CurrentMapState.CloneSelection();
					}

					if (ImGui.MenuItem("Delete Selection", "DEL", false, MapData.MapIsLoaded)) {
						CurrentMapState.DeleteSelection();
					}

					ImGui.Separator();

					if (ImGui.MenuItem("Import Texture", "Ctrl + I", false, MapData.MapIsLoaded)) {
						MyraGui.OpenImportTextureFileDialog();
					}

					if (ImGui.MenuItem("Export Texture", "Ctrl + E", false, MapData.MapIsLoaded)) {
						string fileName = MapData.MapName + "." +
						                  CurrentMapState.StateData.StateTextureResource.XFile +
						                  ".png";

						MyraGui.OpenExportTextureFileDialog(fileName);
					}

					bool canReimportTexture = MapData.MapIsLoaded && MyraGui.LastImportedTextureFile != "";

					if (ImGui.MenuItem("Re-Import Texture", "Ctrl + R", false, canReimportTexture)) {
						MapData.ImportTexture(MyraGui.LastImportedTextureFile);
					}

					if (ImGui.MenuItem("Export UV Map", "Ctrl + U", false, MapData.MapIsLoaded)) {
						string fileName = MapData.MapName + "." +
						                  CurrentMapState.StateData.StateTextureResource.XFile +
						                  ".uvMap" +
						                  ".png";

						MyraGui.OpenExportUvMapFileDialog(fileName);
					}
					
					ImGui.Separator();

					if (ImGui.MenuItem("Export to GLB", "Ctrl + Shift + E", false, MapData.MapIsLoaded)) {
						string fileName = MapData.MapName + ".glb";

						MyraGui.OpenExportGlbFileDialog(fileName);
					}

					ImGui.Separator();

					bool canEditMeshAnimations = MapData.MapIsLoaded &&
					                             CurrentMapState.StateData.MeshAnimationInstructions != null;

					if (ImGui.MenuItem(
						"Edit Mesh Animations",
						"Ctrl + G",
						Gui.ShowMeshAnimationsWindow,
						canEditMeshAnimations)
					) {
						Gui.ShowMeshAnimationsWindow = !Gui.ShowMeshAnimationsWindow;
					}

					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu("Selection")) {
					if (ImGui.MenuItem(
						"Select All Polygons",
						"Ctrl + A",
						false,
						MapData.MapIsLoaded && Gui.SelectedTab != RightPanelTab.Terrain
					)) {
						Gui.Widget = WidgetSelectionMode.PolygonTranslate;
						Selection.SelectAllPolygons();
					}

					if (ImGui.MenuItem(
						"Grow Polygon Selection",
						"+",
						false,
						MapData.MapIsLoaded &&
						Gui.SelectedTab != RightPanelTab.Terrain &&
						Selection.SelectedPolygons.Count > 0
					)) {
						Selection.GrowPolygonSelection();
					}

					ImGui.Separator();

					if (ImGui.MenuItem("Select All Terrain Tiles", "Ctrl + A", false,
						MapData.MapIsLoaded && Gui.SelectedTab == RightPanelTab.Terrain)
					) {
						Selection.SelectAllTerrainTiles();
					}

					if (ImGui.MenuItem(
						"Grow Terrain Selection",
						"+",
						false,
						MapData.MapIsLoaded &&
						Gui.SelectedTab == RightPanelTab.Terrain &&
						Selection.SelectedTerrainTiles.Count > 0)
					) {
						Selection.GrowTerrainSelection();
					}


					ImGui.Separator();

					if (ImGui.MenuItem(
						"Select Next Vertex",
						"F",
						false,
						MapData.MapIsLoaded &&
						Selection.SelectedPolygons.Count > 0 &&
						Gui.Widget == WidgetSelectionMode.PolygonVertexTranslate)
					) {
						TransformWidget.SelectNextVertex(false);
					}

					if (ImGui.MenuItem(
						"Select Previous Vertex",
						"Shift + F",
						false,
						MapData.MapIsLoaded &&
						Selection.SelectedPolygons.Count > 0 &&
						Gui.Widget == WidgetSelectionMode.PolygonVertexTranslate)
					) {
						TransformWidget.SelectNextVertex(true);
					}

					if (ImGui.MenuItem(
						"Select Next Edge",
						"F",
						false,
						MapData.MapIsLoaded &&
						Selection.SelectedPolygons.Count > 0 &&
						Gui.Widget == WidgetSelectionMode.PolygonEdgeTranslate)
					) {
						TransformWidget.SelectNextEdge(false);
					}

					if (ImGui.MenuItem(
						"Select Previous Edge",
						"Shift + F",
						false,
						MapData.MapIsLoaded &&
						Selection.SelectedPolygons.Count > 0 &&
						Gui.Widget == WidgetSelectionMode.PolygonEdgeTranslate)
					) {
						TransformWidget.SelectNextEdge(true);
					}

					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu("View")) {
					ImGui.MenuItem("Camera Controls", "C", ref Gui.ShowCameraControlWindow, MapData.MapIsLoaded);

					if (ImGui.MenuItem("Focus on Selection", "Z", false, MapData.MapIsLoaded)) {
						StageCamera.FocusOnSelection();
					}

					ImGui.Separator();

					bool beforeOrthoModeChange = Configuration.Properties.RenderFftOrtho;
					ImGui.MenuItem(
						"FFT Ortho Mode",
						"B",
						ref Configuration.Properties.RenderFftOrtho,
						MapData.MapIsLoaded
					);

					if (beforeOrthoModeChange != Configuration.Properties.RenderFftOrtho) {
						Configuration.SaveConfiguration();
					}

					bool beforeLightingModeChange = Configuration.Properties.RenderPolygonsInLightingMode;
					ImGui.MenuItem(
						"Lighting Mode",
						"L",
						ref Configuration.Properties.RenderPolygonsInLightingMode,
						MapData.MapIsLoaded
					);

					if (beforeLightingModeChange != Configuration.Properties.RenderPolygonsInLightingMode) {
						Configuration.SaveConfiguration();
					}
					
					bool beforeHideInvisiblePolys = Configuration.Properties.HideHiddenPolysByFacing;
					ImGui.MenuItem(
						"Hide Invisible Polys",
						"H",
						ref Configuration.Properties.HideHiddenPolysByFacing,
						MapData.MapIsLoaded
					);

					if (beforeHideInvisiblePolys != Configuration.Properties.HideHiddenPolysByFacing) {
						Configuration.SaveConfiguration();
					}
					ImGui.Separator();

					if (ImGui.MenuItem(
						"Screenshot Mode",
						"F12",
						false,
						MapData.MapIsLoaded
					)) {
						Stage.ToggleScreenshotMode();
					}

					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu("Help")) {
					if (ImGui.MenuItem("Video Tutorial Series", "", false, true)) {
						Process.Start(new ProcessStartInfo {
							FileName = "https://www.youtube.com/playlist?list=PLh_iA7J_8dx3XcC5xZU3MkRoYFrsLOe0D",
							UseShellExecute = true
						});
					}

					ImGui.Separator();

					if (ImGui.MenuItem("About", "", ref Gui.ShowAboutWindow, true)) { }

					ImGui.EndMenu();
				}

				ImGui.EndMainMenuBar();
			}
		}
	}
}