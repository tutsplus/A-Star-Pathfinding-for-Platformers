using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Algorithms;
using UnityEngine.UI;

public class Bot : Character
{
	public enum BotState
	{
		None = 0,
		MoveTo,
	}
	
	public BotState mCurrentBotState = BotState.None;
	
	public Vector2 mDestination;
	
	public int mCurrentNodeId = -1;

	public int mFramesOfJumping = 0;
	public int mStuckFrames = 0;

    public int mMaxJumpHeight = 5;
	
	public const int cMaxStuckFrames = 20;

    public bool mReachedNodeX;
    public bool mReachedNodeY;

    public bool mGrabsLedges = false;
    bool mMustGrabLeftLedge;
    bool mMustGrabRightLedge;
    bool mCanGrabLedge = false;

    public void TappedOnTile(Vector2i mapPos)
    {
        while (!(mMap.IsGround(mapPos.x, mapPos.y)))
            --mapPos.y;

        MoveTo(new Vector2i(mapPos.x, mapPos.y + 1));
    }

    public void BotInit(bool[] inputs, bool[] prevInputs)
    {
        mWidth = 1;
        mHeight = 3;
        mScale = Vector2.one;

        mInputs = inputs;
        mPrevInputs = prevInputs;

        mAudioSource = GetComponent<AudioSource>();
        mPosition = transform.position;

        mAABB.HalfSize = new Vector2(Constants.cHalfSizes[mWidth - 1], Constants.cHalfSizes[mHeight - 1]);

        mJumpSpeed = Constants.cJumpSpeed[mMaxJumpHeight - 1];
        mWalkSpeed = Constants.cWalkSpeed;

        mAABBOffset.y = mAABB.HalfSizeY;
        mLedgeGrabOffset = 4.0f;
        //transform.localScale = new Vector3(mAABB.HalfSizeX / 8.0f, mAABB.HalfSizeY / 8.0f, 1.0f);
    }

    public void SetJumpHeight(Slider slider)
    {
        mMaxJumpHeight = (int)slider.value;
        mJumpSpeed = Constants.cJumpSpeed[mMaxJumpHeight - 1];
    }

    bool mLazyLedgeGrabs = false;
    public void SetLazyLedgeGrabs(Slider slider)
    {
        mLazyLedgeGrabs = (int)slider.value != 0;
    }

    public void SetCharacterWidth(Slider slider)
    {
        mWidth = (int)slider.value;

        mPosition = transform.position;

        mScale.x = Mathf.Sign(mScale.x) * (float)mWidth;
        transform.localScale = new Vector3(mScale.x, mScale.y, 1.0f);
        
        mAABB.HalfSizeX = Constants.cHalfSizes[mWidth - 1];
    }

    public void SetCharacterHeight(Slider slider)
    {
        mHeight = (int)slider.value;

        mPosition = transform.position;

        mScale.y = (float)mHeight * 0.33333f;
        transform.localScale = new Vector3(mScale.x, mScale.y, 1.0f);

        mAABB.HalfSizeY = Constants.cHalfSizes[mHeight - 1];

        mAABBOffset.y = mAABB.HalfSizeY;
    }

    bool IsOnGroundAndFitsPos(Vector2i pos)
    {
        for (int y = pos.y; y < pos.y + mHeight; ++y)
        {
            for (int x = pos.x; x < pos.x + mWidth; ++x)
            {
                if (mMap.IsObstacle(x, y))
                    return false;
            }
        }

        for (int x = pos.x; x < pos.x + mWidth; ++x)
        {
            if (mMap.IsGround(x, pos.y - 1))
                return true;
        }

        return false;
    }
    public void MoveTo(Vector2i destination)
    {
        mStuckFrames = 0;

        Vector2i startTile = mMap.GetMapTileAtPoint(mAABB.Center - mAABB.HalfSize + Vector2.one * Map.cTileSize * 0.5f);
        
        if (mOnGround && !IsOnGroundAndFitsPos(startTile))
        {
            if (IsOnGroundAndFitsPos(new Vector2i(startTile.x + 1, startTile.y)))
                startTile.x += 1;
            else
                startTile.x -= 1;
        }

        List<Vector2i> path1 = null;
        var path = mMap.mPathFinder.FindPath(
                        startTile,
                        destination,
                        Mathf.CeilToInt(mAABB.HalfSizeX / 8.0f),
                        Mathf.CeilToInt(mAABB.HalfSizeY / 8.0f),
                        (short)mMaxJumpHeight, false);

        if (path != null)
        {
            path1 = new List<Vector2i>();
            path1.AddRange(path);
        }


        var path2 = mMap.mPathFinder.FindPath(
                        startTile,
                        destination,
                        Mathf.CeilToInt(mAABB.HalfSizeX / 8.0f),
                        Mathf.CeilToInt(mAABB.HalfSizeY / 8.0f),
                        (short)mMaxJumpHeight, true);

        path = path2;
        mGrabsLedges = true;

        if (mLazyLedgeGrabs && path1 != null && path1.Count <= path2.Count + 6)
        {
            path = path1;
            mGrabsLedges = false;
        }

        mPath.Clear();

        if (path != null && path.Count > 1)
        {
            for (var i = path.Count - 1; i >= 0; --i)
                mPath.Add(path[i]);

            mCurrentNodeId = 1;
            mReachedNodeX = false;
            mReachedNodeY = false;
            mCanGrabLedge = false;

            ChangeState(BotState.MoveTo);

            mFramesOfJumping = GetJumpFramesForNode(0, mGrabsLedges);
        }
        else
        {
            mCurrentNodeId = -1;

            if (mCurrentBotState == BotState.MoveTo)
                mCurrentBotState = BotState.None;
        }

        if (!Debug.isDebugBuild)
            DrawPathLines();
    }

    public void MoveTo(Vector2 destination)
    {
        MoveTo(mMap.GetMapTileAtPoint(destination));
    }


    public void ChangeState(BotState newState)
    {
        mCurrentBotState = newState;
    }

    int GetJumpFrameCount(int deltaY)
    {
        if (deltaY <= 0)
            return 0;
        else
        {
            switch (deltaY)
            {
                case 1:
                    return 1;
                case 2:
                    return 2;
                case 3:
                    return 6;
                case 4:
                    return 9;
                case 5:
                    return 15;
                case 6:
                    return 21;
                default:
                    return 30;
            }
        }
    }

    public bool ReachedNodeOnXAxis(Vector2 pathPosition, Vector2 prevDest, Vector2 currentDest)
    {
        return (prevDest.x <= currentDest.x && pathPosition.x >= currentDest.x)
            || (prevDest.x >= currentDest.x && pathPosition.x <= currentDest.x)
            || Mathf.Abs(pathPosition.x - currentDest.x) <= Constants.cBotMaxPositionError;
    }

    public bool ReachedNodeOnYAxis(Vector2 pathPosition, Vector2 prevDest, Vector2 currentDest)
    {
        return (prevDest.y <= currentDest.y && pathPosition.y >= currentDest.y)
            || (prevDest.y >= currentDest.y && pathPosition.y <= currentDest.y)
            || (Mathf.Abs(pathPosition.y - currentDest.y) <= Constants.cBotMaxPositionError);
    }

    public void GetContext(out Vector2 prevDest, out Vector2 currentDest, out Vector2 nextDest, out bool destOnGround)
    {
        prevDest = new Vector2(mPath[mCurrentNodeId - 1].x * Map.cTileSize + mMap.transform.position.x,
                                             mPath[mCurrentNodeId - 1].y * Map.cTileSize + mMap.transform.position.y);
        currentDest = new Vector2(mPath[mCurrentNodeId].x * Map.cTileSize + mMap.transform.position.x,
                                          mPath[mCurrentNodeId].y * Map.cTileSize + mMap.transform.position.y);
        nextDest = currentDest;

        if (mPath.Count > mCurrentNodeId + 1)
        {
            nextDest = new Vector2(mPath[mCurrentNodeId + 1].x * Map.cTileSize + mMap.transform.position.x,
                                          mPath[mCurrentNodeId + 1].y * Map.cTileSize + mMap.transform.position.y);
        }

        destOnGround = false;
        for (int x = mPath[mCurrentNodeId].x; x < mPath[mCurrentNodeId].x + mWidth; ++x)
        {
            if (mMap.IsGround(x, mPath[mCurrentNodeId].y - 1))
            {
                destOnGround = true;
                break;
            }
        }

        Vector2 pathPosition = mAABB.Center - mAABB.HalfSize + Vector2.one * Map.cTileSize * 0.5f;

        if (!mReachedNodeX)
            mReachedNodeX = ReachedNodeOnXAxis(pathPosition, prevDest, currentDest);

        if (!mReachedNodeY)
            mReachedNodeY = ReachedNodeOnYAxis(pathPosition, prevDest, currentDest);

        //snap the character if it reached the goal but overshot it by more than cBotMaxPositionError
        if (mReachedNodeX && Mathf.Abs(pathPosition.x - currentDest.x) > Constants.cBotMaxPositionError && Mathf.Abs(pathPosition.x - currentDest.x) < Constants.cBotMaxPositionError*3.0f && !mPrevInputs[(int)KeyInput.GoRight] && !mPrevInputs[(int)KeyInput.GoLeft] && !mCanGrabLedge)
        {
            pathPosition.x = currentDest.x;
            mPosition.x = pathPosition.x - Map.cTileSize * 0.5f + mAABB.HalfSizeX + mAABBOffset.x;
        }

        if ((destOnGround && !mOnGround) 
            || ((mMustGrabLeftLedge || mMustGrabRightLedge) && mCurrentState != CharacterState.GrabLedge))
            mReachedNodeY = false;

        mMustGrabLeftLedge = mGrabsLedges && !destOnGround && CanGrabLedgeOnLeft(mCurrentNodeId);
        mMustGrabRightLedge = mGrabsLedges && !destOnGround && CanGrabLedgeOnRight(mCurrentNodeId);
    }

    public void TestJumpValues()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            mFramesOfJumping = GetJumpFrameCount(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            mFramesOfJumping = GetJumpFrameCount(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            mFramesOfJumping = GetJumpFrameCount(3);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            mFramesOfJumping = GetJumpFrameCount(4);
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            mFramesOfJumping = GetJumpFrameCount(5);
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            mFramesOfJumping = GetJumpFrameCount(6);
    }

    public int GetJumpFramesForNode(int prevNodeId, bool grabLedges)
    {
        int currentNodeId = prevNodeId + 1;

        if ((mPath[currentNodeId].y - mPath[prevNodeId].y > 0 
                || (mPath[currentNodeId].y - mPath[prevNodeId].y == 0 && !mMap.IsGround(mPath[currentNodeId].x, mPath[currentNodeId].y - 1) && mPath[currentNodeId+1].y - mPath[prevNodeId].y > 0))
            && (mOnGround || mCurrentState == CharacterState.GrabLedge))
        {
            int jumpHeight = 1;
            for (int i = currentNodeId; i < mPath.Count; ++i)
            {
                if (mPath[i].y - mPath[prevNodeId].y >= jumpHeight)
                    jumpHeight = mPath[i].y - mPath[prevNodeId].y;
                if (mPath[i].y - mPath[prevNodeId].y < jumpHeight || mMap.IsGround(mPath[i].x, mPath[i].y - 1))
                    return (GetJumpFrameCount(jumpHeight) /*+ (mCurrentState == CharacterState.GrabLedge ? 3 : 0)*/);
                else if (grabLedges && CanGrabLedge(i))
                    return (GetJumpFrameCount(jumpHeight) + 4 /*+ (mCurrentState == CharacterState.GrabLedge ? 3 : 0)*/);
            }
        }

        return mFramesOfJumping;
    }

    public bool CanGrabLedge(int nodeId)
    {
        return CanGrabLedgeOnLeft(nodeId) || CanGrabLedgeOnRight(nodeId);
    }

    public bool CanGrabLedgeOnLeft(int nodeId)
    {
        return (mMap.IsObstacle(mPath[nodeId].x - 1, mPath[nodeId].y + mHeight - 1)
            && !mMap.IsObstacle(mPath[nodeId].x - 1, mPath[nodeId].y + mHeight));
    }

    public bool CanGrabLedgeOnRight(int nodeId)
    {
        return (mMap.IsObstacle(mPath[nodeId].x + mWidth, mPath[nodeId].y + mHeight - 1)
                && !mMap.IsObstacle(mPath[nodeId].x + mWidth, mPath[nodeId].y + mHeight));
    }

    public void BotUpdate()
	{
        switch (mCurrentBotState)
        {
            case BotState.None:

                break;

            case BotState.MoveTo:

                Vector2 prevDest, currentDest, nextDest;
                bool destOnGround;
                GetContext(out prevDest, out currentDest, out nextDest, out destOnGround);
                Vector2 pathPosition = mAABB.Center - mAABB.HalfSize + Vector2.one * Map.cTileSize * 0.5f;

                mInputs[(int)KeyInput.GoRight] = false;
                mInputs[(int)KeyInput.GoLeft] = false;
                mInputs[(int)KeyInput.Jump] = false;
                mInputs[(int)KeyInput.GoDown] = false;

                if (pathPosition.y - currentDest.y > Constants.cBotMaxPositionError && mOnOneWayPlatform)
                    mInputs[(int)KeyInput.GoDown] = true;

                if (mCanGrabLedge && mCurrentState != CharacterState.GrabLedge)
                {
                    //int tileX, tileY;
                    //mMap.GetMapTileAtPoint(pathPosition, out tileX, out tileY);

                    if (mMustGrabLeftLedge /*&& mMap.AnySolidBlockInStripe(tileX - 1, tileY, tileY + mHeight - 1)*/)
                        mInputs[(int)KeyInput.GoLeft] = true;
                    else if (mMustGrabRightLedge /*&& mMap.AnySolidBlockInStripe(tileX + 1, tileY, tileY + mHeight - 1)*/)
                        mInputs[(int)KeyInput.GoRight] = true;

                }
                else if (!mCanGrabLedge && mReachedNodeX && (mMustGrabLeftLedge || mMustGrabRightLedge) &&
                    ((pathPosition.y < currentDest.y && /*currentDest.y - pathPosition.y < Map.cTileSize - 4 &&*/ (currentDest.y + Map.cTileSize*mHeight) < pathPosition.y + mAABB.HalfSizeY * 2) //approach from bottom
                    || (pathPosition.y > currentDest.y && pathPosition.y - currentDest.y < mHeight * Map.cTileSize))) // approach from top
                {
                    mCanGrabLedge = true;

                    if (mMustGrabLeftLedge)
                        mInputs[(int)KeyInput.GoLeft] = true;
                    else if (mMustGrabRightLedge)
                        mInputs[(int)KeyInput.GoRight] = true;
                }
                else if ((mReachedNodeX && mReachedNodeY) || (mCanGrabLedge && mCurrentState == CharacterState.GrabLedge))
                {
                    int prevNodeId = mCurrentNodeId;
                    mCurrentNodeId++;
                    mReachedNodeX = false;
                    mReachedNodeY = false;
                    mCanGrabLedge = false;

                    if (mCurrentNodeId >= mPath.Count)
                    {
                        mCurrentNodeId = -1;
                        ChangeState(BotState.None);
                        break;
                    }

                    mFramesOfJumping = GetJumpFramesForNode(prevNodeId, mGrabsLedges);

                    goto case BotState.MoveTo;
                }
                else if (!mReachedNodeX)
                {
                    if (currentDest.x - pathPosition.x > Constants.cBotMaxPositionError)
                        mInputs[(int)KeyInput.GoRight] = true;
                    else if (pathPosition.x - currentDest.x > Constants.cBotMaxPositionError)
                        mInputs[(int)KeyInput.GoLeft] = true;
                }
                else if (!mReachedNodeY && mPath.Count > mCurrentNodeId + 1 && !destOnGround && !(mMustGrabLeftLedge || mMustGrabRightLedge))
                {
                    int checkedX = 0;

                    int tileX, tileY;
                    mMap.GetMapTileAtPoint(pathPosition, out tileX, out tileY);

                    if (mPath[mCurrentNodeId + 1].x != mPath[mCurrentNodeId].x)
                    {
                        if (mPath[mCurrentNodeId + 1].x > mPath[mCurrentNodeId].x)
                            checkedX = tileX + mWidth;
                        else
                            checkedX = tileX - 1;
                    }

                    if (checkedX != 0 && (!mMap.AnySolidBlockInStripe(checkedX, tileY - 1, mPath[mCurrentNodeId + 1].y) || (Mathf.Abs(pathPosition.x - currentDest.x) > Constants.cBotMaxPositionError)))
                    {
                        //Snap character to the current position if overshot it by more than error margin
                        if (mOldPosition.x < currentDest.x && pathPosition.x > currentDest.x)
                        {
                            mPosition.x = currentDest.x;
                            pathPosition.x = currentDest.x;
                        }

                        if (nextDest.x - pathPosition.x > Constants.cBotMaxPositionError)
                            mInputs[(int)KeyInput.GoRight] = true;
                        else if (pathPosition.x - nextDest.x > Constants.cBotMaxPositionError)
                            mInputs[(int)KeyInput.GoLeft] = true;

                        if (ReachedNodeOnXAxis(pathPosition, currentDest, nextDest) && ReachedNodeOnYAxis(pathPosition, currentDest, nextDest))
                        {
                            mCurrentNodeId += 1;
                            goto case BotState.MoveTo;
                        }
                    }
                }

                if (mFramesOfJumping > 0 &&
                    (mCurrentState == CharacterState.GrabLedge || !mOnGround || (mReachedNodeX && !destOnGround) || (mOnGround && destOnGround)))
                {
                    mInputs[(int)KeyInput.Jump] = true;
                    if (!mOnGround)
                        --mFramesOfJumping;
                }

                if (mCurrentState == Character.CharacterState.GrabLedge && /*!mMustGrabLeftLedge && !mMustGrabRightLedge &&*/ mFramesOfJumping <= 0)
                {
                    mInputs[(int)KeyInput.GoDown] = true;
                }

                if (mPosition == mOldPosition)
                {
                    ++mStuckFrames;
                    if (mStuckFrames > cMaxStuckFrames)
                        MoveTo(mPath[mPath.Count - 1]);
                }
                else
                    mStuckFrames = 0;

                break;
        }

        CharacterUpdate();
	}
}