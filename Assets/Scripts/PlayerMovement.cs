using UnityEngine;
using UnityEngine.SceneManagement;

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
    public string obstacleTag = "Obstacle"; 
    
    [Header("Level Settings")] 
    public string finishTag = "Finish"; 
    public string homeSceneName = "HomeScene"; 
    
    private Rigidbody2D rb;
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 startDragPosition;
    private Vector3 dragDirection;
    private float dragDistance;
    private bool wasTouchPressed = false;
    private bool isDead = false; 

    public EventHandler eventHandler; 
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
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
        if (isDead) return;
        
        HandleInput();

        if (isDragging)
        {
            rb.velocity *= 0.4f; 
            UpdateAiming();
            ShowVisualFeedback();
        }
        else
        {
            HideVisualFeedback();
        }
    }

    // Called when the player physically collides with something
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(obstacleTag))
        {
            Debug.Log("Player collided with an obstacle. Game Over!");
            eventHandler.isGameOver = true;
            Die();
        }
    }
    
    // Called when the player passes through a trigger collider
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(finishTag))
        {
            Debug.Log("Player reached the finish line! Loading Home Scene.");
            LoadHomeScene();
        }
    }
    
    void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.velocity = Vector2.zero;
        HideVisualFeedback();
        isDragging = false;
        
        // This is where you would reload the scene.
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
    
    void LoadHomeScene()
    {
        SceneManager.LoadScene(homeSceneName);
    }

    void HandleInput()
    {
        bool touchPressed = false;
        Vector2 touchPosition = Vector2.zero;
        
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            touchPressed = touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            touchPosition = touch.position;
        }
        else if (Input.GetMouseButton(0))
        {
            touchPressed = true;
            touchPosition = Input.mousePosition;
        }
        
        bool touchJustPressed = touchPressed && !wasTouchPressed;
        bool touchJustReleased = !touchPressed && wasTouchPressed;
        
        if (touchJustPressed && CanStartDrag())
        {
            StartDrag(touchPosition);
        }
        else if (touchJustReleased && isDragging)
        {
            EndDrag();
        }
        
        wasTouchPressed = touchPressed;
    }
    
    bool CanStartDrag()
    {
        return true;
    }
    
    void StartDrag(Vector2 screenPosition)
    {
        Vector3 touchWorldPos = GetWorldPosition(screenPosition);
        float distanceToPlayer = Vector3.Distance(touchWorldPos, transform.position);
        
        if (distanceToPlayer < 1f)
        {
            isDragging = true;
            startDragPosition = transform.position;
        }
    }
    
    void UpdateAiming()
    {
        Vector2 currentTouchPosition = Vector2.zero;
        
        if (Input.touchCount > 0)
        {
            currentTouchPosition = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButton(0))
        {
            currentTouchPosition = Input.mousePosition;
        }
        
        Vector3 currentTouchWorldPos = GetWorldPosition(currentTouchPosition);
        dragDirection = (transform.position - currentTouchWorldPos).normalized;
        dragDistance = Mathf.Min(Vector3.Distance(transform.position, currentTouchWorldPos), maxDragDistance);
    }
    
    void ShowVisualFeedback()
    {
        float baseOffset = 0.5f; 
        Vector3 startPos = transform.position - dragDirection * baseOffset;
        Vector3 endPos = startPos + dragDirection * (dragDistance * 2f);

        Vector3[] trajectoryPoints = CalculateTrajectory(startPos, dragDirection * GetForceAmount());
        aimLine.positionCount = trajectoryPoints.Length;
        aimLine.SetPositions(trajectoryPoints);

        if (powerIndicator != null)
        {
            float powerScale = dragDistance / maxDragDistance;

            if (dragDistance > 0.1f)
            {
                powerIndicator.gameObject.SetActive(true);
                powerIndicator.localScale = Vector3.one * (0.5f + powerScale * 0.5f);
                float indicatorOffset = baseOffset + dragDistance + 0.3f;
                powerIndicator.position = transform.position - dragDirection * indicatorOffset;
            }
            else
            {
                powerIndicator.gameObject.SetActive(false);
            }
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
            velocity *= 0.98f;
        }
        
        return points;
    }
    
    void HideVisualFeedback()
    {
        if (aimLine != null)
            aimLine.positionCount = 0;
        
        if (powerIndicator != null)
            powerIndicator.gameObject.SetActive(false);
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
        worldPos.z = 0f;
        return worldPos;
    }
    
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