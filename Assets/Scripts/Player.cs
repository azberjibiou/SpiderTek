using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // --- Fields ---
    public float maxMoveSpeed = 10f;
    public float groundAccel = 60f;
    public float airAccel = 30f;
    public float jumpSpeed = 12f;
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
        ApplyPosition();
        CheckCollisions();
    }
    // --- Collision Handling ---
    void CheckCollisions()
    {
        // 플레이어 콜라이더 크기
        Vector2 boxSize = new Vector2(playerBoxSizeX, playerBoxSizeY);
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f);
        foreach (var col in hits)
        {
            Debug.Log($"Collision with: {col.gameObject.name}");
            if (col.gameObject == this.gameObject) continue;
            
            // Block: 충돌 방향에 따른 위치 보정
            if (col.gameObject.CompareTag("Block") || col.gameObject.name.Contains("Block"))
            {
                HandleBlockCollision(col, boxSize);
            }
            // Hazard: 사망 처리 (가시, 함정 등)
            else if (col.gameObject.CompareTag("Hazard") || col.gameObject.name.Contains("Spike") || col.gameObject.name.Contains("Hazard"))
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
    
    void HandleBlockCollision(Collider2D blockCollider, Vector2 playerSize)
    {
        // 플레이어와 블록의 경계 정보
        Bounds playerBounds = new Bounds(transform.position, playerSize);
        Bounds blockBounds = blockCollider.bounds;
        
        // 겹친 영역 계산
        float overlapLeft = playerBounds.max.x - blockBounds.min.x;
        float overlapRight = blockBounds.max.x - playerBounds.min.x;
        float overlapTop = blockBounds.max.y - playerBounds.min.y;
        float overlapBottom = playerBounds.max.y - blockBounds.min.y;
        
        // 가장 작은 겹침을 찾아서 그 방향으로 분리
        bool wasOnGround = isGrounded;
        
        if (overlapLeft < overlapRight && overlapLeft < overlapTop && overlapLeft < overlapBottom)
        {
            // 왼쪽에서 충돌 - 플레이어를 왼쪽으로 밀어냄
            position.x = blockBounds.min.x - playerSize.x / 2f;
            velocity.x = 0f;
        }
        else if (overlapRight < overlapTop && overlapRight < overlapBottom)
        {
            // 오른쪽에서 충돌 - 플레이어를 오른쪽으로 밀어냄
            position.x = blockBounds.max.x + playerSize.x / 2f;
            velocity.x = 0f;
        }
        else if (overlapTop < overlapBottom)
        {
            // 위쪽에서 충돌 - 플레이어를 위로 밀어냄 (블록 위에 올림)
            position.y = blockBounds.max.y + playerSize.y / 2f;
            if (velocity.y <= 0) // 떨어지고 있었다면 착지
            {
                velocity.y = 0f;
                isGrounded = true;
            }
        }
        else
        {
            // 아래쪽에서 충돌 - 플레이어를 아래로 밀어냄 (천장에 머리 박음)
            position.y = blockBounds.min.y - playerSize.y / 2f;
            if (velocity.y > 0) // 위로 올라가고 있었다면 속도만 제거
            {
                velocity.y = 0f;
            }
            // 천장에서는 isGrounded = false 유지 (중요!)
        }
        
        transform.position = position;
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

    void ApplyPosition()
    {
        if (isDead) return;
        position += velocity * Time.fixedDeltaTime;
        transform.position = position;
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
