using UnityEngine;

public static class SceneReturnData
{
    public static bool HasReturnPosition { get; private set; }

    private static Vector3 returnPosition;
    private static Quaternion returnRotation;

    public static void SaveReturnPosition(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            return;
        }

        returnPosition = playerTransform.position;
        returnRotation = playerTransform.rotation;
        HasReturnPosition = true;
    }

    public static Vector3 GetReturnPosition()
    {
        return returnPosition;
    }

    public static Quaternion GetReturnRotation()
    {
        return returnRotation;
    }

    public static void Clear()
    {
        HasReturnPosition = false;
    }
}