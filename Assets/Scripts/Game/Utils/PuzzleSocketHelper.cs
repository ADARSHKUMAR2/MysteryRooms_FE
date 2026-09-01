using UnityEngine;

public enum AllowedPuzzleType
{
    Any,
    RotatingStatue,
    SymbolSequence,
    CombinationLock,
    HiddenCompartment,
    MapCoordinates,
    PressurePlate,
    LightPuzzle,
    CardDeckRiddle
}

/// <summary>
/// This script does absolutely nothing in the compiled game. 
/// It only exists so you can easily see empty Socket Transforms in the Unity Editor,
/// and apply constraints to what can spawn here!
/// </summary>
public class PuzzleSocketHelper : MonoBehaviour
{
    [Header("Socket Constraints")]
    [Tooltip("If set to anything other than 'Any', ONLY puzzles of this type can spawn here.")]
    public AllowedPuzzleType allowedPuzzleType = AllowedPuzzleType.Any;

    [Header("Gizmo Settings")]
    public Color gizmoColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    public Vector3 gizmoSize = new Vector3(1f, 1f, 1f);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Change gizmo color based on constraint so you can see it at a glance!
        Color finalGizmoColor = gizmoColor;
        if (allowedPuzzleType != AllowedPuzzleType.Any)
        {
            finalGizmoColor = new Color(0.8f, 0.2f, 0.8f, 0.5f); // Purple if constrained
        }

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        Gizmos.color = finalGizmoColor;
        Gizmos.DrawCube(new Vector3(0, gizmoSize.y / 2f, 0), gizmoSize); 

        Gizmos.color = new Color(finalGizmoColor.r, finalGizmoColor.g, finalGizmoColor.b, 1f);
        Gizmos.DrawWireCube(new Vector3(0, gizmoSize.y / 2f, 0), gizmoSize);

        Gizmos.color = Color.blue;
        Vector3 arrowStart = new Vector3(0, gizmoSize.y / 2f, 0);
        Vector3 arrowEnd = arrowStart + Vector3.forward * (gizmoSize.z / 2f + 0.5f);
        Gizmos.DrawLine(arrowStart, arrowEnd);
        
        Vector3 right = Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0, 180 + 20, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0, 180 - 20, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawLine(arrowEnd, arrowEnd + right * 0.25f);
        Gizmos.DrawLine(arrowEnd, arrowEnd + left * 0.25f);

        // Optional: Draw text label of the constraint type
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        UnityEditor.Handles.Label(transform.position + Vector3.up * (gizmoSize.y + 0.2f), allowedPuzzleType.ToString(), style);

        Gizmos.matrix = oldMatrix;
    }
#endif
}
