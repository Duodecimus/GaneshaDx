﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using GaneshaDx.Common;
using GaneshaDx.Environment;
using GaneshaDx.Resources;
using GaneshaDx.UserInterface.GuiDefinitions;
using ImGuiNET;

namespace GaneshaDx.UserInterface.GuiForms;

public static class GuiWindowFileBrowser {
	private enum SizableElements {
		Window,
		AddressBarWidth,
		LeftColumn,
		SpecialFoldersContainer,
		SpecialFoldersItem,
		MainFilesContainer,
		MainFilesItem,
		FooterLeftSpacing,
		FooterFileLabel,
		FooterButton
	}

	private static readonly Dictionary<SizableElements, Vector2> Sizes = new() {
		{ SizableElements.Window, new Vector2(500, 300) },
		{ SizableElements.AddressBarWidth, new Vector2(440, 0) },
		{ SizableElements.LeftColumn, new Vector2(140, 0) },
		{ SizableElements.SpecialFoldersContainer, new Vector2(130, 200) },
		{ SizableElements.SpecialFoldersItem, new Vector2(120, 20) },
		{ SizableElements.MainFilesContainer, new Vector2(340, 200) },
		{ SizableElements.MainFilesItem, new Vector2(320, 20) },
		{ SizableElements.FooterLeftSpacing, new Vector2(150, 0) },
		{ SizableElements.FooterFileLabel, new Vector2(180, 0) },
		{ SizableElements.FooterButton, new Vector2(60, 20) }
	};

	private static Vector2 _centeredWindowPosition;
	private static bool _resetWindowPosition;

	private static string _filter = ".gns";
	private static string _currentDrive = "C:\\";
	private static readonly List<string> FolderPath = new();
	private static bool _clearsSelectedFileOnNavigation;

	private static string[] _drives = Array.Empty<string>();
	private static string[] _currentFolderFiles = Array.Empty<string>();
	private static string[] _currentFolderFolders = Array.Empty<string>();
	private static string _selectedFile = string.Empty;
	private static string _selectedFolder = string.Empty;
	private static string CurrentFullPath => _currentDrive + string.Join("\\", FolderPath);

	public static string LastImportedTextureFile = String.Empty;

	private static Dictionary<string, string> _additionalData;

	public enum DialogBoxes {
		OpenMap,
		ImportGlb,
		ImportTexture,
		ImportPalette,
		ImportTerrainXML,
		SaveMapAs,
		ExportGlb,
		ExportTexture,
		ExportUvMap,
		ExportPalette,
		ExportTerrainXML
	}

	private static DialogBoxes _dialogBox = DialogBoxes.OpenMap;

	private static readonly Dictionary<DialogBoxes, string> WindowTitles = new() {
		{ DialogBoxes.OpenMap, "Open Map" },
		{ DialogBoxes.SaveMapAs, "Save Map As.." },
		{ DialogBoxes.ImportTexture, "Import Texture" },
		{ DialogBoxes.ExportUvMap, "Export Uv Map" },
		{ DialogBoxes.ExportTexture, "Export Texture" },
		{ DialogBoxes.ImportPalette, "Import Palette" },
		{ DialogBoxes.ExportPalette, "Export Palette" },
		{ DialogBoxes.ImportGlb, "Import GLB" },
		{ DialogBoxes.ExportGlb, "Export GLB" },
		{ DialogBoxes.ImportTerrainXML, "Import Terrain XML" },
		{ DialogBoxes.ExportTerrainXML, "Export Terrain XML" }
	};

	private static readonly Dictionary<DialogBoxes, string> SelectionButtonLabels = new() {
		{ DialogBoxes.OpenMap, "Open" },
		{ DialogBoxes.ImportGlb, "Import" },
		{ DialogBoxes.ImportPalette, "Import" },
		{ DialogBoxes.ImportTexture, "Import" },
		{ DialogBoxes.ImportTerrainXML, "Import" },
		{ DialogBoxes.SaveMapAs, "Save" },
		{ DialogBoxes.ExportGlb, "Export" },
		{ DialogBoxes.ExportTexture, "Export" },
		{ DialogBoxes.ExportUvMap, "Export" },
		{ DialogBoxes.ExportPalette, "Export" },
		{ DialogBoxes.ExportTerrainXML, "Export" },
	};

	private static readonly Dictionary<DialogBoxes, string> DialogBoxFilters = new() {
		{ DialogBoxes.OpenMap, "gns" },
		{ DialogBoxes.SaveMapAs, "gns" },
		{ DialogBoxes.ImportTexture, "png" },
		{ DialogBoxes.ExportUvMap, "png" },
		{ DialogBoxes.ExportTexture, "png" },
		{ DialogBoxes.ImportPalette, "act" },
		{ DialogBoxes.ExportPalette, "act" },
		{ DialogBoxes.ExportGlb, "glb" },
		{ DialogBoxes.ImportGlb, "glb" },
		{ DialogBoxes.ImportTerrainXML, "xml" },
		{ DialogBoxes.ExportTerrainXML, "xml" }
	};

	private static readonly List<DialogBoxes> BoxesWithInputText = new() {
		DialogBoxes.SaveMapAs,
		DialogBoxes.ExportGlb,
		DialogBoxes.ExportPalette,
		DialogBoxes.ExportTexture,
		DialogBoxes.ExportUvMap,
		DialogBoxes.ExportTerrainXML,
	};

	public static void Render() {
		bool windowIsOpen = true;

		GuiStyle.SetNewUiToDefaultStyle();
		ImGui.GetStyle().WindowRounding = 3;
		GuiStyle.SetFont(Fonts.Large);

		ImGui.SetNextWindowSize(Sizes[SizableElements.Window]);

		if (_resetWindowPosition) {
			ImGui.SetNextWindowPos(_centeredWindowPosition);
			_resetWindowPosition = false;
		}

		const ImGuiWindowFlags flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.HorizontalScrollbar |
		                               ImGuiWindowFlags.NoResize;

		ImGui.Begin(WindowTitles[_dialogBox], ref windowIsOpen, flags);
		{
			GuiStyle.SetFont(Fonts.Default);
			GuiStyle.SetNewUiToDefaultStyle();
			ImGui.GetStyle().FrameRounding = 0;

			RenderAddressBar();
			ImGui.Separator();

			ImGui.Columns(2);
			ImGui.SetColumnWidth(0, Sizes[SizableElements.LeftColumn].X);
			{
				RenderLocationsPanel();
				ImGui.NextColumn();
				RenderFileList();
			}
			ImGui.Columns(1);
			ImGui.Separator();

			RenderFooter();
		}
		ImGui.End();


		if (!windowIsOpen) {
			Gui.ShowOpenFileWindow = false;
		}
	}

	public static void Open(DialogBoxes dialogBox, Dictionary<string, string> additionalData = null) {
		_dialogBox = dialogBox;
		_additionalData = additionalData;

		_centeredWindowPosition = new Vector2(
			Stage.ModelingViewport.Width / 2f - Sizes[SizableElements.Window].X / 2f,
			Stage.ModelingViewport.Height / 2f - Sizes[SizableElements.Window].Y / 2f
		);
		_resetWindowPosition = true;

		_filter = DialogBoxFilters[_dialogBox];

		_selectedFile = _dialogBox switch {
			DialogBoxes.OpenMap => MapData.MapIsLoaded ? MapData.MapName + ".gns" : "",
			DialogBoxes.ExportGlb => MapData.MapName + ".glb",
			DialogBoxes.ExportTexture => MapData.MapName + "." + CurrentMapState.StateData.StateTextureResource.XFile + ".png",
			DialogBoxes.ExportUvMap => MapData.MapName + "." + CurrentMapState.StateData.StateTextureResource.XFile + ".uvMap" + ".png",
			DialogBoxes.ExportPalette => _additionalData?["PaletteId"] == "-1"
				? "default.act"
				: MapData.MapName + "." + CurrentMapState.StateData.StateMeshResources[0].XFile +
				  (_additionalData != null ? "." + _additionalData["PaletteId"] : "") +
				  ".act",
			_ => ""
		};

		_clearsSelectedFileOnNavigation = false;

		SetFolderPathFromFullPath(Configuration.Properties.LoadFolder);
		RefreshFileList();

		_clearsSelectedFileOnNavigation = !BoxesWithInputText.Contains(_dialogBox);
		Gui.ShowOpenFileWindow = true;
	}

	private static void RenderAddressBar() {
		if (ImGui.Button("UP")) {
			GoUpToParentDir();
		}

		ImGui.SameLine();
		
		string before = CurrentFullPath;
		string currentPath = CurrentFullPath;
		ImGui.SetNextItemWidth(Sizes[SizableElements.AddressBarWidth].X);
		
		ImGui.InputText("##addressBar", ref currentPath, 1000);
		
		bool inputIsActive = ImGui.IsItemActive();
		if (before != currentPath && !inputIsActive) {
			SetFolderPathFromFullPath(currentPath);
			RefreshFileList();
		}
	}

	private static void RenderLocationsPanel() {
		ImGui.BeginChild("SpecialFoldersList", Sizes[SizableElements.SpecialFoldersContainer]);
		{
			GuiStyle.SetNewUiToDefaultStyle();

			RenderPinnedFoldersList();
			if (Configuration.Properties.PinnedFileBrowserFolders.Count > 0) {
				GuiStyle.AddSpace();
			}

			RenderSpecialFoldersList();
			GuiStyle.AddSpace();

			RenderDriveList();

			GuiStyle.SetNewUiToDefaultStyle();
		}

		ImGui.EndChild();
	}

	private static void RenderPinnedFoldersList() {
		if (Configuration.Properties.PinnedFileBrowserFolders.Count > 0) {
			GuiStyle.AddSpace();

			foreach (string pinnedFolder in Configuration.Properties.PinnedFileBrowserFolders) {
				bool directoryGone = !Directory.Exists(pinnedFolder);

				if (!directoryGone) {
					string label = pinnedFolder.Split("\\").Last();
					GuiStyle.SetElementStyle(
						string.Equals(_selectedFolder, label, StringComparison.CurrentCultureIgnoreCase)
							? ElementStyle.ButtonFileBrowserDirectorySelected
							: ElementStyle.ButtonFileBrowserDirectory
					);


					if (ImGui.Button(label + "###" + pinnedFolder, Sizes[SizableElements.SpecialFoldersItem])) {
						_selectedFolder = label;
						SetFolderPathFromFullPath(pinnedFolder);
						RefreshFileList();
					}
				} else {
					Configuration.Properties.PinnedFileBrowserFolders.Remove(pinnedFolder);
					Configuration.SaveConfiguration();
					break;
				}
			}
		}
	}

	private static void RenderSpecialFoldersList() {
		List<string> specialFolders = new() {
			System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
			System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory),
			System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
		};

		foreach (string specialFolderLocation in specialFolders) {
			string specialFolderName = specialFolderLocation.Split("\\").Last();
			GuiStyle.SetElementStyle(
				string.Equals(_selectedFolder, specialFolderName, StringComparison.CurrentCultureIgnoreCase)
					? ElementStyle.ButtonFileBrowserDirectorySelected
					: ElementStyle.ButtonFileBrowserDirectory
			);

			if (ImGui.Button(specialFolderName, Sizes[SizableElements.SpecialFoldersItem])) {
				_selectedFolder = specialFolderName;
				SetFolderPathFromFullPath(specialFolderLocation);
				RefreshFileList();
			}
		}
	}

	private static void RenderDriveList() {
		foreach (string drive in _drives) {
			GuiStyle.SetElementStyle(
				string.Equals(_currentDrive, drive, StringComparison.CurrentCultureIgnoreCase) &&
				FolderPath.Count == 0
					? ElementStyle.ButtonFileBrowserDirectorySelected
					: ElementStyle.ButtonFileBrowserDirectory
			);

			if (ImGui.Button(drive, Sizes[SizableElements.SpecialFoldersItem])) {
				_currentDrive = drive;
				_selectedFolder = String.Empty;
				FolderPath.Clear();
				RefreshFileList();
			}
		}
	}

	private static void RenderFileList() {
		string folderClicked = string.Empty;

		ImGui.BeginChild("FileList", Sizes[SizableElements.MainFilesContainer]);
		{
			GuiStyle.SetElementStyle(ElementStyle.ButtonFileBrowserDirectory);

			if (FolderPath.Count > 0) {
				if (ImGui.Button("../", Sizes[SizableElements.MainFilesItem])) {
					GoUpToParentDir();
				}
			}

			GuiStyle.SetNewUiToDefaultStyle();

			foreach (string folder in _currentFolderFolders) {
				GuiStyle.SetElementStyle(
					string.Equals(_selectedFolder, folder, StringComparison.CurrentCultureIgnoreCase)
						? ElementStyle.ButtonFileBrowserDirectorySelected
						: ElementStyle.ButtonFileBrowserDirectory
				);

				if (folder.Substring(0, 1) != "$") {
					if (ImGui.Button(folder, Sizes[SizableElements.MainFilesItem])) {
						folderClicked = folder;
					}
				}

				GuiStyle.SetNewUiToDefaultStyle();
			}

			foreach (string file in _currentFolderFiles) {
				string[] filenameSegments = file.Split(".");
				string extension = filenameSegments.Last().ToLower();

				if (extension == _filter) {
					GuiStyle.SetElementStyle(
						string.Equals(_selectedFile, file, StringComparison.CurrentCultureIgnoreCase)
							? ElementStyle.ButtonFileBrowserFileSelected
							: ElementStyle.ButtonFileBrowserFile
					);

					if (ImGui.Button("     " + file, Sizes[SizableElements.MainFilesItem])) {
						if (_selectedFile == file) {
							ConfirmDialogBox();
						} else {
							_selectedFile = file;
						}
					}

					GuiStyle.SetNewUiToDefaultStyle();
				}
			}

			if (folderClicked != string.Empty) {
				if (_selectedFolder == folderClicked) {
					FolderPath.Add(folderClicked);
					RefreshFileList();
				}

				_selectedFolder = folderClicked;
			}
		}
		ImGui.EndChild();
	}

	private static void RenderFooter() {
		ImGui.Columns(3, "footerColumns", false);
		ImGui.SetColumnWidth(0, Sizes[SizableElements.FooterLeftSpacing].X);
		ImGui.SetColumnWidth(1, Sizes[SizableElements.FooterFileLabel].X);


		int index = Configuration.Properties.PinnedFileBrowserFolders.IndexOf(CurrentFullPath);

		bool folderIsSpecialFolder = CurrentFullPath == System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) ||
		                             CurrentFullPath == System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) ||
		                             CurrentFullPath == System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);

		if (index < 0 && !folderIsSpecialFolder && FolderPath.Count > 0) {
			if (ImGui.Button("Pin Folder")) {
				Configuration.Properties.PinnedFileBrowserFolders.Add(CurrentFullPath);
				Configuration.SaveConfiguration();
			}
		}

		if (index >= 0) {
			if (ImGui.Button("Un-Pin Folder")) {
				Configuration.Properties.PinnedFileBrowserFolders.RemoveAt(index);
				Configuration.SaveConfiguration();
			}
		}

		ImGui.NextColumn();

		GuiStyle.AddSpace(1);

		if (BoxesWithInputText.Contains(_dialogBox)) {
			string fileName = _selectedFile;
			ImGui.SetNextItemWidth(Sizes[SizableElements.FooterFileLabel].X - 20);
			ImGui.InputText("##fileNameBox", ref fileName, 100);
			fileName = RemoveInvalidFilenameCharacters(fileName);
			_selectedFile = fileName;
		} else {
			ImGui.Text(
				_selectedFile == String.Empty
					? "None Selected"
					: _selectedFile);
		}

		ImGui.NextColumn();

		GuiStyle.AddSpace(2);
		float widthNeeded = Sizes[SizableElements.FooterButton].X * 2 + 10;
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - widthNeeded);

		if (_selectedFile == String.Empty) {
			GuiStyle.SetElementStyle(ElementStyle.ButtonDisabled);
		}

		if (ImGui.Button(SelectionButtonLabels[_dialogBox], Sizes[SizableElements.FooterButton])) {
			if (_selectedFile != String.Empty) {
				ConfirmDialogBox();
			}
		}

		GuiStyle.SetNewUiToDefaultStyle();

		ImGui.SameLine();

		if (ImGui.Button("Cancel", Sizes[SizableElements.FooterButton])) {
			Gui.ShowOpenFileWindow = false;
		}

		ImGui.Columns(1);
	}

	private static void GoUpToParentDir() {
		if (FolderPath.Count > 0) {
			FolderPath.RemoveAt(FolderPath.Count - 1);
		} else {
			_currentDrive = _drives[0];
		}

		_selectedFolder = String.Empty;

		RefreshFileList();
	}

	private static void RefreshFileList() {
		_drives = Array.Empty<string>();
		_currentFolderFiles = Array.Empty<string>();
		_currentFolderFolders = Array.Empty<string>();

		if (_clearsSelectedFileOnNavigation) {
			_selectedFile = String.Empty;
		}

		_drives = Directory.GetLogicalDrives();

		try {
			_currentFolderFolders = Directory.GetDirectories(CurrentFullPath);
		} catch (Exception) {
			OverlayConsole.AddMessage("Directory is inaccessible: " + CurrentFullPath);
			GoUpToParentDir();
			return;
		}

		for (int index = 0; index < _currentFolderFolders.Length; index++) {
			string[] fullPathSegments = _currentFolderFolders[index].Split("\\");
			_currentFolderFolders[index] = fullPathSegments.Last();
		}

		_currentFolderFiles = Directory.GetFiles(CurrentFullPath);
		for (int index = 0; index < _currentFolderFiles.Length; index++) {
			string[] fullPathSegments = _currentFolderFiles[index].Split("\\");
			_currentFolderFiles[index] = fullPathSegments.Last();
		}
	}

	private static void SetFolderPathFromFullPath(string folder) {
		string[] folderPaths = folder.Split("\\");
		FolderPath.Clear();
		_currentDrive = folderPaths[0] + "\\";
		for (int index = 1; index < folderPaths.Length; index++) {
			FolderPath.Add(folderPaths[index]);
		}
	}

	private static string RemoveInvalidFilenameCharacters(string input) {
		char[] invalidChars = Path.GetInvalidFileNameChars();
		return string.Concat(input.Split(invalidChars));
	}

	private static void ConfirmDialogBox() {
		string filePath = CurrentFullPath + "\\" + _selectedFile;

		Dictionary<DialogBoxes, Action> operations = new() {
			{
				DialogBoxes.OpenMap,
				() => {
					Selection.SelectedPolygons.Clear();
					Selection.HoveredPolygons.Clear();
					Stage.Ganesha.PostponeRender(1);
					MapData.LoadMapDataFromFullPath(filePath);
				}
			}, {
				DialogBoxes.ImportTexture,
				() => {
					LastImportedTextureFile = filePath;
					MapData.ImportTexture(filePath);
				}
			}, {
				DialogBoxes.ImportPalette,
				() => {
					MapData.ImportPalette(
						filePath,
						int.Parse(_additionalData["PaletteId"]),
						_additionalData["PaletteType"]
					);
				}
			}, {
				DialogBoxes.ImportGlb,
				() => {
					Selection.SelectedPolygons.Clear();
					Selection.HoveredPolygons.Clear();
					Stage.Ganesha.PostponeRender(1);
					MapData.ImportGlb(filePath);
				}
			},{
				DialogBoxes.ImportTerrainXML,
				() => {
					MapData.ImportTerrainXML(filePath);
				}
			}, {
				DialogBoxes.SaveMapAs,
				() => {
					string mapName = _selectedFile;
					List<string> fileNameSegments = mapName.Split('.').ToList();

					if (fileNameSegments.Count > 1 && fileNameSegments.Last().ToLower() == "gns") {
						mapName = mapName.Remove(mapName.Length - 4);
					}

					MapData.SaveMapAs(CurrentFullPath, mapName);
				}
			}, {
				DialogBoxes.ExportGlb,
				() => {
					string mapName = _selectedFile;
					List<string> fileNameSegments = mapName.Split('.').ToList();

					if (fileNameSegments.Last().ToLower() != "glb") {
						_selectedFile += ".glb";
					}

					MapData.ExportGlb(CurrentFullPath + "\\" + _selectedFile);
				}
			},  {
				DialogBoxes.ExportTerrainXML,
				() => {
					string mapName = _selectedFile;
					List<string> fileNameSegments = mapName.Split('.').ToList();

					if (fileNameSegments.Last().ToLower() != "xml") {
						_selectedFile += ".xml";
					}

					MapData.ExportTerrainXML(CurrentFullPath + "\\" + _selectedFile);
				}
			}, {
				DialogBoxes.ExportTexture,
				() => { MapData.ExportTexture(filePath); }
			}, {
				DialogBoxes.ExportPalette, () => {
					MapData.ExportPalette(
						filePath,
						int.Parse(_additionalData["PaletteId"]),
						_additionalData["PaletteType"]
					);
				}
			}, {
				DialogBoxes.ExportUvMap, () => { MapData.ExportUvMap(filePath); }
			}
		};

		operations[_dialogBox]();

		Gui.ShowOpenFileWindow = false;
	}
}