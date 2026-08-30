using UnityEngine;
using System;
using System.Collections;

public class PhysicalPressurePlate : MonoBehaviour
{
    [Tooltip("The ID of this plate that matches the backend pattern (e.g., 1, 2, 3)")]
    public int plateID; 
    
    [Header("Visuals")]
    [Tooltip("The actual 3D mesh that moves up/down")]
    public Transform movingPlateMesh;
    public Renderer plateRenderer;

    [Header("Materials (URP)")]
    [Tooltip("Dark stone when locked")]
    public Material inactiveMaterial;
    [Tooltip("Stone with glowing runes when active")]
    public Material activeMaterial;
    [Tooltip("Color of emission when stepped on CORRECTLY")]
    public Color correctGlowColor = new Color(0.85f, 0.65f, 0.13f); // Gold
    [Tooltip("Color of emission when stepped on WRONG")]
    public Color errorGlowColor = new Color(0.8f, 0.1f, 0.1f); // Red
    
    [Header("Cinematic Movement Settings")]
    [Tooltip("Where the plate starts (hidden in floor) relative to parent")]
    public float hiddenYOffset = -1.5f;
    [Tooltip("Where the plate stops (active height) relative to parent")]
    public float activeYOffset = 0f;
    [Tooltip("How far the plate sinks when stepped on")]
    public float pressedYOffset = -0.1f;
    [Tooltip("How fast the plate rises from the ground")]
    public float riseSpeed = 1f;

    [Header("Audio (Optional)")]
    public AudioSource grindSound; // Sound of heavy stone moving
    public AudioSource clickSound; // Sound of stepping on plate

    public enum PlateState { Hidden, Active, Pressed }
    private PlateState currentState = PlateState.Hidden;
    
    public Action<int> onPlateStepped;
    private Coroutine movementCoroutine;
    private Material runtimeMaterial;

    private void Awake()
    {
        // Setup the moving mesh default position
        if (movingPlateMesh != null)
        {
            Vector3 pos = movingPlateMesh.localPosition;
            pos.y = hiddenYOffset;
            movingPlateMesh.localPosition = pos;
        }

        // Create a unique instance of the material so we can change its emission color later
        if (plateRenderer != null && inactiveMaterial != null)
        {
            runtimeMaterial = new Material(inactiveMaterial);
            plateRenderer.material = runtimeMaterial;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't trigger if the plate hasn't risen yet!
        if (currentState == PlateState.Hidden) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log($"[PhysicalPressurePlate] Plate {plateID} stepped on by Player!");
            onPlateStepped?.Invoke(plateID);
        }
    }

    /// <summary>
    /// Animates the plate rising from the floor
    /// </summary>
    public void RevealPlate()
    {
        if (currentState == PlateState.Hidden)
        {
            currentState = PlateState.Active;
            
            // Swap to active material (with emissive runes)
            if (plateRenderer != null && activeMaterial != null)
            {
                runtimeMaterial = new Material(activeMaterial);
                plateRenderer.material = runtimeMaterial;
            }

            if (grindSound != null) grindSound.Play();

            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(MoveToY(activeYOffset, riseSpeed));
        }
    }

    /// <summary>
    /// Animates the plate sinking when stepped on, and sets its glow color
    /// </summary>
    public void PressPlate(bool isCorrectSequence)
    {
        if (currentState != PlateState.Pressed)
        {
            currentState = PlateState.Pressed;
            
            if (clickSound != null) clickSound.Play();

            // Set the emission color
            if (runtimeMaterial != null)
            {
                runtimeMaterial.EnableKeyword("_EMISSION");
                runtimeMaterial.SetColor("_EmissionColor", isCorrectSequence ? correctGlowColor : errorGlowColor);
            }

            // Sink the plate slightly (fast movement)
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(MoveToY(pressedYOffset, 5f)); 
        }
    }

    /// <summary>
    /// Resets the plate to its default active state (un-pressed)
    /// </summary>
    public void ResetPlate()
    {
        if (currentState == PlateState.Pressed)
        {
            currentState = PlateState.Active;

            // Turn off the glowing color, back to default active state
            if (runtimeMaterial != null && activeMaterial != null)
            {
                runtimeMaterial.CopyPropertiesFromMaterial(activeMaterial);
            }

            // Rise back up to active height
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(MoveToY(activeYOffset, 3f));
        }
    }

    private IEnumerator MoveToY(float targetY, float speed)
    {
        if (movingPlateMesh == null) yield break;

        Vector3 startPos = movingPlateMesh.localPosition;
        Vector3 targetPos = new Vector3(startPos.x, targetY, startPos.z);
        
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * speed;
            movingPlateMesh.localPosition = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        
        movingPlateMesh.localPosition = targetPos;
    }
}
