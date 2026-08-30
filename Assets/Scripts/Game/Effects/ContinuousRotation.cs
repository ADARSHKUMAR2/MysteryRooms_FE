using UnityEngine;

public class ContinuousRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Degrees to rotate per second on each axis")]
    public Vector3 rotationSpeed = new Vector3(0f, 45f, 0f);

    [Tooltip("Whether the rotation is relative to world space or local space")]
    public Space rotationSpace = Space.Self;

    [Header("Optional: Floating Effect")]
    public bool enableFloating = false;
    [Tooltip("How high/low the object bobs")]
    public float floatAmplitude = 0.5f;
    [Tooltip("How fast the object bobs up and down")]
    public float floatFrequency = 1f;

    // Cache the starting position for the floating math
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.localPosition;
    }

    private void Update()
    {
        // 1. Handle Continuous Rotation
        // Multiply by Time.deltaTime so the speed is frame-rate independent
        transform.Rotate(rotationSpeed * Time.deltaTime, rotationSpace);

        // 2. Handle Optional Floating/Bobbing
        if (enableFloating)
        {
            // Calculate new Y position using a sine wave based on time
            float newY = startPosition.y + Mathf.Sin(Time.time * Mathf.PI * floatFrequency) * floatAmplitude;
            
            // Apply new position while keeping X and Z the same
            transform.localPosition = new Vector3(startPosition.x, newY, startPosition.z);
        }
    }
}
