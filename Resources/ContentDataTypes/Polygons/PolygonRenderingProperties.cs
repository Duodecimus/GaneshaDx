﻿namespace GaneshaDx.Resources.ContentDataTypes.Polygons {
	public class PolygonRenderingProperties {
		public readonly string RawData;
		public bool InvisibleNorthwest;
		public bool InvisibleNorthNorthwest;
		public bool InvisibleNorthNortheast;
		public bool InvisibleNortheast;
		public bool InvisibleWestNorthWest;
		public bool InvisibleEastNortheast;
		public bool InvisibleWestSouthwest;
		public bool InvisibleEastSoutheast;
		public bool InvisibleSouthwest;
		public bool InvisibleSouthSouthwest;
		public bool InvisibleSouthSoutheast;
		public bool InvisibleSoutheast;
		public bool LitTexture;
		private bool _unknownA;
		private bool _unknownB;
		private bool _unknownC;

		public PolygonRenderingProperties() {
			RawData = "0000000000000000";
			LitTexture = true;
		}

		public PolygonRenderingProperties(string rawData) {
			RawData = rawData;
			LitTexture = RawData.Substring(0, 1) == "1";
			_unknownA = RawData.Substring(1, 1) == "1";
			InvisibleSouthwest = RawData.Substring(2, 1) == "1";
			InvisibleNorthwest = RawData.Substring(3, 1) == "1";
			InvisibleNortheast = RawData.Substring(4, 1) == "1";
			InvisibleSoutheast = RawData.Substring(5, 1) == "1";
			InvisibleSouthSouthwest = RawData.Substring(6, 1) == "1";
			InvisibleWestSouthwest = RawData.Substring(7, 1) == "1";
			InvisibleWestNorthWest = RawData.Substring(8, 1) == "1";
			InvisibleNorthNorthwest = RawData.Substring(9, 1) == "1";
			InvisibleNorthNortheast = RawData.Substring(10, 1) == "1";
			InvisibleEastNortheast = RawData.Substring(11, 1) == "1";
			InvisibleEastSoutheast = RawData.Substring(12, 1) == "1";
			InvisibleSouthSoutheast = RawData.Substring(13, 1) == "1";
			_unknownB = RawData.Substring(14, 1) == "1";
			_unknownC = RawData.Substring(15, 1) == "1";
		}
	}
}