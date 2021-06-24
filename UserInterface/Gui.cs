﻿using System;
using GaneshaDx.Environment;
using GaneshaDx.Resources;
using GaneshaDx.UserInterface.GuiDefinitions;
using GaneshaDx.UserInterface.GuiForms;
using GaneshaDx.UserInterface.Input;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace GaneshaDx.UserInterface {
	public static class Gui {
		public static RightPanelTab SelectedTab;
		private static bool _showDebugPanel;
		public static bool ShowCameraControlWindow;
		public static bool ShowPreferencesWindow;
		public static bool ShowAddPolygonWindow;
		private static bool _showManageResourcesWindow;
		public static bool ShowTipsWindow;

		public static void Render() {
			Stage.GraphicsDevice.Clear(Color.Transparent);

			if (AppInput.ControlHeld && AppInput.ShiftHeld && AppInput.AltHeld && AppInput.KeyJustPressed(Keys.D)) {
				_showDebugPanel = !_showDebugPanel;
			}

			Stage.ImGuiRenderer.BeforeLayout(Stage.GameTime.ElapsedGameTime);
			{
				GuiStyle.SetNewUiToDefaultStyle();
				MyraGui.LockModeling = false;

				if (_showDebugPanel) {
					ImGui.ShowDemoWindow();
				}

				if (_showManageResourcesWindow) {
					GuiWindowManageResources.Render();
					MyraGui.LockModeling = true;
				} else {
					GuiMenuBar.Render();
					GuiWindowTexturePreview.Render();

					if (MapData.MapIsLoaded) {
						RenderTabPanel();
						RenderMainPanel();

						if (ShowCameraControlWindow) {
							GuiWindowCameraControls.Render();
						}

						if (ShowPreferencesWindow) {
							GuiWindowPreferences.Render();
						}

						if (ShowAddPolygonWindow) {
							GuiWindowAddPolygon.Render();
						}

						if (ShowTipsWindow) {
							GuiWindowTips.Render();
						}
					}

					if (ImGui.GetIO().WantCaptureKeyboard || ImGui.GetIO().WantCaptureMouse) {
						MyraGui.LockModeling = true;
					}
				}
			}
			Stage.ImGuiRenderer.AfterLayout();
		}

		private static void RenderTabPanel() {
			GuiStyle.SetNewUiToDefaultStyle();

			ImGui.GetStyle().FrameRounding = 0;
			ImGui.GetStyle().WindowRounding = 0;
			GuiStyle.SetElementStyle(ElementStyle.WindowNoPadding);
			ImGui.GetStyle().Colors[(int) ImGuiCol.WindowBg] = GuiStyle.ColorPalette[ColorName.Darker];

			ImGui.SetNextWindowSize(new Vector2(GuiStyle.RightPanelWidth, GuiStyle.TabPanelHeight));
			ImGui.SetNextWindowPos(new Vector2(Stage.Width - GuiStyle.RightPanelWidth, GuiStyle.MenuBarHeight));

			ImGui.Begin("Tab Panel", GuiStyle.FixedWindowFlags | ImGuiWindowFlags.NoBringToFrontOnFocus);
			{
				ImGui.Indent();
				GuiStyle.SetElementStyle(SelectedTab == RightPanelTab.Polygon
					? ElementStyle.ButtonTabSelected
					: ElementStyle.ButtonTabUnselected);

				if (ImGui.Button("Polygon")) {
					SelectedTab = RightPanelTab.Polygon;
				}

				ImGui.SameLine();

				GuiStyle.SetElementStyle(SelectedTab == RightPanelTab.Texture
					? ElementStyle.ButtonTabSelected
					: ElementStyle.ButtonTabUnselected);

				if (ImGui.Button("Texture")) {
					SelectedTab = RightPanelTab.Texture;
				}

				ImGui.SameLine();

				GuiStyle.SetElementStyle(SelectedTab == RightPanelTab.Terrain
					? ElementStyle.ButtonTabSelected
					: ElementStyle.ButtonTabUnselected);

				if (ImGui.Button("Terrain")) {
					SelectedTab = RightPanelTab.Terrain;
				}

				ImGui.SameLine();

				GuiStyle.SetElementStyle(SelectedTab == RightPanelTab.Map
					? ElementStyle.ButtonTabSelected
					: ElementStyle.ButtonTabUnselected);

				if (ImGui.Button("Map")) {
					SelectedTab = RightPanelTab.Map;
				}

				ImGui.Unindent();
			}
			ImGui.End();
		}

		private static void RenderMainPanel() {
			const int top = GuiStyle.TabPanelHeight + GuiStyle.MenuBarHeight;
			ImGui.SetNextWindowSize(new Vector2(GuiStyle.RightPanelWidth, Stage.Height - top));
			ImGui.SetNextWindowPos(new Vector2(Stage.Width - GuiStyle.RightPanelWidth, top));

			GuiStyle.SetNewUiToDefaultStyle();
			ImGui.GetStyle().FrameRounding = 0;

			GuiStyle.SetElementStyle(ElementStyle.FixedPanelStyle);

			ImGui.Begin("Main Panel", GuiStyle.FixedWindowFlags);
			{
				switch (SelectedTab) {
					case RightPanelTab.Polygon:
						GuiPanelPolygon.Render();
						break;
					case RightPanelTab.Texture:
						GuiPanelTexture.Render();
						break;
					case RightPanelTab.Terrain:
						GuiPanelTerrain.Render();
						break;
					case RightPanelTab.Map:
						GuiPanelMap.Render();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			ImGui.End();
		}

		public static void ToggleManageResourcesWindow() {
			_showManageResourcesWindow = !_showManageResourcesWindow;
			Stage.FullModelingViewportMode = _showManageResourcesWindow;

			if (_showManageResourcesWindow) {
				Selection.SelectedPolygons.Clear();
				Selection.SelectedTerrainTiles.Clear();
			}
		}
	}
}