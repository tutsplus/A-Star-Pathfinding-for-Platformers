using UnityEngine;
using System.Collections;

public class GameCamera : MonoBehaviour
{

    /// <summary>
    /// A reference to the the player.
    /// </summary>
    public Transform mPlayerTransform;

    public Character mPlayer;

    /// <summary>
    /// The position.
    /// </summary>
    public Vector3 mPosition;

    /// <summary>
    /// The map. Assigned from editor.
    /// </summary>
    public Map mMap;

    public float dampTime = 0.15f;
    private Vector3 velocity = Vector3.zero;

    const int cOuterVisibilityX = 2;
    const int cOuterVisibilityY = 2;

    void Start()
    {
        mPosition = transform.position;
    }

    public void FixedUpdate()
    {
        if (mPlayerTransform == null)
            return;

        if (mPlayer == null)
            mPlayer = mPlayerTransform.GetComponent<Character>();

        Vector2 targetPos;

        targetPos = mPlayer.mPosition;

        mPosition = new Vector3(targetPos.x, targetPos.y, mPosition.z);

        var point = GetComponent<Camera>().WorldToViewportPoint(mPosition);
        var delta = mPosition - GetComponent<Camera>().ViewportToWorldPoint(new Vector3(0.5f, 0.5f, point.z)); //(new Vector3(0.5, 0.5, point.z));
        var destination = transform.position + delta;

        var cameraPos = Vector3.SmoothDamp(transform.position, destination, ref velocity, dampTime);
        if (Mathf.Abs(cameraPos.x - targetPos.x) < 2.0f)
            cameraPos.x = targetPos.x;
        if (Mathf.Abs(cameraPos.y - targetPos.y) < 2.0f)
            cameraPos.y = targetPos.y;

        //make sure the camera doesn't go outside the map bounds on x axis
        if (cameraPos.x < mMap.position.x + Camera.main.pixelWidth * 0.5f - Map.cTileSize / 2 + cOuterVisibilityX * Map.cTileSize)
            cameraPos.x = mMap.position.x + Camera.main.pixelWidth * 0.5f - Map.cTileSize / 2 + cOuterVisibilityX * Map.cTileSize;
        else if (cameraPos.x > mMap.position.x + mMap.mWidth * Map.cTileSize - Camera.main.pixelWidth * 0.5f - Map.cTileSize / 2 - cOuterVisibilityX * Map.cTileSize)
            cameraPos.x = mMap.position.x + mMap.mWidth * Map.cTileSize - Camera.main.pixelWidth * 0.5f - Map.cTileSize / 2 - cOuterVisibilityX * Map.cTileSize;

        //make sure the camera doesn't go outside the map bounds on y axis
        if (cameraPos.y < mMap.position.y + Camera.main.pixelHeight * 0.5f - Map.cTileSize / 2 + cOuterVisibilityX * Map.cTileSize)
            cameraPos.y = mMap.position.y + Camera.main.pixelHeight * 0.5f - Map.cTileSize / 2 + cOuterVisibilityX * Map.cTileSize;
        else if (cameraPos.y > mMap.position.y + mMap.mHeight * Map.cTileSize - Camera.main.pixelHeight * 0.5f - Map.cTileSize / 2 - cOuterVisibilityX * Map.cTileSize)
            cameraPos.y = mMap.position.y + mMap.mHeight * Map.cTileSize - Camera.main.pixelHeight * 0.5f - Map.cTileSize / 2 - cOuterVisibilityX * Map.cTileSize;



        transform.position = new Vector3(Mathf.Round(cameraPos.x), Mathf.Round(cameraPos.y), cameraPos.z);
    }
}
