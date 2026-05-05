using UnityEngine;

// A = move left (negative Z), D = move right (positive Z).
// Y and X are fully locked.
public class SyrupCupDraggable : MonoBehaviour
{
    [Tooltip("Movement speed in world units per second.")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("Left boundary in world Z.")]
    [SerializeField] private float minZ = -3f;

    [Tooltip("Right boundary in world Z.")]
    [SerializeField] private float maxZ = 3f;

    private void Update()
    {
        float input = 0f;

        if (Input.GetKey(KeyCode.A)) input =  1f;
        if (Input.GetKey(KeyCode.D)) input = -1f;

        if (input == 0f) return;

        Vector3 pos = transform.position;
        pos.z = Mathf.Clamp(pos.z + input * moveSpeed * Time.deltaTime, minZ, maxZ);
        transform.position = pos;
    }
}
