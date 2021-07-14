﻿using System.Collections.Generic;
using GaneshaDx.Resources;
using GaneshaDx.Resources.ContentDataTypes.MeshAnimations;
using GaneshaDx.UserInterface.GuiDefinitions;
using ImGuiNET;
using TextCopy;

namespace GaneshaDx.UserInterface.GuiForms {
	public static class GuiWindowDebugAnimatedMeshData {
		private static bool _showUnused = false;
		
		public static void Render() {
			GuiStyle.SetNewUiToDefaultStyle();
			ImGui.GetStyle().WindowRounding = 3;
			ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[2]);

			bool windowIsOpen = true;

			ImGui.Begin("Animation Bytes", ref windowIsOpen);
			{
				ImGui.Checkbox("Show Unused? ", ref _showUnused);
				
				MeshAnimationInstructions set = CurrentMapState.StateData.MeshAnimationInstructions;
				ImGui.PopFont();

				if (ImGui.CollapsingHeader("Instructions")) {
					ImGui.Indent();

					const int inputWidth = 40;
					for (int setIndex = 0; setIndex < set.FrameStates.Count; setIndex++) {
						MeshAnimationFrameState frameState = set.FrameStates[setIndex];

						GuiStyle.SetNewUiToDefaultStyle();

						bool highlightHeader = false;

						foreach (int data in frameState.Properties) {
							if (data != 0) {
								highlightHeader = true;
								break;
							}
						}

						if (highlightHeader) {
							ImGui.GetStyle().Colors[(int) ImGuiCol.Header] = GuiStyle.ColorPalette[ColorName.Lightest];
							ImGui.GetStyle().Colors[(int) ImGuiCol.Text] = GuiStyle.ColorPalette[ColorName.Dark];
						}

						if ((highlightHeader || _showUnused) && ImGui.CollapsingHeader("Set " + (setIndex + 1))) {
							GuiStyle.SetNewUiToDefaultStyle();
							ImGui.Columns(5, "InstructionSetData", false);
							ImGui.SetColumnWidth(0, 30);
							ImGui.SetColumnWidth(1, inputWidth + 10);
							ImGui.SetColumnWidth(2, inputWidth + 10);
							ImGui.SetColumnWidth(3, inputWidth + 10);
							ImGui.SetColumnWidth(4, inputWidth + 10);

							for (int dataIndex = 0; dataIndex < frameState.Properties.Count;) {
								ImGui.Text(dataIndex + ": ");
								ImGui.NextColumn();

								for (int field = 0; field < 4; field++) {
									int value = frameState.Properties[dataIndex];
									ImGui.SetNextItemWidth(inputWidth);
									ImGui.DragInt("###instruction_" + setIndex + "_" + dataIndex, ref value);
									frameState.Properties[dataIndex] = value;
									ImGui.NextColumn();
									dataIndex++;
								}
							}

							ImGui.Columns(1);

							if (ImGui.Button("Copy##instructionSet_" + setIndex)) {
								List<string> bytes = new List<string>();
								foreach (int x in frameState.Properties) {
									bytes.Add(x.ToString());
								}

								Clipboard.SetText(string.Join(",", bytes));
							}

							ImGui.SameLine();

							if (ImGui.Button("Paste##instructionSet_" + setIndex)) {
								string bytes = Clipboard.GetText();
								string[] byteArray = bytes.Split(",");
								for (int index = 0; index < byteArray.Length; index++) {
									string entry = byteArray[index];
									byte byteValue = (byte) int.Parse(entry);
									frameState.Properties[index] = byteValue;
								}
							}
						}
					}

					ImGui.Unindent();
				}

				if (ImGui.CollapsingHeader("Links")) {
					ImGui.Indent();

					const int inputWidth = 40;
					for (int setIndex = 0; setIndex < set.MeshAnimations.Count; setIndex++) {
						MeshAnimation link = set.MeshAnimations[setIndex];

						GuiStyle.SetNewUiToDefaultStyle();

						bool highlightHeader = false;

						foreach (int data in link.RawData) {
							if (data != 0) {
								highlightHeader = true;
								break;
							}
						}

						if (highlightHeader) {
							ImGui.GetStyle().Colors[(int) ImGuiCol.Header] = GuiStyle.ColorPalette[ColorName.Lightest];
							ImGui.GetStyle().Colors[(int) ImGuiCol.Text] = GuiStyle.ColorPalette[ColorName.Dark];
						}

						if ((highlightHeader || _showUnused) && ImGui.CollapsingHeader("Link " + (setIndex + 1))) {
							GuiStyle.SetNewUiToDefaultStyle();
							ImGui.Columns(6, "LinkSetData", false);
							ImGui.SetColumnWidth(0, 30);
							ImGui.SetColumnWidth(1, 30);
							ImGui.SetColumnWidth(2, inputWidth + 10);
							ImGui.SetColumnWidth(3, inputWidth + 10);
							ImGui.SetColumnWidth(4, inputWidth + 10);
							ImGui.SetColumnWidth(5, inputWidth + 10);

							ImGui.Text("");
							ImGui.NextColumn();
							ImGui.Text("KF");
							ImGui.NextColumn();
							ImGui.Text("A");
							ImGui.NextColumn();
							ImGui.Text("B");
							ImGui.NextColumn();
							ImGui.Text("C");
							ImGui.NextColumn();
							ImGui.Text("D");
							ImGui.NextColumn();

							int keyFrameId = 1;
							for (int dataIndex = 0; dataIndex < link.RawData.Count;) {
								ImGui.Text(dataIndex + ": ");
								ImGui.NextColumn();

								ImGui.Text(keyFrameId.ToString());
								keyFrameId++;
								ImGui.NextColumn();

								for (int field = 0; field < 4; field++) {
									int value = link.RawData[dataIndex];
									ImGui.SetNextItemWidth(inputWidth);
									ImGui.DragInt("###link_" + setIndex + "_" + dataIndex, ref value);
									link.RawData[dataIndex] = (byte) value;
									ImGui.NextColumn();
									dataIndex++;
								}
							}

							ImGui.Columns(1);

							if (ImGui.Button("Copy##linkSet_" + setIndex)) {
								List<string> bytes = new List<string>();
								foreach (byte x in link.RawData) {
									bytes.Add(x.ToString());
								}

								Clipboard.SetText(string.Join(",", bytes));
							}

							ImGui.SameLine();

							if (ImGui.Button("Paste##linkSet_" + setIndex)) {
								string bytes = Clipboard.GetText();
								string[] byteArray = bytes.Split(",");
								for (int index = 0; index < byteArray.Length; index++) {
									string entry = byteArray[index];
									byte byteValue = (byte) int.Parse(entry);
									link.RawData[index] = byteValue;
								}
							}
						}
					}

					ImGui.Unindent();
				}

				if (ImGui.CollapsingHeader("Unknown Chunk")) {
					ImGui.Indent();

					const int inputWidth = 40;

					ImGui.Columns(9, "Data", false);
					ImGui.SetColumnWidth(0, 30);
					ImGui.SetColumnWidth(1, inputWidth + 10);
					ImGui.SetColumnWidth(2, inputWidth + 10);
					ImGui.SetColumnWidth(3, inputWidth + 10);
					ImGui.SetColumnWidth(4, inputWidth + 10);
					ImGui.SetColumnWidth(5, inputWidth + 10);
					ImGui.SetColumnWidth(6, inputWidth + 10);
					ImGui.SetColumnWidth(7, inputWidth + 10);
					ImGui.SetColumnWidth(8, inputWidth + 10);

					for (int dataIndex = 0; dataIndex < set.UnknownChunk.Data.Count;) {
						ImGui.Text(dataIndex + ": ");
						ImGui.NextColumn();

						for (int field = 0; field < 8; field++) {
							int value = set.UnknownChunk.Data[dataIndex];
							ImGui.SetNextItemWidth(inputWidth);
							ImGui.DragInt("###linkUnknownChunk" + dataIndex, ref value);
							set.UnknownChunk.Data[dataIndex] = (byte) value;
							ImGui.NextColumn();
							dataIndex++;
							if (dataIndex > set.UnknownChunk.Data.Count - 1) {
								break;
							}
						}
					}

					ImGui.Columns(1);

					if (ImGui.Button("Copy##linkUnknownChunk")) {
						List<string> bytes = new List<string>();
						foreach (byte x in set.UnknownChunk.Data) {
							bytes.Add(x.ToString());
						}

						Clipboard.SetText(string.Join(",", bytes));
					}

					ImGui.SameLine();

					if (ImGui.Button("Paste##UnknownChunk")) {
						string bytes = Clipboard.GetText();
						string[] byteArray = bytes.Split(",");
						for (int index = 0; index < byteArray.Length; index++) {
							string entry = byteArray[index];
							byte byteValue = (byte) int.Parse(entry);
							set.UnknownChunk.Data[index] = byteValue;
						}
					}
				}

				ImGui.Unindent();
			}
			if (!windowIsOpen) {
				Gui.ShowDebugAnimatedMeshWindow = false;
			}
		}
	}
}