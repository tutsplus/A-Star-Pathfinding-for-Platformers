using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Algorithms;

public class Character : MovingObject
{
    [System.Serializable]
    public enum CharacterState
    {
        Stand,
        Run,
        Jump,
        GrabLedge,
    };

    public AudioClip mHitWallSfx;
    public AudioClip mJumpSfx;
    public AudioClip mWalkSfx;
    public AudioSource mAudioSource;

    public float mWalkSfxTimer = 0.0f;
    public const float cWalkSfxTime = 0.25f;
    /// <summary>
    /// The current state.
    /// </summary>
    [HideInInspector]
    public CharacterState mCurrentState = CharacterState.Stand;

    public Animator mAnimator;

    /// <summary>
    /// The number of frames passed from changing the state to jump.
    /// </summary>
    protected int mFramesFromJumpStart = 0;

    protected bool[] mInputs;
    protected bool[] mPrevInputs;

    /// <summary>
    /// The hero's vertical speed when he starts a jump
    /// </summary>
    public float mJumpSpeed;

    /// <summary>
    /// The walk speed constant in pixels/second.
    /// </summary>
    public float mWalkSpeed;

    public List<Vector2i> mPath = new List<Vector2i>();

    public int mWidth = 1;
    public int mHeight = 3;
    public Vector2i mLedgeTile;
    public float mLedgeGrabOffset;

    public int mCannotGoLeftFrames = 0;
    public int mCannotGoRightFrames = 0;

    /// <summary>
    /// Raises the draw gizmos event.
    /// </summary>
    void OnDrawGizmos()
    {
        DrawMovingObjectGizmos();

        //calculate the position of the aabb's center
        var aabbPos = transform.position + new Vector3(mAABBOffset.x, mAABBOffset.y, 0.0f);

        //draw grabbing line
        float dir;

        if (mScale.x == 0.0f)
            dir = 1.0f;
        else
            dir = Mathf.Sign(mScale.x);

        Gizmos.color = Color.blue;
        Vector2 halfSize = mAABB.HalfSize;
        var grabVectorTopLeft = new Vector2(aabbPos.x, aabbPos.y)
            + new Vector2(-(halfSize.x + 1.0f) * dir, halfSize.y);
        grabVectorTopLeft.y -= Constants.cGrabLedgeStartY;

        //tk2dSprite sprite = GetComponent<tk2dSprite>();

        //sprite.spriteId

        var grabVectorBottomLeft = new Vector2(aabbPos.x, aabbPos.y)
            + new Vector2(-(halfSize.x + 1.0f) * dir, halfSize.y);
        grabVectorBottomLeft.y -= Constants.cGrabLedgeEndY;
        var grabVectorTopRight = grabVectorTopLeft + Vector2.right * (halfSize.x + 1.0f) * 2.0f * dir;
        var grabVectorBottomRight = grabVectorBottomLeft + Vector2.right * (halfSize.x + 1.0f)*2.0f * dir;

        Gizmos.DrawLine(grabVectorTopLeft, grabVectorBottomLeft);
        Gizmos.DrawLine(grabVectorTopRight, grabVectorBottomRight);
        //draw the path

        if (mPath != null && mPath.Count > 0)
        {
            var start = mPath[0];

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(mMap.transform.position + new Vector3(start.x * Map.cTileSize, start.y * Map.cTileSize, -5.0f), 5.0f);

            for (var i = 1; i < mPath.Count; ++i)
            {
                var end = mPath[i];
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(mMap.transform.position + new Vector3(end.x * Map.cTileSize, end.y * Map.cTileSize, -5.0f), 5.0f);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(mMap.transform.position + new Vector3(start.x * Map.cTileSize, start.y * Map.cTileSize, -5.0f),
                                mMap.transform.position + new Vector3(end.x * Map.cTileSize, end.y * Map.cTileSize, -5.0f));
                start = end;
            }
        }
    }

    public LineRenderer lineRenderer;

    protected void DrawPathLines()
    {
        if (mPath != null && mPath.Count > 0)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetVertexCount(mPath.Count);
            lineRenderer.SetWidth(4.0f, 4.0f);

            for (var i = 0; i < mPath.Count; ++i)
            {
                lineRenderer.SetColors(Color.red, Color.red);
                lineRenderer.SetPosition(i, mMap.transform.position + new Vector3(mPath[i].x * Map.cTileSize, mPath[i].y * Map.cTileSize, -5.0f));
            }
        }
        else
            lineRenderer.enabled = false;
    }

    public void UpdatePrevInputs()
    {
        var count = (byte)KeyInput.Count;

        for (byte i = 0; i < count; ++i)
            mPrevInputs[i] = mInputs[i];
    }

    private void HandleJumping()
    {
        //increase the number of frames that we've been in the jump state
        ++mFramesFromJumpStart;

        //if we hit the ceiling, we don't want to compensate pro jumping, we can prevent by faking a huge mFramesFromJumpStart
        if (mAtCeiling)
            mFramesFromJumpStart = 100;

        //if we're jumping/falling then apply the gravity
        //this should be applied at the beginning of the jump routine
        //because this way we can assure that when we hit the ground 
        //the speed.y will not change after we zero it
        mSpeed.y += Constants.cGravity * Time.deltaTime;

        mSpeed.y = Mathf.Max(mSpeed.y, Constants.cMaxFallingSpeed);

        if (!mInputs[(int)KeyInput.Jump] && mSpeed.y > 0.0f)
        {
            mSpeed.y = Mathf.Min(mSpeed.y, 200.0f);
            mFramesFromJumpStart = 100;
        }

        //in air movement
        //if both or none horizontal movement keys are pressed
        if (mInputs[(int)KeyInput.GoRight] == mInputs[(int)KeyInput.GoLeft])
        {
            mSpeed.x = 0.0f;
        }
        else if (mInputs[(int)KeyInput.GoRight])	//if right key is pressed then accelerate right
        {
            transform.localScale = new Vector3(-mScale.x, mScale.y, 1.0f);
            mSpeed.x = mWalkSpeed;

            //..W
            //.H.     <- to not get stuck in these kind of situations we beed to advance
            //..W			the hero forward if he doesn't push a wall anymore
            if (mPushedRightWall && !mPushesRightWall)
                mPosition.x += 1.0f;
        }
        else if (mInputs[(int)KeyInput.GoLeft])	//if left key is pressed then accelerate left
        {
            transform.localScale = new Vector3(mScale.x, mScale.y, 1.0f);
            mSpeed.x = -mWalkSpeed;

            //W..
            //.H.     <- to not get stuck in these kind of situations we need to advance
            //W..			the hero forward if he doesn't push a wall anymore
            if (mPushedLeftWall && !mPushesLeftWall)
                mPosition.x -= 1.0f;
        }

        //if we just started falling and want to jump, then jump anyway
        if (mInputs[(int)KeyInput.Jump] && (mOnGround || (mSpeed.y < 0.0f && mFramesFromJumpStart < Constants.cJumpFramesThreshold)))
            mSpeed.y = mJumpSpeed;
    }
    
    public void CharacterUpdate()
    {
        switch (mCurrentState)
        {
            case CharacterState.Stand:

                mWalkSfxTimer = cWalkSfxTime;
                mAnimator.Play("Stand");

                mSpeed = Vector2.zero;

                if (!mOnGround)
                {
                    mCurrentState = CharacterState.Jump;
                    break;
                }

                //if left or right key is pressed, but not both
                if (mInputs[(int)KeyInput.GoRight] != mInputs[(int)KeyInput.GoLeft])
                {
                    mCurrentState = CharacterState.Run;
                }
                else if (mInputs[(int)KeyInput.Jump])
                {
                    mSpeed.y = mJumpSpeed;
                    mAudioSource.PlayOneShot(mJumpSfx);
                    mCurrentState = CharacterState.Jump;
                }

                if (mInputs[(int)KeyInput.GoDown] && mOnOneWayPlatform)
                    mPosition -= Vector2.up * cOneWayPlatformThreshold;

                break;
            case CharacterState.Run:

                mAnimator.Play("Walk");

                mWalkSfxTimer += Time.deltaTime;

                if (mWalkSfxTimer > cWalkSfxTime)
                {
                    mWalkSfxTimer = 0.0f;
                    mAudioSource.PlayOneShot(mWalkSfx);
                }

                //if both or neither left nor right keys are pressed then stop walking and stand

                if (mInputs[(int)KeyInput.GoRight] == mInputs[(int)KeyInput.GoLeft])
                {
                    mCurrentState = CharacterState.Stand;
                    mSpeed = Vector2.zero;
                }
                else if (mInputs[(int)KeyInput.GoRight])
                {
                    mSpeed.x = mWalkSpeed;
                    transform.localScale = new Vector3(-mScale.x, mScale.y, 1.0f);
                }
                else if (mInputs[(int)KeyInput.GoLeft])
                {
                    mSpeed.x = -mWalkSpeed;
                    transform.localScale = new Vector3(mScale.x, mScale.y, 1.0f);
                }

                //if there's no tile to walk on, fall
                if (mInputs[(int)KeyInput.Jump])
                {
                    mSpeed.y = mJumpSpeed;
                    mAudioSource.PlayOneShot(mJumpSfx, 1.0f);
                    mCurrentState = CharacterState.Jump;
                }
                else if (!mOnGround)
                {
                    mCurrentState = CharacterState.Jump;
                    break;
                }

                //don't move left when pushing left wall
                if (mPushesLeftWall)
                    mSpeed.x = Mathf.Max(mSpeed.x, 0.0f);
                //don't move right when pushing right wall
                else if (mPushesRightWall)
                    mSpeed.x = Mathf.Min(mSpeed.x, 0.0f);

                if (mInputs[(int)KeyInput.GoDown] && mOnOneWayPlatform)
                    mPosition -= Vector2.up * cOneWayPlatformThreshold;

                break;
            case CharacterState.Jump:

                mWalkSfxTimer = cWalkSfxTime;

                mAnimator.Play("Jump");

                HandleJumping();

                if (mCannotGoLeftFrames > 0)
                {
                    --mCannotGoLeftFrames;
                    mInputs[(int)KeyInput.GoLeft] = false;
                }
                if (mCannotGoRightFrames > 0)
                {
                    --mCannotGoRightFrames;
                    mInputs[(int)KeyInput.GoRight] = false;
                }

                if (mSpeed.y <= 0.0f && mFramesFromJumpStart > 5 && !mAtCeiling
                    && ((mPushesRightWall && mInputs[(int)KeyInput.GoRight]) || (mPushesLeftWall && mInputs[(int)KeyInput.GoLeft])))
                {
                    //we'll translate the original aabb's halfSize so we get a vector Vector2iing
                    //the top right corner of the aabb when we want to grab the right edge
                    //and top left corner of the aabb when we want to grab the left edge
                    Vector2 halfSize;

                    if (mPushesRightWall && mInputs[(int)KeyInput.GoRight])
                        halfSize = mAABB.HalfSize;
                    else
                        halfSize = new Vector2(-mAABB.HalfSizeX - 1.0f, mAABB.HalfSizeY);

                    halfSize.y += mLedgeGrabOffset;

                    int tileIndexX, tileIndexY;
                    mMap.GetMapTileAtPoint(mAABB.Center + halfSize, out tileIndexX, out tileIndexY);

                    int oldTileX, oldTileY;
                    mMap.GetMapTileAtPoint(mOldPosition + mAABBOffset + halfSize, out oldTileX, out oldTileY);

                    int startTile = (mPushedLeftWall && mPushesLeftWall) || (mPushedRightWall && mPushesRightWall) ? oldTileY : tileIndexY;

                    for (int y = startTile; y >= tileIndexY; --y)
                    {
                        //check if by snapping into the grabbing position we won't go into a solid block
                        var collidesAfterSnapping = false;
                        var widthInTiles = mWidth;
                        for (var x = tileIndexX - (int)Mathf.Sign(halfSize.x); Mathf.Abs(tileIndexX - x) <= widthInTiles; x -= (int)Mathf.Sign(halfSize.x))
                        {
                            if (mMap.IsObstacle(x, y - 1 - (int)(halfSize.y * 2.0f) / Map.cTileSize))
                            {
                                collidesAfterSnapping = true;
                                break;
                            }
                        }

                        //check whether the tile on our right corner is empty, if it is then check
                        //whether the tile below it (the one we want to grab onto) is not empty
                        //and finally if there's a block above us, if there is there's no point to grabbing a ledge cause we can't jump of it
                        if (!collidesAfterSnapping
                            && !mMap.IsObstacle(tileIndexX, y)
                            && mMap.IsObstacle(tileIndexX, y - 1)
                            && !mMap.IsObstacle(tileIndexX - (int)Mathf.Sign(halfSize.x), y))
                        {
                            //calculate the appropriate corner
                            var tileCorner = mMap.GetMapTilePosition(tileIndexX, y - 1);
                            tileCorner.x -= Mathf.Sign(halfSize.x) * Map.cTileSize / 2;
                            tileCorner.y += Map.cTileSize / 2;

                            //check whether the tile's corner is between our grabbing Vector2is
                            if (y != tileIndexY ||
                                (tileCorner.y - (mAABB.Center.y + halfSize.y) >= -Constants.cGrabLedgeEndY
                                && tileCorner.y - (mAABB.Center.y + halfSize.y) <= -Constants.cGrabLedgeStartY))
                            {
                                //save the tile we are holding so we can check later on if we can still hold onto it
                                mLedgeTile = new Vector2i(tileIndexX, y - 1);

                                //calculate our position so the corner of our AABB and the tile's are next to each other
                                mPosition.y = tileCorner.y - halfSize.y - mAABBOffset.y;
                                mSpeed = Vector2.zero;

                                //finally grab the edge
                                mCurrentState = CharacterState.GrabLedge;
                                mAnimator.Play("GrabLedge");
                                mMap.mLedgeGrabStopFrames = Map.cLedgeGrabStopFrames;
                                mAudioSource.PlayOneShot(mHitWallSfx, 0.5f);
                                break;
                                //mGame.PlayOneShot(SoundType.Character_LedgeGrab, mPosition, Game.sSfxVolume);
                            }
                        }
                    }
                }

                //if we hit the ground
                if (mOnGround)
                {
                    //if there's no movement change state to standing
                    if (mInputs[(int)KeyInput.GoRight] == mInputs[(int)KeyInput.GoLeft])
                    {
                        mCurrentState = CharacterState.Stand;
                        mSpeed = Vector2.zero;
                        mAudioSource.PlayOneShot(mHitWallSfx, 0.5f);
                    }
                    else	//either go right or go left are pressed so we change the state to walk
                    {
                        mCurrentState = CharacterState.Run;
                        mSpeed.y = 0.0f;
                        mAudioSource.PlayOneShot(mHitWallSfx, 0.5f);
                    }
                }
                break;

            case CharacterState.GrabLedge:

                mAnimator.Play("GrabLedge");

                bool ledgeOnLeft = mLedgeTile.x * Map.cTileSize < mPosition.x;
                bool ledgeOnRight = !ledgeOnLeft;

                //if down button is held then drop down
                if (mInputs[(int)KeyInput.GoDown]
                    || (mInputs[(int)KeyInput.GoLeft] && ledgeOnRight)
                    || (mInputs[(int)KeyInput.GoRight] && ledgeOnLeft))
                {
                    if (ledgeOnLeft)
                        mCannotGoLeftFrames = 3;
                    else
                        mCannotGoRightFrames = 3;

                    mCurrentState = CharacterState.Jump;
                    //mGame.PlayOneShot(SoundType.Character_LedgeRelease, mPosition, Game.sSfxVolume);
                }
                else if (mInputs[(int)KeyInput.Jump])
                {
                    //the speed is positive so we don't have to worry about hero grabbing an edge
                    //right after he jumps because he doesn't grab if speed.y > 0
                    mSpeed.y = mJumpSpeed;
                    mAudioSource.PlayOneShot(mJumpSfx, 1.0f);
                    mCurrentState = CharacterState.Jump;
                }

                //when the tile we grab onto gets destroyed
                if (!mMap.IsObstacle(mLedgeTile.x, mLedgeTile.y))
                    mCurrentState = CharacterState.Jump;

                break;
        }

        if ((!mWasOnGround && mOnGround)
            || (!mWasAtCeiling && mAtCeiling)
            || (!mPushedLeftWall && mPushesLeftWall)
            || (!mPushedRightWall && mPushesRightWall))
            mAudioSource.PlayOneShot(mHitWallSfx, 0.5f);

        UpdatePhysics();

        if (mWasOnGround && !mOnGround)
            mFramesFromJumpStart = 0;

        UpdatePrevInputs();
    }
}
