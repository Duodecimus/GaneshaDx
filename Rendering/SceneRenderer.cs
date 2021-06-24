﻿using System;
using System.Collections.Generic;
using GaneshaDx.Common;
using GaneshaDx.Environment;
using GaneshaDx.Resources;
using GaneshaDx.Resources.ContentDataTypes.Palettes;
using GaneshaDx.Resources.ContentDataTypes.Polygons;
using GaneshaDx.Resources.ContentDataTypes.TextureAnimations;
using GaneshaDx.UserInterface;
using GaneshaDx.UserInterface.GuiDefinitions;
using GaneshaDx.UserInterface.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DirectionalLight = GaneshaDx.Resources.ContentDataTypes.DirectionalLight;

namespace GaneshaDx.Rendering {
	public static class SceneRenderer {
		private static readonly List<DirectionalLightIndicator> LightIndicators = new List<DirectionalLightIndicator>();
		public static readonly List<Palette> AnimationAdjustedPalettes = new List<Palette>();
		private static Compass _compass;

		public static void Update() {
			if (MapData.MapIsLoaded) {
				SetUvAnimationInstructionsToFftShader();
				SetPaletteAnimationInstructions();
			}
		}

		public static void Reset() {
			LightIndicators.Clear();
		}

		public static void Render() {
			if (!MapData.MapIsLoaded) {
				return;
			}

			SetCompass();
			SetDirectionalLightIndicators();

			Stage.GraphicsDevice.Viewport = Stage.ModelingViewport;
			Stage.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
			Stage.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

			if (Gui.SelectedTab == RightPanelTab.Terrain) {
				CurrentMapState.StateData.Terrain.Render();

				foreach (Polygon polygon in CurrentMapState.StateData.PolygonCollectionBucket) {
					polygon.Render();
				}
			} else {
				foreach (Polygon polygon in CurrentMapState.StateData.PolygonCollectionBucket) {
					polygon.Render();
				}

				CurrentMapState.StateData.Terrain.Render();

				foreach (Polygon polygon in Selection.SelectedPolygons) {
					if (polygon.IsSelected && polygon != Selection.SelectedPolygons[0]) {
						polygon.RenderVertexIndicators();
					}
					
					Selection.SelectedPolygons[0].RenderVertexIndicators();
				}
			}

			foreach (DirectionalLightIndicator lightIndicator in LightIndicators) {
				lightIndicator.Render();
			}

			_compass.Render();
		}

		private static void SetCompass() {
			_compass ??= new Compass();
		}

		private static void SetDirectionalLightIndicators() {
			if (LightIndicators.Count != 3) {
				LightIndicators.Clear();
				List<DirectionalLight> directionalLights = CurrentMapState.StateData.DirectionalLights;
				LightIndicators.Add(new DirectionalLightIndicator(directionalLights[0], Color.DarkRed));
				LightIndicators.Add(new DirectionalLightIndicator(directionalLights[1], Color.DarkGreen));
				LightIndicators.Add(new DirectionalLightIndicator(directionalLights[2], Color.DarkBlue));
			}
		}

		private static void SetUvAnimationInstructionsToFftShader() {
			for (int animationIndex = 0; animationIndex < 32; animationIndex++) {
				Stage.FftPolygonEffect.Parameters["UsesAnimatedUv" + animationIndex].SetValue(false);
			}

			List<AnimatedTextureInstructions> animations = CurrentMapState.StateData.TextureAnimations;

			if (animations == null) {
				return;
			}

			for (int animationIndex = 0; animationIndex < animations.Count; animationIndex++) {
				AnimatedTextureInstructions animatedTextureInstructions = animations[animationIndex];

				if (animatedTextureInstructions.TextureAnimationType != TextureAnimationType.UvAnimation) {
					continue;
				}

				UvAnimation uvAnimation = (UvAnimation) animatedTextureInstructions.Instructions;

				if (uvAnimation == null) {
					continue;
				}

				if (uvAnimation.UvAnimationMode != UvAnimationMode.ForwardLooping &&
				    uvAnimation.UvAnimationMode != UvAnimationMode.ForwardAndReverseLooping
				) {
					continue;
				}

				Rectangle canvasRectangle = new Rectangle(
					uvAnimation.CanvasX,
					uvAnimation.CanvasY + uvAnimation.CanvasTexturePage * 256,
					uvAnimation.SizeWidth,
					uvAnimation.SizeHeight
				);

				int frameId = GetAnimationFrameId(uvAnimation);

				int frameX = uvAnimation.FirstFrameX + frameId * uvAnimation.SizeWidth;
				int frameY = uvAnimation.FirstFrameY + uvAnimation.FirstFrameTexturePage * 256;

				if (frameX + uvAnimation.SizeWidth > 256) {
					frameX = 8;
					frameY += 16;
				}

				Stage.FftPolygonEffect.Parameters["UsesAnimatedUv" + animationIndex].SetValue(true);

				Stage.FftPolygonEffect.Parameters["AnimatedUvCanvas" + animationIndex].SetValue(
					new Vector4(
						canvasRectangle.X / 256f,
						canvasRectangle.Y / 1024f,
						(canvasRectangle.X + canvasRectangle.Width) / 256f,
						(canvasRectangle.Y + canvasRectangle.Height) / 1024f
					)
				);

				Stage.FftPolygonEffect.Parameters["AnimatedUvSource" + animationIndex].SetValue(
					new Vector4(
						frameX / 256f,
						frameY / 1024f,
						(frameX + canvasRectangle.Width) / 256f,
						(frameY + canvasRectangle.Height) / 1024f
					)
				);

				uvAnimation.PreviousFramesFrameId = frameId;
			}
		}

		private static void SetPaletteAnimationInstructions() {
			AnimationAdjustedPalettes.Clear();

			foreach (Palette palette in CurrentMapState.StateData.Palettes) {
				Palette newPalette = new Palette();
				foreach (PaletteColor paletteColor in palette.Colors) {
					newPalette.Colors.Add(new PaletteColor(
						paletteColor.Red, paletteColor.Green, paletteColor.Blue, paletteColor.IsTransparent
					));
				}

				AnimationAdjustedPalettes.Add(newPalette);
			}

			List<AnimatedTextureInstructions> animations = CurrentMapState.StateData.TextureAnimations;

			if (animations == null) {
				return;
			}

			foreach (AnimatedTextureInstructions textureAnimationInstructions in animations) {
				if (textureAnimationInstructions.TextureAnimationType != TextureAnimationType.PaletteAnimation) {
					continue;
				}

				PaletteAnimation paletteAnimation = (PaletteAnimation) textureAnimationInstructions.Instructions;

				if (paletteAnimation == null) {
					continue;
				}

				int frameId = GetAnimationFrameId(paletteAnimation) + paletteAnimation.AnimationStartIndex;

				Palette targetPalette = AnimationAdjustedPalettes[paletteAnimation.OverriddenPaletteId];
				Palette animatedPaletteFrame = CurrentMapState.StateData.PaletteAnimationFrames[frameId];

				for (int colorIndex = 0; colorIndex < targetPalette.Colors.Count; colorIndex++) {
					targetPalette.Colors[colorIndex] = animatedPaletteFrame.Colors[colorIndex];
				}
			}
		}

		private static int GetAnimationFrameId(UvAnimation uvAnimation) {
			double adjustedFrameDuration = uvAnimation.FrameDuration * 1000 / 60f;
			double totalDuration = uvAnimation.FrameCount * adjustedFrameDuration;
			double millisecondsPlayed = Stage.GameTime.TotalGameTime.TotalMilliseconds;
			double timeIntoLoop = millisecondsPlayed % totalDuration;
			double timesLoopPlayed = Math.Floor(millisecondsPlayed / totalDuration);
			int frameId = (int) Math.Floor((float) timeIntoLoop / adjustedFrameDuration);

			if (uvAnimation.UvAnimationMode == UvAnimationMode.ForwardAndReverseLooping &&
			    timesLoopPlayed % 2 == 0
			) {
				frameId = uvAnimation.FrameCount - 1 - frameId;
			}

			return frameId;
		}

		private static int GetAnimationFrameId(PaletteAnimation paletteAnimation) {
			double adjustedFrameDuration = paletteAnimation.FrameDuration * 1000 / 60f;
			double totalDuration = paletteAnimation.FrameCount * adjustedFrameDuration;
			double millisecondsPlayed = Stage.GameTime.TotalGameTime.TotalMilliseconds;
			double timeIntoLoop = millisecondsPlayed % totalDuration;
			int frameId = (int) Math.Floor((float) timeIntoLoop / adjustedFrameDuration);

			return frameId;
		}
	}
}