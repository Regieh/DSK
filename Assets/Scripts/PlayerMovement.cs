using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxForce = 20f;
    public float maxDragDistance = 3f;
    public float forceMultiplier = 5f;
    
    [Header("Visual Feedback")]
    public LineRenderer aimLine;
    public Transform powerIndicator;
    public Color aimLineColor = Color.white;
    public int aimLineSegments = 20;
    
    [Header("Death Settings")]
    public string obstacleTag = "Obstacle"; // Tag for obstacles that kill the player
    
    private Rigidbody2D rb;
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 startDragPosition;
    private Vector3 dragDirection;
    private float dragDistance;
    private bool wasTouchPressed = false;
    private bool isDead = false; // Track if player is dead

    public EventHandler eventHandler; // Reference to EventHandler for game state management
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
        // Setup aim line if not assigned
        if (aimLine == null)
        {
            GameObject aimLineObj = new GameObject("AimLine");
            aimLine = aimLineObj.AddComponent<LineRenderer>();
        }
        
        SetupAimLine();
    }
    
    void SetupAimLine()
    {
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.material.color = aimLineColor;
        aimLine.startWidth = 0.1f;
        aimLine.endWidth = 0.05f;
        aimLine.positionCount = 0;
        aimLine.useWorldSpace = true;
    }
    
    void Update()
    {
        // Don't allow input if player is dead
        if (isDead) return;
        
        HandleInput();

        if (isDragging)
        {
            // Slow down player while dragging
            rb.linearVelocity *= 0.4f;

            UpdateAiming();
            ShowVisualFeedback();
        }
        else
        {
            HideVisualFeedback();
        }
    }

    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if collided with an obstacle
        if (collision.gameObject.CompareTag(obstacleTag))
        {
            eventHandler.isGameOver = true; // Trigger game over state
            Die();
        }
    }
    
    void Die()
    {
        if (isDead) return; // Prevent multiple death calls
        
        isDead = true;
        
        // Stop the player
        rb.linearVelocity = Vector2.zero;
        
        // Hide visual feedback
        HideVisualFeedback();
        
        // Disable dragging
        isDragging = false;
        
        Debug.Log("Player died!");
        
        // You can add more death effects here:
        // - Play death animation
        // - Show game over UI
        // - Restart level after delay
        // - Disable player renderer
        // - Play death sound
        
        // Example: Restart level after 2 seconds
        Invoke("RestartLevel", 2f);
    }
    
    void RestartLevel()
    {
        // Reset player state
        isDead = false;
        
        // Reset position to spawn point (modify as needed)
        transform.position = Vector3.zero;
        
        // Reset velocity
        rb.linearVelocity = Vector2.zero;
        
        Debug.Log("Level restarted!");
        
        // Or use SceneManager to reload the scene:
        // UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    void HandleInput()
    {
        bool touchPressed = false;
        Vector2 touchPosition = Vector2.zero;
        
        // Handle touch input for mobile
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            touchPressed = touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            touchPosition = touch.position;
        }
        // Handle mouse input for editor testing
        else if (Input.GetMouseButton(0))
        {
            touchPressed = true;
            touchPosition = Input.mousePosition;
        }
        
        bool touchJustPressed = touchPressed && !wasTouchPressed;
        bool touchJustReleased = !touchPressed && wasTouchPressed;
        
        if (touchJustPressed && CanStartDrag())
        {
            Debug.Log("Attempting to start drag...");
            StartDrag(touchPosition);
        }
        else if (touchJustReleased && isDragging)
        {
            Debug.Log("Ending drag with distance: " + dragDistance);
            EndDrag();
        }
        
        wasTouchPressed = touchPressed;
    }
    
    bool CanStartDrag()
    {
        // Allow dragging at any time - removed velocity check
        return true;
    }
    
    void StartDrag(Vector2 screenPosition)
    {
        Vector3 touchWorldPos = GetWorldPosition(screenPosition);
        Debug.Log("Touch world position: " + touchWorldPos);
        Debug.Log("Player position: " + transform.position);
        
        // Check if touch is close enough to the player
        float distanceToPlayer = Vector3.Distance(touchWorldPos, transform.position);
        Debug.Log("Distance to player: " + distanceToPlayer);
        
        if (distanceToPlayer < 1f) // Adjust this threshold as needed
        {
            isDragging = true;
            startDragPosition = transform.position;
            // Don't stop velocity - allow dragging while moving
            Debug.Log("Drag started successfully!");
        }
        else
        {
            Debug.Log("Too far from player to start drag");
        }
    }
    
    void UpdateAiming()
    {
        Vector2 currentTouchPosition = Vector2.zero;
        
        // Get current touch/mouse position
        if (Input.touchCount > 0)
        {
            currentTouchPosition = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButton(0))
        {
            currentTouchPosition = Input.mousePosition;
        }
        
        Vector3 currentTouchWorldPos = GetWorldPosition(currentTouchPosition);
        // Use current player position instead of initial drag position for smoother feel
        dragDirection = (transform.position - currentTouchWorldPos).normalized;
        dragDistance = Mathf.Min(Vector3.Distance(transform.position, currentTouchWorldPos), maxDragDistance);
    }
    
    void ShowVisualFeedback()
    {
        // Show aim line
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + dragDirection * (dragDistance * 2f);
        
        // Create trajectory preview
        Vector3[] trajectoryPoints = CalculateTrajectory(startPos, dragDirection * GetForceAmount());
        
        aimLine.positionCount = trajectoryPoints.Length;
        aimLine.SetPositions(trajectoryPoints);
        
        // Update power indicator size if exists
        if (powerIndicator != null)
        {
            float powerScale = dragDistance / maxDragDistance;
            powerIndicator.localScale = Vector3.one * (0.5f + powerScale * 0.5f);
            powerIndicator.position = transform.position - dragDirection * dragDistance;
        }
    }
    
    Vector3[] CalculateTrajectory(Vector3 startPos, Vector3 velocity)
    {
        Vector3[] points = new Vector3[aimLineSegments];
        float timeStep = 0.1f;
        
        for (int i = 0; i < aimLineSegments; i++)
        {
            float time = i * timeStep;
            points[i] = startPos + velocity * time;
            
            // Apply physics drag simulation (optional)
            velocity *= 0.98f; // Simulate drag
        }
        
        return points;
    }
    
    void HideVisualFeedback()
    {
        if (aimLine != null)
            aimLine.positionCount = 0;
        
        if (powerIndicator != null)
            powerIndicator.localScale = Vector3.zero;
    }
    
    void EndDrag()
    {
        if (isDragging && dragDistance > 0.1f)
        {
            ApplyForce();
        }
        
        isDragging = false;
        dragDistance = 0f;
    }
    
    void ApplyForce()
    {
        float forceAmount = GetForceAmount();
        Vector2 forceVector = dragDirection * forceAmount;
        
        rb.AddForce(forceVector, ForceMode2D.Impulse);
    }
    
    float GetForceAmount()
    {
        return (dragDistance / maxDragDistance) * maxForce * forceMultiplier;
    }
    
    Vector3 GetWorldPosition(Vector2 screenPosition)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
        worldPos.z = 0f; // For 2D, set z to 0
        return worldPos;
    }
    
    // Optional: Add gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (isDragging)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxDragDistance);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, dragDirection * dragDistance);
        }
    }
}