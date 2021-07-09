﻿using System.Collections.Generic;
using GaneshaDx.Common;
using Microsoft.Xna.Framework;

namespace GaneshaDx.Resources.ContentDataTypes.MeshAnimations {
	public class MeshAnimationFrameState {
		public readonly List<int> Properties = new List<int>();

		public Vector3 Position;
		public Vector3 Rotation;
		public Vector3 Scale;
		public readonly List<MeshAnimationKeyFrameType> PositionKeyFrameTypes = new List<MeshAnimationKeyFrameType>();
		public readonly List<MeshAnimationKeyFrameType> RotationKeyFrameTypes = new List<MeshAnimationKeyFrameType>();
		public readonly List<MeshAnimationKeyFrameType> ScaleKeyFrameTypes = new List<MeshAnimationKeyFrameType>();

		public MeshAnimationFrameState(List<byte> rawData) {
			for (int byteIndex = 0; byteIndex < rawData.Count; byteIndex += 2) {
				Properties.Add(Utilities.GetIntFromLittleEndian(rawData[byteIndex], rawData[byteIndex + 1]));
			}

			Rotation = new Vector3(Properties[0] / 4096f, Properties[1] / 4096f, Properties[2] / 4096f);
			Position = new Vector3(Properties[4], Properties[5], Properties[6]);
			Scale = new Vector3(Properties[8] / 4096f, Properties[9] / 4096f, Properties[10] / 4096f);

			RotationKeyFrameTypes.Add(ConvertKeyFrameType(Properties[30]));
			RotationKeyFrameTypes.Add(ConvertKeyFrameType(Properties[31]));
			RotationKeyFrameTypes.Add(ConvertKeyFrameType(Properties[32]));

			PositionKeyFrameTypes.Add(ConvertKeyFrameType(Properties[33]));
			PositionKeyFrameTypes.Add(ConvertKeyFrameType(Properties[34]));
			PositionKeyFrameTypes.Add(ConvertKeyFrameType(Properties[35]));

			ScaleKeyFrameTypes.Add(ConvertKeyFrameType(Properties[36]));
			ScaleKeyFrameTypes.Add(ConvertKeyFrameType(Properties[37]));
			ScaleKeyFrameTypes.Add(ConvertKeyFrameType(Properties[38]));
		}

		private MeshAnimationKeyFrameType ConvertKeyFrameType(int value) {
			MeshAnimationKeyFrameType type = value switch {
				0 => MeshAnimationKeyFrameType.None,
				5 => MeshAnimationKeyFrameType.TweenTo,
				6 => MeshAnimationKeyFrameType.TweenBy,
				9 => MeshAnimationKeyFrameType.Unknown9,
				10 => MeshAnimationKeyFrameType.Unknown10,
				17 => MeshAnimationKeyFrameType.Unknown17,
				18 => MeshAnimationKeyFrameType.Unknown18,
				_ => MeshAnimationKeyFrameType.Other
			};

			return type;
		}

		private int ConvertKeyFrameType(MeshAnimationKeyFrameType value) {
			int type = value switch {
				 MeshAnimationKeyFrameType.None=> 0,
				 MeshAnimationKeyFrameType.TweenTo=> 5,
				 MeshAnimationKeyFrameType.TweenBy=> 6,
				 MeshAnimationKeyFrameType.Unknown9=> 9,
				 MeshAnimationKeyFrameType.Unknown10=> 10,
				 MeshAnimationKeyFrameType.Unknown17=> 17,
				 MeshAnimationKeyFrameType.Unknown18=> 18,
				_ => 99
			};

			return type;
		}
	}
}