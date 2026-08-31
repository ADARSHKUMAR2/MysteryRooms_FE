using UnityEngine;
using System.Collections.Generic;
using MysteryRooms.Game.Data;
using Unity.Netcode;

public class LightPuzzle : BasePuzzle
{
    [Header("Beam Settings")]
    [Tooltip("The LineRenderer component (should be on a child object, NOT the root)")]
    public LineRenderer lineRenderer;
    
    [Tooltip("Where the beam shoots from")]
    public Transform emissionPoint;

    public float maxDistance = 50f;
    public int maxBounces = 5;
    public LayerMask reflectiveLayer; 
    public LayerMask obstacleLayer;   
    
    [Header("Target Receptor")]
    public Transform targetCrystal;   
    private bool hasTriggeredSolve = false;

    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(false);

    protected override void Start()
    {
        base.Start();
        
        if (lineRenderer == null)
        {
            Debug.LogError($"[{puzzleID}] LineRenderer is not assigned!");
            return;
        }

        if (emissionPoint == null)
        {
            emissionPoint = lineRenderer.transform; // Fallback
        }

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, emissionPoint.position);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isSolvedNet.OnValueChanged += OnSolvedStateChanged;
        
        if (isSolvedNet.Value) OnSolvedStateChanged(false, true);
        
        // Ensure beam is properly updated immediately for late joiners
        UpdateBeamState();
    }

    public override void OnNetworkDespawn()
    {
        isSolvedNet.OnValueChanged -= OnSolvedStateChanged;
        base.OnNetworkDespawn();
    }

    // Override SetLocked so we can trigger the beam to turn on/off instantly
    public override void SetLocked(bool locked)
    {
        base.SetLocked(locked);
        UpdateBeamState();
    }

    private void Update()
    {
        // Continuously cast the beam ONLY if we are unlocked/in-progress and not solved
        if (currentState == PuzzleState.Unlocked || currentState == PuzzleState.InProgress)
        {
            CastBeam();
        }
    }

    /// <summary>
    /// Handles turning the visual beam entirely on or off based on puzzle state
    /// </summary>
    private void UpdateBeamState()
    {
        if (lineRenderer == null) return;

        if (currentState == PuzzleState.Locked)
        {
            // Turn beam completely off
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
        else
        {
            // Turn beam on
            lineRenderer.enabled = true;
            if (isSolvedNet.Value)
            {
                // If already solved, we still want to draw it, but green/gold
                CastBeam(); 
            }
        }
    }

    private void CastBeam()
    {
        if (lineRenderer == null || emissionPoint == null) return;

        List<Vector3> beamPositions = new List<Vector3>();
        beamPositions.Add(emissionPoint.position);

        Vector3 currentPosition = emissionPoint.position;
        Vector3 currentDirection = emissionPoint.forward;
        bool hitTarget = false;

        // DEBUG: Track what we hit
        GameObject lastHitObject = null;

        for (int i = 0; i < maxBounces; i++)
        {
            Ray ray = new Ray(currentPosition, currentDirection);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxDistance, reflectiveLayer | obstacleLayer))
            {
                beamPositions.Add(hit.point);
                lastHitObject = hit.collider.gameObject;

                if (targetCrystal != null && hit.collider.transform == targetCrystal)
                {
                    hitTarget = true;
                    break; 
                }

                if (((1 << hit.collider.gameObject.layer) & reflectiveLayer) != 0)
                {
                    currentDirection = Vector3.Reflect(currentDirection, hit.normal);
                    currentPosition = hit.point + currentDirection * 0.01f; 
                }
                else
                {
                    break;
                }
            }
            else
            {
                beamPositions.Add(currentPosition + currentDirection * maxDistance);
                break;
            }
        }

        lineRenderer.positionCount = beamPositions.Count;
        lineRenderer.SetPositions(beamPositions.ToArray());

        // LOGGING BLOCK
        if (hitTarget)
        {
            if (!isSolvedNet.Value && !hasTriggeredSolve)
            {
                hasTriggeredSolve = true; // Stop it from running 60 times a second!
                Debug.Log($"[{puzzleID}] 🎯 Target hit! Triggering ServerRpc...");
                
                // Change color locally instantly
                if (lineRenderer != null)
                {
                    lineRenderer.startColor = Color.green;
                    lineRenderer.endColor = Color.green;
                }
                
                // Tell Server to unlock
                if (IsSpawned) SubmitLightSolvedServerRpc();
            }
        }
        else if (lastHitObject != null)
        {
            // If the player moved the mirror AWAY before the server confirmed it, reset the lock
            hasTriggeredSolve = false;
            if (lineRenderer != null)
            {
                lineRenderer.startColor = Color.cyan;
                lineRenderer.endColor = Color.cyan;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitLightSolvedServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[{puzzleID}] 📡 ServerRpc Received! Attempting to unlock...");
        
        if (!isSolvedNet.Value)
        {
            isSolvedNet.Value = true;
            Debug.Log($"[{puzzleID}] 🔓 Puzzle officially marked as solved on Server!");
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
        }
    }



    private void OnSolvedStateChanged(bool prev, bool current)
    {
        if (current && !prev) // Only log once when it actually flips from false to true!
        {
            Debug.Log($"🎉 [{puzzleID}] Light beam connected! Puzzle solved.");
            
            if (lineRenderer != null)
            {
                lineRenderer.startColor = Color.green;
                lineRenderer.endColor = Color.green;
            }
        }
    }

    protected override bool CheckSolution()
    {
        return isSolvedNet.Value;
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        if (IsServer) isSolvedNet.Value = false;
        
        if (lineRenderer != null)
        {
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
        }
        
        UpdateBeamState();
    }
}
