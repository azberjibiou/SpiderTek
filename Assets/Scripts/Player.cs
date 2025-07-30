using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // --- Fields ---
    public float maxMoveSpeed = 10f;
    public float groundAccel = 60f;
    public float airAccel = 30f;
    public float ropeAccel = 15f; // 로프 사용 시 가속
    public float jumpSpeed = 18f;
    public float maxVelocity = 45f;
    public Web currentWeb;
    public bool isGrounded = false;
    public bool isWalledLeft = false;   // 왼쪽 벽에 닿아있는지
    public bool isWalledRight = false;  // 오른쪽 벽에 닿아있는지
    public bool isCeilinged = false;    // 천장에 닿아있는지
    public bool isDead = false;
    public Vector2 position;
    public Vector2 velocity;
    public Vector2 acceleration;
    public float moveInput = 0f; // 입력값 저장 (Web.cs에서 접근 가능)
    private bool jumpPressed = false; // 점프 입력 저장
    private float jumpBufferTimer = 0f; // 점프 버퍼 타이머
    private const float jumpBufferTime = 0.1f; // 버퍼 지속 시간(초)
    private const float playerBoxSizeX = 0.8f; // 플레이어 콜라이더 가로 크기
    private const float playerBoxSizeY = 0.8f; // 플레이어 콜라이더 세로 크기

    private const bool isDebugMode = false; // 디버그 모드 여부 (개발 중에만 사용)

    // --- Unity Methods ---
    void Start()
    {
        position = transform.position;
        velocity = Vector2.zero;
        acceleration = Vector2.zero;
        if(isDebugMode)
        {
            Time.timeScale = 0.1f; // 디버그 모드에서는 시간 흐름을 느리게 설정
        }
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        UpdateCollisionStates();
        UpdateHorizontal();
        UpdateJumpBuffer();
        UpdateJump();
        ApplyGravity();
        ClampVelocity();
        ApplyPositionWithCollision();
        //Debug.Log($"[PLAYER] Position: {position}, Velocity: {velocity}, Grounded: {isGrounded}");
    }
    // --- Collision Handling ---
    void UpdateCollisionStates()
    {
        Vector2 center = transform.position;
        float halfWidth = playerBoxSizeX * 0.5f;
        float halfHeight = playerBoxSizeY * 0.5f;
        float checkDistance = 0.1f; // 체크할 거리
        
        // 아래 (바닥) 체크 - OverlapBox로 영역 전체 검사
        Vector2 bottomCheckPos = center + Vector2.down * (halfHeight + checkDistance * 0.5f);
        Vector2 bottomCheckSize = new Vector2(playerBoxSizeX * 0.8f, checkDistance);
        Collider2D bottomHit = Physics2D.OverlapBox(bottomCheckPos, bottomCheckSize, 0f);
        isGrounded = (bottomHit != null && IsBlockCollider(bottomHit));
        
        // 위 (천장) 체크 - OverlapBox로 영역 전체 검사
        Vector2 topCheckPos = center + Vector2.up * (halfHeight + checkDistance * 0.5f);
        Vector2 topCheckSize = new Vector2(playerBoxSizeX * 0.8f, checkDistance);
        Collider2D topHit = Physics2D.OverlapBox(topCheckPos, topCheckSize, 0f);
        isCeilinged = (topHit != null && IsBlockCollider(topHit));
        
        // 왼쪽 벽 체크 - OverlapBox로 영역 전체 검사
        Vector2 leftCheckPos = center + Vector2.left * (halfWidth + checkDistance * 0.5f);
        Vector2 leftCheckSize = new Vector2(checkDistance, playerBoxSizeY * 0.8f);
        Collider2D leftHit = Physics2D.OverlapBox(leftCheckPos, leftCheckSize, 0f);
        isWalledLeft = (leftHit != null && IsBlockCollider(leftHit));
        
        // 오른쪽 벽 체크 - OverlapBox로 영역 전체 검사
        Vector2 rightCheckPos = center + Vector2.right * (halfWidth + checkDistance * 0.5f);
        Vector2 rightCheckSize = new Vector2(checkDistance, playerBoxSizeY * 0.8f);
        Collider2D rightHit = Physics2D.OverlapBox(rightCheckPos, rightCheckSize, 0f);
        isWalledRight = (rightHit != null && IsBlockCollider(rightHit));
        
        // 디버그 로그 (필요시)
        if (isDebugMode && (isWalledLeft || isWalledRight || isCeilinged))
        {
            Debug.Log($"[COLLISION_STATE] Ground: {isGrounded}, Ceiling: {isCeilinged}, Left: {isWalledLeft}, Right: {isWalledRight}");
        }
    }
    
    bool IsBlockCollider(Collider2D collider)
    {
        return collider.gameObject.CompareTag("Block") || collider.gameObject.name.Contains("Block");
    }

    void ApplyPositionWithCollision()
    {
        if (isDead) return;

        // 0. 충돌 상태에 따라 속도 제한
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = 0f; // 바닥에 닿았을 때 아래로 떨어지지 않도록
        }
        if (isCeilinged && velocity.y > 0f)
        {
            velocity.y = 0f; // 천장에 닿았을 때 위로 올라가지 않도록
        }
        if (isWalledLeft && velocity.x < 0f)
        {
            velocity.x = 0f; // 왼쪽 벽에 닿았을 때 왼쪽으로 이동하지 않도록
        }
        if (isWalledRight && velocity.x > 0f)
        {
            velocity.x = 0f; // 오른쪽 벽에 닿았을 때 오른쪽으로 이동하지 않도록
        }
        
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
        
        // 3. 블록 충돌이 있으면 raycast로 정확한 위치 찾기
        if (hasBlockCollision)
        {
            HandleBlockCollision(previousPosition, deltaPosition);
        }
    }

    void HandleBlockCollision(Vector2 previousPosition, Vector2 deltaPosition)
    {
        // 원래 위치로 돌아가기
        position = previousPosition;
        transform.position = position;

        // 이동 방향과 거리
        Vector2 direction = deltaPosition.normalized;
        float distance = deltaPosition.magnitude;
        Vector2 boxSize = new Vector2(playerBoxSizeX, playerBoxSizeY);

        // BoxCast는 플레이어 중심에서 시작 (박스 자체가 이동하므로 오프셋 불필요)
        RaycastHit2D hit = Physics2D.BoxCast(
            previousPosition,            // 플레이어 중심에서 시작
            boxSize,                     // 박스 크기
            0f,                          // 회전
            direction,                   // 방향
            distance,                    // 원래 이동 거리
            LayerMask.GetMask("Default") // 레이어
        );

        if (hit.collider != null)
        {
            // 중심에서 충돌점까지의 거리보다 조금 덜 이동 (여유 공간 확보)
            float safeDistance = Mathf.Max(0f, hit.distance - 0.1f);
            position = previousPosition + direction * safeDistance;
            Debug.Log($"[COLLISION] Hit at {hit.point} - Hit distance: {hit.distance:F3}, Safe distance: {safeDistance:F3}");
            Debug.Log($"[COLLISION] Previous position: {previousPosition}, New position: {position}");
            transform.position = position;

            // hit.normal을 사용해서 속도를 수직이 되도록 보정
            Vector2 normal = hit.normal;
            velocity = ReflectVelocityWithNormal(velocity, normal);

            // 바닥 충돌인지 확인 (normal이 위쪽을 향하면 바닥)
            if (normal.y > 0.7f)
            {
                isGrounded = true;
                Debug.Log($"[COLLISION] grounded ok - Normal: {normal}");
            }
            else
            {
                isGrounded = false;
            }
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f);
        Debug.Log($"[COLLISION] hits({transform.position}, {boxSize}): {hits.Length}");

    }

    /// <summary>
    /// velocity가 normal과 수직이 되도록 보정 (벽을 따라 미끄러지기)
    /// </summary>
    Vector2 ReflectVelocityWithNormal(Vector2 velocity, Vector2 normal)
    {
        // normal과 수직인 성분만 제거
        // 공식: newVelocity = velocity - (velocity · normal) * normal
        float dotProduct = Vector2.Dot(velocity, normal);
        Vector2 newVelocity = velocity - dotProduct * normal;
        
        Debug.Log($"[PHYSICS] Velocity correction - Original: {velocity}, Normal: {normal}, Dot: {dotProduct:F2}, New: {newVelocity}");
        return newVelocity;
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

    void UpdateHorizontal()
    {
        if (isDead) return;
        float targetSpeed = moveInput * maxMoveSpeed;
        float accel = isGrounded ? groundAccel : airAccel;
        
        // 로프 사용 중일 때는 감속 금지, 제한된 가속만 허용
        if (currentWeb != null && currentWeb.isAttached && currentWeb.isRope)
        {
            accel = ropeAccel;
            float currentSpeedX = Mathf.Abs(velocity.x);

            // 현재 속도가 targetSpeed보다 작고, 가속하려는 경우에만 허용
            if (currentSpeedX > targetSpeed)
            {
                accel = 0; // 가/감속 차단
                Debug.Log($"[PLAYER] Rope acceleration blocked - Current: {velocity.x:F2}, Target: {targetSpeed:F2}");
            }
        }
        
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
        if (currentWeb != null && currentWeb.isAttached && !currentWeb.isRope) // 현재 거미줄이 있으면 중력 작용 안 함
            return;
        velocity.y += GameManager.gravity * Time.fixedDeltaTime;
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
