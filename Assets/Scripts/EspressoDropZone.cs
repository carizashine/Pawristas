using UnityEngine;

// Legacy script from the old drag/drop espresso minigame.
// The new espresso minigame uses EspressoMiniGameController instead.
// This file is kept only so old scene references do not break compilation.
public class EspressoDropZone : MonoBehaviour
{
    public bool IsPuckInside(Collider puckCollider)
    {
        return false;
    }

    public bool ReceivePuck(EspressoPuckDraggable puck)
    {
        Debug.LogWarning("EspressoDropZone is no longer used. The espresso minigame now uses EspressoMiniGameController.");
        return false;
    }
}