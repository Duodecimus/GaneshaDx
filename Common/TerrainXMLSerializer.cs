using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using GaneshaDx.Resources.ContentDataTypes.Terrains;
using System.Xml;

namespace GaneshaDx.Common {
	internal class TerrainXMLSerializer {
		public static void SaveTerrainToXml(Terrain terrain, string filePath) {
			var serializer = new XmlSerializer(typeof(Terrain));
			using var writer = new StreamWriter(filePath);
			serializer.Serialize(writer, terrain);
		}
		public static Terrain LoadTerrainFromXml(string filePath) {
			List<string> errorMessages = new();

			// Use XmlReaderSettings to capture XML parsing errors (line numbers, etc.)
			XmlReaderSettings settings = new() {
				ValidationType = ValidationType.None // Disable schema validation
			};

			Terrain terrain = null;
			try {
				// Use XmlReader for reading and detecting issues in the XML
				using XmlReader reader = XmlReader.Create(filePath, settings);
				// Try to deserialize the Terrain object

				// Deserialize manually so that we can handle individual errors
				var serializer = new XmlSerializer(typeof(Terrain));
				try {
					terrain = (Terrain)serializer.Deserialize(reader);
				}
				catch (InvalidOperationException ex) {
					// Capture deserialization errors
					errorMessages.Add($"{ex.Message} {ex.InnerException.Message}");
				}
			}
			catch (XmlException xmlEx) {
				// Capture XML errors such as invalid tags or malformed XML with line and position
				errorMessages.Add($"XML Error at Line {xmlEx.LineNumber}, Position {xmlEx.LinePosition}: {xmlEx.Message}");
			}
			catch (Exception ex) {
				// Catch any other unexpected exceptions
				errorMessages.Add($"Unexpected Error: {ex.Message}");
			}

			// If errors were found, report them
			if (errorMessages.Count > 0) {
				foreach (var errorMessage in errorMessages) {
					OverlayConsole.AddMessage($"Error: {errorMessage}");
				}
				OverlayConsole.AddMessage($"There are errors. Import process cancelled.");
				return null;
			}
			return terrain;
		}
	}
}
