﻿using GaneshaDx.Common;
using GaneshaDx.Environment;
using GaneshaDx.Resources;
using GaneshaDx.UserInterface.GuiDefinitions;
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
					ImGui.MenuItem("Add Polygon", "Ctrl + N", ref Gui.ShowAddPolygonWindow, MapData.MapIsLoaded);

					if (ImGui.MenuItem("Clone Selection", "Ctrl + J", false, MapData.MapIsLoaded)) {
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


					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu("View")) {
					ImGui.MenuItem("Camera Controls", "C", ref Gui.ShowCameraControlWindow, MapData.MapIsLoaded);

					if (ImGui.MenuItem("Focus on Selection", "Z", false, MapData.MapIsLoaded)) {
						StageCamera.FocusOnSelection();
					}

					ImGui.Separator();

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

					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu("Help")) {
					ImGui.MenuItem("Tips", "F1", ref Gui.ShowTipsWindow, MapData.MapIsLoaded);

					ImGui.Separator();

					if (ImGui.MenuItem("About", "F2", false, false)) { }

					ImGui.EndMenu();
				}

				ImGui.EndMainMenuBar();
			}
		}
	}
}