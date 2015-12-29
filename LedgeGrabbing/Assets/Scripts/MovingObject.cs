using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MovingObject : MonoBehaviour 
{
	/// <summary>
	/// The previous position.
	/// </summary>
	public Vector2 mOldPosition;
	/// <summary>
	/// The current position.
	/// </summary>
	public Vector2 mPosition;
    public Vector2 mScale;

	/// <summary>
	/// The current speed in pixels/second.
	/// </summary>
	public Vector2 mSpeed;
	
	/// <summary>
	/// The previous speed in pixels/second.
	/// </summary>
	public Vector2 mOldSpeed;

    public Vector2 mAABBOffset;

	/// <summary>
	/// The AABB for collision queries.
	/// </summary>
	public AABB mAABB;
	
	/// <summary>
	/// The tile map.
	/// </summary>
	public Map mMap;

	/// <summary>
	/// True if the instance is right beside the right wall.
	/// </summary>
	//[HideInInspector]
	public bool mPushesRightWall = false;
	/// <summary>
	/// True if the instance is right beside the left wall.
	/// </summary>
	//[HideInInspector]
	public bool mPushesLeftWall = false;
	/// <summary>
	/// True if the instance is on the ground.
	/// </summary>
	//[HideInInspector]
	public bool mOnGround = false;
	/// <summary>
	/// True if the instance hits the ceiling.
	/// </summary>
	//[HideInInspector]
	public bool mAtCeiling = false;
	/// <summary>
	/// The previous state of atCeiling.
	/// </summary>
	//[HideInInspector]
	public bool mWasAtCeiling = false;
	/// <summary>
	/// The previous state of onGround.
	/// </summary>
	//[HideInInspector]
	public bool mWasOnGround = false;
	/// <summary>
	/// The previous state of pushesRightWall.
	/// </summary>
	//[HideInInspector]
	public bool mPushedRightWall = false;
	/// <summary>
	/// The previous state of pushesLeftWall.
	/// </summary>
	//[HideInInspector]
	public bool mPushedLeftWall = false;
	
	public bool mOnOneWayPlatform = false;
	
	/// <summary>
	/// Depth for z-ordering the sprites.
	/// </summary>
	public float mSpriteDepth = -1.0f;
	
	/// <summary>
	/// If the object is colliding with one way platform tile and the distance to the tile's top is less
	/// than this threshold, then the object will be aligned to the one way platform.
	/// </summary>
	public float cOneWayPlatformThreshold = 2.0f;

    public bool mIgnoresOneWayPlatforms = false;

	void OnDrawGizmos()
	{
		DrawMovingObjectGizmos ();
	}

	/// <summary>
	/// Draws the aabb and ceiling, ground and wall sensors .
	/// </summary>
	protected void DrawMovingObjectGizmos()
	{
		//calculate the position of the aabb's center
		var aabbPos = transform.position + (Vector3)mAABBOffset;
		
		//draw the aabb rectangle
		Gizmos.color = Color.yellow;
   		Gizmos.DrawWireCube(aabbPos, mAABB.HalfSize*2.0f);
		
		//draw the ground checking sensor
		Vector2 bottomLeft = aabbPos - new Vector3(mAABB.HalfSizeX, mAABB.HalfSizeY, 0.0f) - Vector3.up + Vector3.right;
		var bottomRight = new Vector2(bottomLeft.x + mAABB.HalfSizeX*2.0f - 2.0f, bottomLeft.y);
		
		Gizmos.color = Color.red;
		Gizmos.DrawLine(bottomLeft, bottomRight);
		
		//draw the ceiling checking sensor
		Vector2 topRight = aabbPos + new Vector3(mAABB.HalfSize.x, mAABB.HalfSize.y, 0.0f) + Vector3.up - Vector3.right;
		var topLeft = new Vector2(topRight.x - mAABB.HalfSize.x*2.0f + 2.0f, topRight.y);
		
		Gizmos.color = Color.red;
		Gizmos.DrawLine(topLeft, topRight);
		
		//draw left wall checking sensor
		bottomLeft = aabbPos - new Vector3(mAABB.HalfSize.x, mAABB.HalfSize.y, 0.0f) - Vector3.right;
		topLeft = bottomLeft;
		topLeft.y += mAABB.HalfSize.y * 2.0f;
		
		Gizmos.DrawLine(topLeft, bottomLeft);
		
		//draw right wall checking sensor
		
		bottomRight = aabbPos + new Vector3(mAABB.HalfSize.x, -mAABB.HalfSize.y, 0.0f) + Vector3.right;
		topRight = bottomRight;
		topRight.y += mAABB.HalfSize.y * 2.0f;
		
		Gizmos.DrawLine(topRight, bottomRight);
	}

	/// <summary>
	/// Determines whether there's ceiling right above the hero.
	/// </summary>
	/// <returns>
	/// <c>true</c> if there is ceiling right above the hero; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='ceilY'>
	/// The position of the bottom of the ceiling tile in world coordinates.
	/// </param>
	public bool HasCeiling(Vector2 position, out float ceilingY)
	{
		//make sure the aabb is up to date with the position
		var center = position + mAABBOffset;
		
		//init the groundY
		ceilingY = 0.0f;
		
		//set the Vector2is right below us on our left and right sides
		var topRight = center + mAABB.HalfSize + Vector2.up - Vector2.right;
		var topLeft = new Vector2(topRight.x - mAABB.HalfSize.x*2.0f + 2.0f, topRight.y);
		
		//get the indices of a tile below us on our left side
		int tileIndexX, tileIndexY; 
		
		//iterate over all the tiles that the object may collide with from the left to the right
		for (var checkedVector2i = topLeft; checkedVector2i.x < topRight.x + Map.cTileSize; checkedVector2i.x += Map.cTileSize)
		{
			//makre sure that we don't check beyound the top right corner
			checkedVector2i.x = Mathf.Min(checkedVector2i.x, topRight.x);
			
			mMap.GetMapTileAtPoint (checkedVector2i, out tileIndexX, out tileIndexY);
			
			if (tileIndexY < 0 || tileIndexY >= mMap.mHeight) return false;
			if (tileIndexX < 0 || tileIndexX >= mMap.mWidth) return false;
			
			//if below this tile there is another tile, that means we can't possibly
			//hit it without hitting the one below, so we can immidiately skip to the topRight corner check
			if (!mMap.IsObstacle(tileIndexX, tileIndexY - 1))
			{
				//if the tile is not empty, it means we have ceiling right above us
                if (mMap.IsObstacle(tileIndexX, tileIndexY))
				{
					//calculate the y position of the bottom of the ceiling tile
					ceilingY = (float)tileIndexY * Map.cTileSize - Map.cTileSize/2.0f + mMap.position.y;
					return true;
				}
			}
			
			//if we checked all the possible tiles and there's nothing right above the aabb
			if (checkedVector2i.x == topRight.x)
				return false;
		}
		
		//there's nothing right above the aabb
		return false; 
	}
	
	/// <summary>
	/// Determines whether there's ground right below the hero.
	/// </summary>
	/// <returns>
	/// <c>true</c> if there is ground right below the hero; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='groundY'>
	/// The position of the top of the ground tile in world coordinates.
	/// </param>
	public bool HasGround(Vector2 position, out float groundY)
	{
		//make sure the aabb is up to date with the position
        var center = position + mAABBOffset;
		
		//init the groundY
		groundY = 0.0f;
		
		//set the Vector2is right below us on our left and right sides
		var bottomLeft = center - mAABB.HalfSize - Vector2.up + Vector2.right;
		var bottomRight = new Vector2(bottomLeft.x + mAABB.HalfSize.x*2.0f - 2.0f, bottomLeft.y);
		
		//left side
		//calculate the indices of a tile below us on our left side
		int tileIndexX, tileIndexY; 
		
		//iterate over all the tiles that the object may collide with from the left to the right
		for (var checkedVector2i = bottomLeft; checkedVector2i.x < bottomRight.x + Map.cTileSize; checkedVector2i.x += Map.cTileSize)
		{
			//makre sure that we don't check beyound the bottom right corner
			checkedVector2i.x = Mathf.Min(checkedVector2i.x, bottomRight.x);
			
			mMap.GetMapTileAtPoint (checkedVector2i, out tileIndexX, out tileIndexY);
			
			if (tileIndexY < 0 || tileIndexY >= mMap.mHeight) return false;
			if (tileIndexX < 0 || tileIndexX >= mMap.mWidth) return false;
			
			//if above this tile there is another tile, that means we can't possibly
			//hit it without hitting the one above
			if (!mMap.IsObstacle(tileIndexX, tileIndexY + 1))
			{
				var floorTop = (float)tileIndexY * Map.cTileSize + Map.cTileSize/2.0f + mMap.position.y;
				//if the tile is not empty, it means we have a floor right below us
                if (mMap.IsObstacle(tileIndexX, tileIndexY))
				{
					//calculate the y position of the floor tile's top
					groundY = floorTop;
					return true;
				}//if there's a one way platform below us, treat it as a floor only if we're falling or standing
				else if ((mMap.IsOneWayPlatform(tileIndexX, tileIndexY) && !mIgnoresOneWayPlatforms) && mSpeed.y <= 0.0f
						&& Mathf.Abs(checkedVector2i.y - floorTop) <= cOneWayPlatformThreshold + mOldPosition.y - position.y)
				{
					groundY = floorTop;
					mOnOneWayPlatform = true;
				}
			}
			
			//if we checked all the possible tiles and there's nothing right below the aabb
			if (checkedVector2i.x == bottomRight.x)
			{
				if (mOnOneWayPlatform)
					return true;
				return false;
			}
		}
		
		//there's nothing right beneath the aabb
		return false; 
	}
	
	/// <summary>
	/// Checks if the hero collides with a wall on the right.
	/// </summary>
	/// <returns>
	/// True if the hero collides with the wall on the right, otherwise false.
	/// </returns>
	/// <param name='wallX'>
	/// The X coordinate in world space of the left edge of the wall the hero collides with.
	/// </param>
	public bool CollidesWithRightWall(Vector2 position, out float wallX)
	{
		//make sure the aabb is up to date with the position
        var center = position + mAABBOffset;
		
		//init the wallX
		wallX = 0.0f;
		
		//calculate the bottom left and top left vertices of our aabb
		var bottomRight = center + new Vector2(mAABB.HalfSize.x, -mAABB.HalfSize.y) + Vector2.right;
		var topRight = bottomRight + new Vector2(0.0f, mAABB.HalfSize.y * 2.0f);
		
		//get the bottom right vertex's tile indices
		int tileIndexX, tileIndexY;
		
		//iterate over all the tiles that the object may collide with from the top to the bottom
		for (var checkedVector2i = bottomRight; checkedVector2i.y < topRight.y + Map.cTileSize; checkedVector2i.y += Map.cTileSize)
		{
			//make sure that we don't check beyound the top right corner
			checkedVector2i.y = Mathf.Min(checkedVector2i.y, topRight.y);
			
			mMap.GetMapTileAtPoint (checkedVector2i, out tileIndexX, out tileIndexY);
			
			if (tileIndexY < 0 || tileIndexY >= mMap.mHeight) return false;
			if (tileIndexX < 0 || tileIndexX >= mMap.mWidth) return false;
			
			//if the tile has another tile on the left, we can't touch the tile's left side because it's blocked
			if (!mMap.IsObstacle(tileIndexX - 1, tileIndexY))
			{
				//if the tile is not empty, then we hit the wall
                if (mMap.IsObstacle(tileIndexX, tileIndexY))
				{
					//calculate the x position of the left side of the wall
					wallX = (float)tileIndexX * Map.cTileSize - Map.cTileSize/2.0f + mMap.position.x;
					return true;
				}
			}
			
			//if we checked all the possible tiles and there's nothing right next to the aabb
			if (checkedVector2i.y == topRight.y)
				return false;
		}
		
		return false;
	}
	
	/// <summary>
	/// Checks if the hero collides with a wall on the left.
	/// </summary>
	/// <returns>
	/// True if the hero collides with the wall on the left, otherwise false.
	/// </returns>
	/// <param name='wallX'>
	/// The X coordinate in world space of the right edge of the wall the hero collides with.
	/// </param>
	public bool CollidesWithLeftWall(Vector2 position, out float wallX)
	{
		//make sure the aabb is up to date with the position
        var center = position + mAABBOffset;
		
		//init the wallX
		wallX = 0.0f;
		
		//calculate the bottom left and top left vertices of our mAABB.
		var bottomLeft = center - mAABB.HalfSize - Vector2.right;
		var topLeft = bottomLeft + new Vector2(0.0f, mAABB.HalfSize.y * 2.0f);
		
		//get the bottom left vertex's tile indices
		int tileIndexX, tileIndexY;
		
		//iterate over all the tiles that the object may collide with from the top to the bottom
		for (var checkedVector2i = bottomLeft; checkedVector2i.y < topLeft.y + Map.cTileSize; checkedVector2i.y += Map.cTileSize)
		{
			//make sure that we don't check beyound the top right corner
			checkedVector2i.y = Mathf.Min(checkedVector2i.y, topLeft.y);
			
			mMap.GetMapTileAtPoint (checkedVector2i, out tileIndexX, out tileIndexY);
			
			if (tileIndexY < 0 || tileIndexY >= mMap.mHeight) return false;
			if (tileIndexX < 0 || tileIndexX >= mMap.mWidth) return false;
			
			//if the tile has another tile on the right, we can't touch the tile's right side because it's blocked
			if (!mMap.IsObstacle(tileIndexX + 1, tileIndexY))
			{
				//if the tile is not empty, then we hit the wall
                if (mMap.IsObstacle(tileIndexX, tileIndexY))
				{
					//calculate the x position of the right side of the wall
					wallX = (float)tileIndexX * Map.cTileSize + Map.cTileSize/2.0f + mMap.position.x;
					return true;
				}
			}
			
			//if we checked all the possible tiles and there's nothing right next to the aabb
			if (checkedVector2i.y == topLeft.y)
				return false;
		}
		
		return false;
	}

	/// <summary>
	/// Updates the moving object's physics, integrates the movement, updates sensors for terrain collisions.
	/// </summary>
	public void UpdatePhysics()
	{	
		//assign the previous state of onGround, atCeiling, pushesRightWall, pushesLeftWall
		//before those get recalculated for this frame
		mWasOnGround = mOnGround;
		mPushedRightWall = mPushesRightWall;
		mPushedLeftWall = mPushesLeftWall;
		mWasAtCeiling = mAtCeiling;
		
		mOnOneWayPlatform = false;
		
		//save the speed to oldSpeed vector
		mOldSpeed = mSpeed;
		
		//save the position to the oldPosition vector
		mOldPosition = mPosition;
		
		//integrate the movement only if we're not tweening
		mPosition += mSpeed*Time.deltaTime;
		
		var checkAgainLeft = false;
		

		float groundY, ceilingY;
		float rightWallX = 0.0f, leftWallX = 0.0f;
		
		//if we overlap a tile on the left then align the hero
		if (mSpeed.x <= 0.0f && CollidesWithLeftWall(mPosition, out leftWallX))
		{
            if (mOldPosition.x - mAABB.HalfSize.x + mAABBOffset.x >= leftWallX)
			{
                mPosition.x = leftWallX + mAABB.HalfSize.x - mAABBOffset.x;
				mSpeed.x = Mathf.Max(mSpeed.x, 0.0f);
				
				mPushesLeftWall = true;
			}
			else
				checkAgainLeft = true;
		}
		else
			mPushesLeftWall = false;
		
		var checkAgainRight = false;
		
		//if we overlap a tile on the right then align the hero
		if (mSpeed.x >= 0.0f && CollidesWithRightWall(mPosition, out rightWallX))
		{
            if (mOldPosition.x + mAABB.HalfSize.x + mAABBOffset.x <= rightWallX)
			{
                mPosition.x = rightWallX - mAABB.HalfSize.x - mAABBOffset.x;
				mSpeed.x = Mathf.Min(mSpeed.x, 0.0f);
				
				mPushesRightWall = true;
			}
			else
				checkAgainRight = true;
		}
		else
			mPushesRightWall = false;
		
		//when we hit the ground
		//we can't hit the ground if our speed is positive
		if (HasGround(mPosition, out groundY) && mSpeed.y <= 0.0f
            && mOldPosition.y - mAABB.HalfSize.y + mAABBOffset.y >= groundY - 0.5f)
		{
			//calculate the y position on top of the ground
            mPosition.y = groundY + mAABB.HalfSize.y - mAABBOffset.y;
				
			//stop falling
			mSpeed.y = 0.0f;

			//we are on the ground now
			mOnGround = true;
		}
		else
			mOnGround = false;
		
		//check if the hero hit the ceiling
		if (HasCeiling(mPosition, out ceilingY) && mSpeed.y >= 0.0f
            && mOldPosition.y + mAABB.HalfSize.y + mAABBOffset.y + 1.0f <= ceilingY)
		{
            mPosition.y = ceilingY - mAABB.HalfSize.y - mAABBOffset.y - 1.0f;
				
			//stop going up
			mSpeed.y = 0.0f;
			
			mAtCeiling = true;
		}
		else
			mAtCeiling = false;
		
		//if we are colliding with the block but we don't know from which side we had hit him, just prioritize the horizontal alignment
		if (checkAgainLeft && !mOnGround && !mAtCeiling)
		{
			mPosition.x = leftWallX + mAABB.HalfSize.x;
			mSpeed.x = Mathf.Max(mSpeed.x, 0.0f);

			mPushesLeftWall = true;
		}
		else if (checkAgainRight && !mOnGround && !mAtCeiling)
		{
			mPosition.x = rightWallX - mAABB.HalfSize.x;
			mSpeed.x = Mathf.Min(mSpeed.x, 0.0f);

			mPushesRightWall = true;
		}
		
		//update the aabb
        mAABB.Center = mPosition + mAABBOffset;
		
		//apply the changes to the transform
		transform.position = new Vector3(Mathf.Round(mPosition.x), Mathf.Round(mPosition.y), mSpriteDepth);
	}
}