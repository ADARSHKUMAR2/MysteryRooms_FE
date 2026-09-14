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

    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(false);
    private bool hasTriggeredSolve = false; // Prevents spamming the ServerRpc

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
        
        UpdateBeamState();
    }

    public override void OnNetworkDespawn()
    {
        isSolvedNet.OnValueChanged -= OnSolvedStateChanged;
        base.OnNetworkDespawn();
    }

    public override void SetLocked(bool locked)
    {
        base.SetLocked(locked);
        UpdateBeamState();
    }

    private void Update()
    {
        if (currentState == PuzzleState.Unlocked || currentState == PuzzleState.InProgress)
        {
            if (lineRenderer != null && !lineRenderer.enabled)
            {
                lineRenderer.enabled = true; 
            }
            
            CastBeam();
        }
    }

    private void UpdateBeamState()
    {
        if (lineRenderer == null) return;

        if (currentState == PuzzleState.Locked)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
        else
        {
            lineRenderer.enabled = true;
            if (isSolvedNet.Value)
            {
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

        for (int i = 0; i < maxBounces; i++)
        {
            Ray ray = new Ray(currentPosition, currentDirection);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxDistance, reflectiveLayer | obstacleLayer))
            {
                beamPositions.Add(hit.point);

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

        if (hitTarget)
        {
            if (!isSolvedNet.Value && !hasTriggeredSolve)
            {
                hasTriggeredSolve = true;
                
                // Change color locally instantly for feedback
                if (lineRenderer != null)
                {
                    lineRenderer.startColor = Color.green;
                    lineRenderer.endColor = Color.green;
                }
                
                if (IsSpawned) SubmitLightSolvedServerRpc();
            }
        }
        else if (!hitTarget && hasTriggeredSolve && !isSolvedNet.Value)
        {
            // If the player moved the mirror away before the server confirmed it
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
        if (!isSolvedNet.Value)
        {
            isSolvedNet.Value = true;
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
        }
    }

    private void OnSolvedStateChanged(bool prev, bool current)
    {
        if (current && !prev)
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
        hasTriggeredSolve = false;
        
        if (lineRenderer != null)
        {
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
        }
        
        UpdateBeamState();
    }
}
