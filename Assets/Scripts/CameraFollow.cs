using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // The player to follow
    
    [Header("Follow Settings")]
    public float followSpeed = 5f;
    public bool smoothFollow = true;
    public bool followX = false;
    public bool followY = true;
    
    [Header("Offset Settings")]
    public Vector3 offset = new Vector3(0, 3, -10); // Default camera offset (z should be negative for 2D)
    
    [Header("Boundary Settings")]
    public bool useBoundaries = false;
    public Vector2 minBounds = new Vector2(-10, -5);
    public Vector2 maxBounds = new Vector2(10, 5);
    
    [Header("Look Ahead Settings")]
    public bool useLookAhead = false;
    public float lookAheadDistance = 2f;
    public float lookAheadSpeed = 2f;
    
    [Header("Player Interaction Settings")]
    public bool slowFollowDuringDrag = true;
    public float dragFollowSpeedMultiplier = 0.3f; // How much slower during drag (0.3 = 30% of normal speed)
    
    private Vector3 velocity = Vector3.zero;
    private Rigidbody2D targetRigidbody;
    private PlayerMovement playerMovement;
    private float lastPlayerY; // Store the last Y position of the player
    
    void Start()
    {
        // Auto-find player if target not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        // Get components for interaction detection
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody2D>();
            playerMovement = target.GetComponent<PlayerMovement>();
            lastPlayerY = target.position.y; // Initialize last Y position
        }
        
        // Set initial position
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Check if player is dragging and adjust follow speed accordingly
        bool playerIsDragging = playerMovement != null && IsPlayerDragging();
        float currentFollowSpeed = followSpeed;
        
        if (slowFollowDuringDrag && playerIsDragging)
        {
            // Slow down camera follow during drag for more stable aiming
            currentFollowSpeed *= dragFollowSpeedMultiplier;
        }
        
        Vector3 desiredPosition = CalculateDesiredPosition();
        Vector3 currentPosition = transform.position;
        
        // Handle Y-axis reflection directly (only Y movement)
        if (followY)
        {
            float playerYDelta = target.position.y - lastPlayerY;
            currentPosition.y += playerYDelta; // Directly reflect Y changes
            
            // Enforce minimum Y position of 0
            currentPosition.y = Mathf.Max(0f, currentPosition.y);
            
            lastPlayerY = target.position.y; // Update last Y position
        }
        
        // Keep X position fixed (no X following)
        // Keep Z offset
        currentPosition.z = desiredPosition.z;
        
        transform.position = currentPosition;
        
        // Apply boundaries if enabled
        if (useBoundaries)
        {
            ApplyBoundaries();
        }
    }
    
    bool IsPlayerDragging()
    {
        // Check if player is currently dragging
        if (playerMovement == null) return false;
        
        // We'll use reflection to check the private isDragging field
        var field = typeof(PlayerMovement).GetField("isDragging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (bool)field.GetValue(playerMovement);
        }
        
        // Fallback: detect dragging by checking touch input
        return Input.touchCount > 0 || Input.GetMouseButton(0);
    }
    
    Vector3 CalculateDesiredPosition()
    {
        Vector3 targetPosition = target.position;
        
        // Add look-ahead if enabled
        if (useLookAhead && targetRigidbody != null && targetRigidbody.linearVelocity.magnitude > 0.1f)
        {
            Vector3 lookAheadOffset = targetRigidbody.linearVelocity.normalized * lookAheadDistance;
            targetPosition += Vector3.Lerp(Vector3.zero, lookAheadOffset, Time.deltaTime * lookAheadSpeed);
        }
        
        return targetPosition + offset;
    }
    
    void ApplyBoundaries()
    {
        Vector3 pos = transform.position;
        // Only clamp Y position since we don't follow X
        if (followY) pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        transform.position = pos;
    }
    
    // Public methods for runtime adjustments
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody2D>();
            playerMovement = target.GetComponent<PlayerMovement>();
            lastPlayerY = target.position.y; // Reset last Y position
        }
    }
    
    public void SetFollowSpeed(float speed)
    {
        followSpeed = Mathf.Max(0.1f, speed);
    }
    
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    public void SetBoundaries(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
    }
    
    public void EnableBoundaries(bool enable)
    {
        useBoundaries = enable;
    }
    
    public void EnableLookAhead(bool enable)
    {
        useLookAhead = enable;
    }
    
    // Gizmos for visualizing boundaries in editor
    void OnDrawGizmosSelected()
    {
        if (useBoundaries)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, transform.position.z);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
            Gizmos.DrawWireCube(center, size);
        }
        
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position, 0.5f);
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}