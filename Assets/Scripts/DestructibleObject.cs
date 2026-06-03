using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hanzzz.MeshDemolisher;

public class DestructibleObject : MonoBehaviour
{
    [Header("Health Settings")]
    public float hp = 100f;

    [Header("Demolish Settings")]
    [Tooltip("Parent transform containing empty GameObjects representing break points")]
    public Transform breakPointsParent;
    [Tooltip("Material for the inside of the broken pieces")]
    public Material interiorMaterial;
    [Tooltip("Parent to hold the broken pieces. If empty, will create one automatically.")]
    public Transform resultParent;

    private MeshDemolisher meshDemolisher;
    private bool isDead = false;

    private void Start()
    {
        isDead = false;
        gameObject.SetActive(true);
        
        // Clear any leftover pieces from Editor testing
        if (resultParent != null)
        {
            for (int i = resultParent.childCount - 1; i >= 0; i--)
            {
                Destroy(resultParent.GetChild(i).gameObject);
            }
        }
    }

    private void Update()
    {
        // For testing: Press F to instantly reduce HP to 0 and break the object
        if (Input.GetKeyDown(KeyCode.F))
        {
            TakeDamage(hp);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        hp -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. HP left: {hp}");

        if (hp <= 0)
        {
            DieAndBreak();
        }
    }

    [ContextMenu("Test Demolish Now!")]
    private void DieAndBreak()
    {
        isDead = true;

        if (meshDemolisher == null)
        {
            meshDemolisher = new MeshDemolisher();
        }

        if (resultParent == null)
        {
            GameObject container = new GameObject(gameObject.name + "_BrokenPieces");
            resultParent = container.transform;
        }

        if (breakPointsParent == null)
        {
            Debug.LogError("BreakPointsParent is not assigned! Cannot demolish.");
            return;
        }

        // Gather all break points
        List<Transform> breakPoints = new List<Transform>();
        for (int i = 0; i < breakPointsParent.childCount; i++)
        {
            breakPoints.Add(breakPointsParent.GetChild(i));
        }

        // Verify if the mesh can be demolished (optional, just logging warning)
        if (!meshDemolisher.VerifyDemolishInput(gameObject, breakPoints))
        {
            Debug.LogWarning("Mesh verification failed! Attempting to demolish anyway...");
        }

        // Demolish the object!
        List<GameObject> brokenPieces = meshDemolisher.Demolish(gameObject, breakPoints, interiorMaterial);

        // Process each broken piece
        foreach (GameObject piece in brokenPieces)
        {
            // Move piece to the result parent
            piece.transform.SetParent(resultParent, true);
            
            // Shrink the piece very slightly to prevent the convex colliders from overlapping.
            // Overlapping colliders cause the physics engine to forcefully push them apart (explosion effect).
            piece.transform.localScale = piece.transform.localScale * 0.98f;

            // Add Rigidbody so it falls and interacts with physics
            Rigidbody rb = piece.AddComponent<Rigidbody>();
            rb.mass = 1f;

            // Add MeshCollider so pieces can collide with each other and the floor
            MeshCollider collider = piece.AddComponent<MeshCollider>();
            collider.convex = true; // Must be convex for Rigidbody collisions
        }

        // Hide or destroy the original unbroken object
        gameObject.SetActive(false);
    }
}
