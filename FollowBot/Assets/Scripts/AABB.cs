using UnityEngine;
using System.Collections;

[System.Serializable]
public class AABB
{
	/// <summary>
	/// The center of the AABB.
	/// </summary>
	[HideInInspector]
	private Vector2 center;
	/// <summary>
	/// The half size of the AABB.
	/// </summary>
	private Vector2 halfSize;
	
    public Vector2 Center
    {
        get { return center; }
        set { center = value; }
    }

    public float CenterX
    {
        get { return center.x; }
        set { center.x = value; }
    }
    public float CenterY
    {
        get { return center.y; }
        set { center.y = value; }
    }

    public Vector2 HalfSize
    {
        get { return halfSize; }
        set { halfSize = value; }
    }

    public float HalfSizeX
    {
        get { return halfSize.x; }
        set { halfSize.x = value; }
    }

    public float HalfSizeY
    {
        get { return halfSize.y; }
        set { halfSize.y = value; }
    }

	public AABB()
	{
	}
	
	public AABB(Vector2 center, Vector2 halfSize) 
	{
		this.center = center;
		this.halfSize = halfSize;
	}
	
	/// <summary>
	/// Overlaps the specified other Axis Aligned Bounding Box.
	/// </summary>
	/// <param name='other'>
	/// The AABB to test against.
	/// </param>
	public bool Overlaps(AABB other)
	{
		if ( Mathf.Abs(center.x - other.center.x) > halfSize.x + other.halfSize.x ) return false;
		if ( Mathf.Abs(center.y - other.center.y) > halfSize.y + other.halfSize.y ) return false;
		return true;
	}
	
	public bool Overlaps(Vector2 otherCenter, Vector2 otherHalfSize)
	{
		if ( Mathf.Abs(center.x - otherCenter.x) > halfSize.x + otherHalfSize.x ) return false;
		if ( Mathf.Abs(center.y - otherCenter.y) > halfSize.y + otherHalfSize.y ) return false;
		return true;
	}


	
	/// <summary>
	/// Overlaps the specified other Axis Aligned Bounding Box.
	/// </summary>
	/// <param name='other'>
	/// The AABB to test against.
	/// </param>
	/// <param name='overlapWidth'>
	/// The signed overlap width.
	/// </param>
	/// <param name='overlapHeight'>
	/// The signed overlap height.
	/// </param>
	public bool Overlaps(AABB other, out float overlapWidth, out float overlapHeight)
	{
		overlapWidth = overlapHeight = 0;
		if ( Mathf.Abs(center.x - other.center.x) > halfSize.x + other.halfSize.x ) return false;
		if ( Mathf.Abs(center.y - other.center.y) > halfSize.y + other.halfSize.y ) return false;
		overlapWidth = (other.halfSize.x + halfSize.x) - Mathf.Abs(center.x - other.center.x);
		overlapHeight = (other.halfSize.y + halfSize.y) - Mathf.Abs(center.y - other.center.y);
		return true;
	}
	
	/// <summary>
	/// Checks for overlap between AABBs a and b.
	/// </summary>
	/// <param name='a'>
	/// A reference to the first AABB.
	/// </param>
	/// <param name='b'>
	/// A reference to the second AABB.
	/// </param>
	public static bool Overlaps(AABB a, AABB b)
	{
		if ( Mathf.Abs(a.center.x - b.center.x) > a.halfSize.x + b.halfSize.x ) return false;
		if ( Mathf.Abs(a.center.y - b.center.y) > a.halfSize.y + b.halfSize.y ) return false;
		return true;
	}
	
	public static bool Overlaps(AABB a, Vector2 otherCenter, Vector2 otherHalfSize)
	{
		if ( Mathf.Abs(a.center.x - otherCenter.x) > a.halfSize.x + otherHalfSize.x ) return false;
		if ( Mathf.Abs(a.center.y - otherCenter.y) > a.halfSize.y + otherHalfSize.y ) return false;
		return true;
	}
	
	public static bool Overlaps(AABB a, AABB b, out float overlapWidth, out float overlapHeight)
	{
		overlapWidth = overlapHeight = 0;
		if ( Mathf.Abs(a.center.x - b.center.x) > a.halfSize.x + b.halfSize.x ) return false;
		if ( Mathf.Abs(a.center.y - b.center.y) > a.halfSize.y + b.halfSize.y ) return false;
		overlapWidth = (b.halfSize.x + a.halfSize.x) - Mathf.Abs(a.center.x - b.center.x);
		overlapHeight = (b.halfSize.y + a.halfSize.y) - Mathf.Abs(a.center.y - b.center.y);
		return true;
	}
	
	/// <summary>
	/// Checks whether a Vector2i p lies inside AABB a.
	/// </summary>
	/// <returns>
	/// True if the Vector2i is inside the aabb, otherwise false.
	/// </returns>
	/// <param name='a'>
	/// If set to <c>true</c> a.
	/// </param>
	/// <param name='p'>
	/// If set to <c>true</c> p.
	/// </param>
	public static bool PointInside(AABB a, Vector2 p)
	{
		if ( Mathf.Abs(a.center.x - p.x) > a.halfSize.x ) return false;
		if ( Mathf.Abs(a.center.y - p.y) > a.halfSize.y ) return false;
		return true;
	}
}
