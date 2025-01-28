using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace GaneshaDx.Common;

public enum ResourceType {
	InitialMeshData,
	OverrideMeshData,
	AlternateStateMehData,
	Texture,
	Padded,
	UnknownExtraDataA,
	UnknownExtraDataB,
	UnknownTwin,
	UnknownTrailingData,
	BadFormat
}

public enum MapWeather {
	None,
	NoneAlt,
	Normal,
	Strong,
	VeryStrong
}

public enum MapTime {
	Day,
	Night
}

public enum MapArrangementState {
	Primary,
	Secondary
}

public enum Axis {
	X,
	Y,
	Z,
	None
}
	
public enum WidgetSelectionMode {
	Select,
	PolygonTranslate,
	PolygonEdgeTranslate,
	PolygonVertexTranslate,
	PolygonRotate
}

public enum TextureAnimationType {
	UvAnimation,
	PaletteAnimation,
	UnknownAnimation,
	None
}
	
public enum UvAnimationMode {
	ForwardLooping,
	ForwardAndReverseLooping,
	ForwardOnceOnTrigger,
	ReverseOnceOnTrigger,
	Disabled,
	Unknown
}

public enum PaletteAnimationMode {
	ForwardLooping,
	ForwardAndReverseLooping,
	ForwardOnceOnTrigger,
	ForwardLoopingOnTrigger,
	Unknown
}
	
public enum MeshType {
	PrimaryMesh,
	AnimatedMesh1,
	AnimatedMesh2,
	AnimatedMesh3,
	AnimatedMesh4,
	AnimatedMesh5,
	AnimatedMesh6,
	AnimatedMesh7,
	AnimatedMesh8
}

public enum PolygonType {
	TexturedTriangle,
	TexturedQuad,
	UntexturedTriangle,
	UntexturedQuad,
}

public enum TerrainSurfaceType {
	[EnumMember(Value = "NaturalSurface")]
	NaturalSurface,
	[EnumMember(Value = "SandArea")]
	SandArea,
	[EnumMember(Value = "Stalactite")]
	Stalactite,
	[EnumMember(Value = "Grassland")]
	Grassland,
	[EnumMember(Value = "Thicket")]
	Thicket,
	[EnumMember(Value = "Snow")]
	Snow,
	[EnumMember(Value = "RockyCliff")]
	RockyCliff,
	[EnumMember(Value = "Gravel")]
	Gravel,
	[EnumMember(Value = "Wasteland")]
	Wasteland,
	[EnumMember(Value = "Swamp")]
	Swamp,
	[EnumMember(Value = "Marsh")]
	Marsh,
	[EnumMember(Value = "PoisonedMarsh")]
	PoisonedMarsh,
	[EnumMember(Value = "LavaRocks")]
	LavaRocks,
	[EnumMember(Value = "Ice")]
	Ice,
	[EnumMember(Value = "Waterway")]
	Waterway,
	[EnumMember(Value = "River")]
	River,
	[EnumMember(Value = "Lake")]
	Lake,
	[EnumMember(Value = "Sea")]
	Sea,
	[EnumMember(Value = "Lava")]
	Lava,
	[EnumMember(Value = "Road")]
	Road,
	[EnumMember(Value = "WoodenFloor")]
	WoodenFloor,
	[EnumMember(Value = "StoneFloor")]
	StoneFloor,
	[EnumMember(Value = "Roof")]
	Roof,
	[EnumMember(Value = "StoneWall")]
	StoneWall,
	[EnumMember(Value = "Sky")]
	Sky,
	[EnumMember(Value = "Darkness")]
	Darkness,
	[EnumMember(Value = "Salt")]
	Salt,
	[EnumMember(Value = "Book")]
	Book,
	[EnumMember(Value = "Obstacle")]
	Obstacle,
	[EnumMember(Value = "Rug")]
	Rug,
	[EnumMember(Value = "Tree")]
	Tree,
	[EnumMember(Value = "Box")]
	Box,
	[EnumMember(Value = "Brick")]
	Brick,
	[EnumMember(Value = "Chimney")]
	Chimney,
	[EnumMember(Value = "MudWall")]
	MudWall,
	[EnumMember(Value = "Bridge")]
	Bridge,
	[EnumMember(Value = "WaterPlant")]
	WaterPlant,
	[EnumMember(Value = "Stairs")]
	Stairs,
	[EnumMember(Value = "Furniture")]
	Furniture,
	[EnumMember(Value = "Ivy")]
	Ivy,
	[EnumMember(Value = "Deck")]
	Deck,
	[EnumMember(Value = "Machine")]
	Machine,
	[EnumMember(Value = "IronPlate")]
	IronPlate,
	[EnumMember(Value = "Moss")]
	Moss,
	[EnumMember(Value = "Tombstone")]
	Tombstone,
	[EnumMember(Value = "Waterfall")]
	Waterfall,
	[EnumMember(Value = "Coffin")]
	Coffin,
	[EnumMember(Value = "FftbgPool")]
	FftbgPool,
	[EnumMember(Value = "UnusedX30")]
	UnusedX30,
	[EnumMember(Value = "UnusedX31")]
	UnusedX31,
	[EnumMember(Value = "UnusedX32")]
	UnusedX32,
	[EnumMember(Value = "UnusedX33")]
	UnusedX33,
	[EnumMember(Value = "UnusedX34")]
	UnusedX34,
	[EnumMember(Value = "UnusedX35")]
	UnusedX35,
	[EnumMember(Value = "UnusedX36")]
	UnusedX36,
	[EnumMember(Value = "UnusedX37")]
	UnusedX37,
	[EnumMember(Value = "UnusedX38")]
	UnusedX38,
	[EnumMember(Value = "UnusedX39")]
	UnusedX39,
	[EnumMember(Value = "UnusedX3A")]
	UnusedX3A,
	[EnumMember(Value = "UnusedX3B")]
	UnusedX3B,
	[EnumMember(Value = "UnusedX3C")]
	UnusedX3C,
	[EnumMember(Value = "UnusedX3D")]
	UnusedX3D,
	[EnumMember(Value = "UnusedX3E")]
	UnusedX3E,
	[EnumMember(Value = "CrossSection")]
	CrossSection
}

public enum TerrainSlopeType {
	[EnumMember(Value = "Flat")]
	Flat,
	[EnumMember(Value = "InclineNorth")]
	InclineNorth,
	[EnumMember(Value = "InclineEast")]
	InclineEast,
	[EnumMember(Value = "InclineSouth")]
	InclineSouth,
	[EnumMember(Value = "InclineWest")]
	InclineWest,
	[EnumMember(Value = "ConvexNortheast")]
	ConvexNortheast,
	[EnumMember(Value = "ConvexSoutheast")]
	ConvexSoutheast,
	[EnumMember(Value = "ConvexSouthwest")]
	ConvexSouthwest,
	[EnumMember(Value = "ConvexNorthwest")]
	ConvexNorthwest,
	[EnumMember(Value = "ConcaveNortheast")]
	ConcaveNortheast,
	[EnumMember(Value = "ConcaveSoutheast")]
	ConcaveSoutheast,
	[EnumMember(Value = "ConcaveSouthwest")]
	ConcaveSouthwest,
	[EnumMember(Value = "ConcaveNorthwest")]
	ConcaveNorthwest
}


public enum TerrainDarkness {
	[EnumMember(Value = "Normal")]
	Normal,
	[EnumMember(Value = "Dark")]
	Dark,
	[EnumMember(Value = "Darker")]
	Darker,
	[EnumMember(Value = "Darkest")]
	Darkest
}

public enum MeshAnimationTweenType {
	TweenTo,
	TweenBy,
	Oscillate,
	OscillateOffset,
	Unk9,
	Unk17,
	Invalid,
	Unknown
}