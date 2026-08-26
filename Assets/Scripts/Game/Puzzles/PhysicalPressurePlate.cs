using UnityEngine;
using System;

public class PhysicalPressurePlate : MonoBehaviour
{
    [Tooltip("The ID of this plate that matches the backend pattern (e.g., 1, 2, 3)")]
    public int plateID; 
    
    [Header("Visuals (Optional)")]
    public Renderer plateRenderer;

    [Tooltip("When the puzzle is locked by dependencies")]
    public Material inactiveMaterial;
    [Tooltip("When the puzzle is ready to be solved")]
    public Material defaultMaterial;
    [Tooltip("When the player is standing on it")]
    public Material pressedMaterial;
    public enum PlateState { Inactive, Default, Pressed }

    public Action<int> onPlateStepped;

    private void OnTriggerEnter(Collider other)
    {
        // 1. Log every single object that touches the plate's trigger
        Debug.Log($"[PhysicalPressurePlate] Object entered trigger: '{other.name}' with Tag: '{other.tag}' on Plate ID {plateID}");

        // Make sure your player character has the tag "Player"
        if (other.CompareTag("Player"))
        {
            // 2. Log success
            Debug.Log($"[PhysicalPressurePlate] Success! Plate {plateID} stepped on by Player!");
            onPlateStepped?.Invoke(plateID);
        }
        else
        {
            // 3. Log failure/ignore
            Debug.Log($"[PhysicalPressurePlate] Ignored object '{other.name}' on Plate {plateID} because it does not have the 'Player' tag.");
        }
    }

    public void SetVisualState(PlateState state)
    {
        if (plateRenderer == null) return;

        switch (state)
        {
            case PlateState.Inactive:
                if (inactiveMaterial != null) plateRenderer.material = inactiveMaterial;
                break;
            case PlateState.Default:
                if (defaultMaterial != null) plateRenderer.material = defaultMaterial;
                break;
            case PlateState.Pressed:
                if (pressedMaterial != null) plateRenderer.material = pressedMaterial;
                break;
        }
    }
}
