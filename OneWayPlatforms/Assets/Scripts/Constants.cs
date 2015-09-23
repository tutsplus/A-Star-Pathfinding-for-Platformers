using UnityEngine;
using System.Collections;

public class Constants
{
    public const float cGravity = -1030.0f;
    public const float cMaxFallingSpeed = -900.0f;

    public const float cWalkSpeed = 160.0f;

    //public const float cJumpSpeed = 210.0f; //1
    //public const float cJumpSpeed = 280.0f; //2
    //public const float cJumpSpeed = 350.0f; //3
    //public const float cJumpSpeed = 380.0f; //4
    //public const float cJumpSpeed = 410.0f; //5
    //public const float cJumpSpeed = 460.0f; //6

    public static readonly float[] cJumpSpeed = { 210.0f, 280.0f, 350.0f, 380.0f, 410.0f, 460.0f };
    public static readonly float[] cHalfSizes = { 6.0f, 12.0f, 18.0f, 26.0f, 30.0f, 36.0f, 42.0f, 50.0f, 60.0f, 62.0f};
    public const int cJumpFramesThreshold = 4;

    public const float cBotMaxPositionError = 1.0f;
}
