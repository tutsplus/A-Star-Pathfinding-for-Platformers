using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#region Vector2i

[System.Serializable]
public struct Vector2i : IEquatable<Vector2i>
{
	public int x, y;
	
	public Vector2i(int _x, int _y)
	{
		x = _x;
		y = _y;
	}

    public static implicit operator Vector2 (Vector2i v)
    {
        return new Vector2(v.x, v.y);
    }

    public static Vector2i operator +(Vector2i v, Vector2i v2)
    {
        return new Vector2i(v.x + v2.x, v.y + v2.y);
    }

    public static bool operator ==(Vector2i v, Vector2i v2)
    {
        return (v.x == v2.x && v.y == v2.y);
    }

    public static bool operator !=(Vector2i v, Vector2i v2)
    {
        return (v.x != v2.x || v.y != v2.y);
    }

    public bool Equals(Vector2i other)
    {
        return x == other.x && y == other.y;
    }
}

class Vector2iEqualityComparer : IEqualityComparer<Vector2i>
{
	public bool Equals(Vector2i v1, Vector2i v2)
	{
		return (v1.x == v2.x && v1.y == v2.y);
	}
	
	public int GetHashCode(Vector2i v)
	{
		return v.x*7 + v.y*13;
	}
}

#endregion