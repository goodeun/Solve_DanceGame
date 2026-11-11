using UnityEngine;

/// <summary>
/// Keeps the 3D fever gauge facing the active game camera.
/// </summary>
public sealed class FeverGaugeBillboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector3 rotationOffset;

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        // Apply after Cinemachine has positioned the real camera for this frame.
        transform.rotation = Quaternion.LookRotation(
            targetCamera.transform.position - transform.position,
            targetCamera.transform.up) * Quaternion.Euler(rotationOffset);
    }
}
