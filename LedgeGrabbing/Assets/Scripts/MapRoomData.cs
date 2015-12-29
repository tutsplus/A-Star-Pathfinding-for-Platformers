using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum RoomEntranceType
{
	None = 0,
    Top = 1,
	Left = 2,
    Bottom = 4,
	Right = 8,
}

[System.Serializable]
public class RoomEntrance
{
	public RoomEntranceType type;
	public int begX, begY;
	public int length;
}

public enum ObjectType
{
    None,
}

[System.Serializable]
public struct ObjectTile
{
	public ObjectTile(int x, int y, ObjectType objectType)
	{
		this.x = x;
		this.y = y;
		type = objectType;
	}
	
	public int x;
	public int y;
    public ObjectType type;
}

[System.Serializable]
public class MapRoomData : ScriptableObject
{
	public int height;
	public int width;

	public List<RoomEntrance> entrances = new List<RoomEntrance>();
	public List<ObjectTile> objectTiles = new List<ObjectTile>();
    public List<ObjectTile> secondWave = new List<ObjectTile>();
    public Vector2i floorEntrance;

	public TileType entranceFill;
	public TileType[] tileData;
    public TileType[] altTileData;
	public TileType[] bgTileData;

    public byte[] altTileDataMask;

    public MapRoomData mirroredRoom;
}
