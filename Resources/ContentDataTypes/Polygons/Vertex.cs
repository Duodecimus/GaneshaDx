﻿using Microsoft.Xna.Framework;

namespace GaneshaDx.Resources.ContentDataTypes.Polygons {
	public class Vertex {
		public Vector3 Position;
		public Vector3 AnimationAdjustedPosition;
		public bool UsesNormal = false;
		public float NormalAzimuth;
		public float NormalElevation;
		public Color Color;
	}
}