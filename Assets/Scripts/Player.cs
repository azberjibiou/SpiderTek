using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // --- Fields ---
    public float maxMoveSpeed = 10f;
    public float groundAccel = 60f;
    public float airAccel = 30f;
    public float jumpSpeed = 12f;
    private float maxVelocity = 45f;
    public Web currentWeb;
    public bool isGrounded = false;
    public bool isDead = false;
    public Vector2 position;
    public Vector2 velocity;
    public Vector2 acceleration;
    private float gravity = -30f;
    private float moveInput = 0f; // 입력값 저장
    private bool jumpPressed = false; // 점프 입력 저장
    private float jumpBufferTimer = 0f; // 점프 버퍼 타이머
    private const float jumpBufferTime = 0.1f; // 버퍼 지속 시간(초)
    private const float playerBoxSizeX = 0.8f; // 플레이어 콜라이더 가로 크기
    private const float playerBoxSizeY = 0.8f; // 플레이어 콜라이더 세로 크기

    // --- Unity Methods ---
    void Start()
    {
        position = transform.position;
        velocity = Vector2.zero;
        acceleration = Vector2.zero;
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        UpdateGrounded();
        UpdateHorizontal();
        UpdateJumpBuffer();
        UpdateJump();
        ApplyGravity();
        ClampVelocity();
        ApplyPositionWithCollision();
    }
    // --- Collision Handling ---
    void ApplyPositionWithCollision()
    {
        if (isDead) return;
        
        // 1. 일단 이동!
        Vector2 deltaPosition = velocity * Time.fixedDeltaTime;
        Vector2 previousPosition = position;
        position += deltaPosition;
        transform.position = position;
        
        // 2. 충돌 검사
        Vector2 boxSize = new Vector2(playerBoxSizeX, playerBoxSizeY);
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f);
        
        bool hasBlockCollision = false;
        
        foreach (var col in hits)
        {
            if (col.gameObject == this.gameObject) continue;
            
            // 블록 충돌 체크
            if (col.gameObject.CompareTag("Block") || col.gameObject.name.Contains("Block"))
            {
                hasBlockCollision = true;
            }
            // 블록이 아닌 것들은 바로 처리
            else
            {
                // Hazard: 사망 처리
                if (col.gameObject.CompareTag("Hazard") || col.gameObject.name.Contains("Spike") || col.gameObject.name.Contains("Hazard"))
                {
                    Die();
                }
                // Checkpoint: 체크포인트 활성화
                else if (col.gameObject.CompareTag("Checkpoint") || col.gameObject.name.Contains("Checkpoint"))
                {
                    ActivateCheckpoint(col.gameObject);
                }
            }
        }
        
        // 3. 블록 충돌이 있으면 separation axis 사용
        if (hasBlockCollision)
        {
            HandleBlockCollisionWithSeparationAxis(previousPosition, deltaPosition);
        }
    }

    void HandleBlockCollisionWithSeparationAxis(Vector2 previousPosition, Vector2 deltaPosition)
    {
        // X축 먼저 시도
        position = previousPosition;
        position.x += deltaPosition.x; // X축만 이동
        transform.position = position;
        
        if (CheckBlockCollision())
        {
            // X축 충돌 - 이전 X 위치로 복원하고 X 속도 제거
            position.x = previousPosition.x;
            velocity.x = 0f;
            transform.position = position;
        }
        
        // Y축 이동
        position.y += deltaPosition.y;
        transform.position = position;
        
        if (CheckBlockCollision())
        {
            // Y축 충돌 - 이전 Y 위치로 복원
            position.y = previousPosition.y;
            
            if (velocity.y <= 0f)
            {
                // 바닥 충돌 (착지)
                velocity.y = 0f;
                isGrounded = true;
            }
            else
            {
                // 천장 충돌
                velocity.y = 0f;
            }
            transform.position = position;
        }
    }

    bool CheckBlockCollision()
    {
        Vector2 boxSize = new Vector2(playerBoxSizeX, playerBoxSizeY);
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f);
        
        foreach (var col in hits)
        {
            if (col.gameObject == this.gameObject) continue;
            
            if (col.gameObject.CompareTag("Block") || col.gameObject.name.Contains("Block"))
            {
                return true;
            }
        }
        return false;
    }

    void Die()
    {
        // GameManager에서 사망 처리
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.PlayerDie();
        }
        else
        {
            Debug.LogWarning("GameManager를 찾을 수 없습니다!");
        }
    }

    void ActivateCheckpoint(GameObject checkpoint)
    {
        // GameManager에서 체크포인트 활성화 처리
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            Checkpoint checkpointComponent = checkpoint.GetComponent<Checkpoint>();
            if (checkpointComponent != null)
            {
                gameManager.ActivateCheckpoint(checkpointComponent);
            }
        }
        else
        {
            Debug.LogWarning("GameManager를 찾을 수 없습니다!");
        }
    }

    // --- Input Handling ---
    void HandleInput()
    {
        // 좌/우 이동 입력값 저장만 (새로운 Input System)
        float horizontal = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            horizontal = -1f;
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            horizontal = 1f;
        moveInput = horizontal;
        
        // 점프 입력 버퍼링 (새로운 Input System)
        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isDead)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        
        // 거미줄 쏘기 (마우스 왼쪽)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            CancelWeb();
            ShootWeb(false);
        }
        
        // 로프 쏘기 (마우스 오른쪽)
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelWeb();
            ShootWeb(true);
        }
        
        // 거미줄/로프 캔슬 (S키)
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            CancelWeb();
        }
        
        // ESC (일시정지)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // GameManager에서 일시정지 처리
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                if (gameManager.isPaused)
                    gameManager.Resume();
                else
                    gameManager.Pause();
            }
        }
    }

    // --- Movement ---
    // 입력값을 ApplyPhysics에서 사용하므로, Move 함수는 삭제

    void Jump()
    {
        velocity.y = jumpSpeed;
        isGrounded = false;
    }

    void UpdateGrounded()
    {
        float rayLength = 0.1f;
        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayLength);
        isGrounded = (hit.collider != null && 
                     (hit.collider.gameObject.CompareTag("Block") || 
                      hit.collider.gameObject.name.Contains("Block")));
    }

    void UpdateHorizontal()
    {
        if (isDead) return;
        float targetSpeed = moveInput * maxMoveSpeed;
        float accel = isGrounded ? groundAccel : airAccel;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * Time.fixedDeltaTime);
    }

    void UpdateJumpBuffer()
    {
        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.fixedDeltaTime;
    }

    void UpdateJump()
    {
        if (isDead) return;
        if (isGrounded && jumpBufferTimer > 0f)
        {
            Jump();
            jumpBufferTimer = 0f;
        }
    }

    void ApplyGravity()
    {
        if (isDead) return;
        if (!isGrounded)
            velocity.y += gravity * Time.fixedDeltaTime;
    }

    void ClampVelocity()
    {
        if (isDead) return;
        // 웹/로프 사용 중일 때는 조금 더 관대하게 (스윙감을 위해)
        if (currentWeb != null && currentWeb.isAttached)
        {
            float webMaxVelocity = maxVelocity * 1.2f; // 20% 더 허용
            velocity = Vector2.ClampMagnitude(velocity, webMaxVelocity);
        }
        else
        {
            // 방향을 유지하면서 전체 크기만 제한
            float maxSpeed = maxVelocity;
            velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
        }
    }

    // --- Web/Rope ---
    void ShootWeb(bool isRope)
    {
        // 이미 웹이 있으면 무시
        if (currentWeb != null) return;
        
        // 마우스 위치(월드 좌표) - 새로운 Input System
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Camera.main.nearClipPlane;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 startPos = transform.position;
        Vector2 targetPos = new Vector2(mouseWorld.x, mouseWorld.y);
        
        // Web 오브젝트를 런타임에 생성
        GameObject webObj = new GameObject(isRope ? "Rope" : "Web");
        Web web = webObj.AddComponent<Web>();
        
        if (web != null)
        {
            web.Initialize(startPos, targetPos, isRope);
            currentWeb = web;
        }
        else
        {
            Debug.LogWarning("Web 컴포넌트 생성 실패!");
            Destroy(webObj);
        }
    }

    void CancelWeb()
    {
        // 현재 거미줄/로프가 있으면 삭제
        if (currentWeb != null)
        {
            Destroy(currentWeb.gameObject);
            currentWeb = null;
        }
    }
}
