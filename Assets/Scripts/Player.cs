using UnityEngine;

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
    // --- Collision Handling ---
    void CheckCollisions()
    {
        // 플레이어 콜라이더 크기(예시: 0.8 x 0.8)
        Vector2 boxSize = new Vector2(0.8f, 0.8f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f);
        foreach (var col in hits)
        {
            if (col.gameObject == this.gameObject) continue;
            // Block: 위치 보정(간단히 위로 올림)
            if (col.gameObject.name == "Block")
            {
                // 예시: 블럭 위에 정확히 올려놓기
                position.y = col.bounds.max.y + boxSize.y / 2f;
                velocity.y = 0f;
                transform.position = position;
            }
            // Spike: 사망 처리
            else if (col.gameObject.name == "Spike")
            {
                Die();
            }
            // Checkpoint: 체크포인트 활성화
            else if (col.gameObject.name == "Checkpoint")
            {
                ActivateCheckpoint(col.gameObject);
            }
        }
    }

    void Die()
    {
        // GameHandler(혹은 GameManager)에서 사망 처리하도록 위임
        // 예시: GameHandler.Instance.PlayerDie();
        Debug.Log("플레이어 사망! (GameHandler로 위임)");
    }

    void ActivateCheckpoint(GameObject checkpoint)
    {
        // GameManager에서 체크포인트 활성화 처리하도록 위임
        // 예시: GameManager.Instance.ActivateCheckpoint(checkpoint.GetComponent<Checkpoint>());
        Debug.Log("체크포인트 활성화: " + checkpoint.name + " (GameManager로 위임)");
    }
    }

    // --- Input Handling ---
    void HandleInput()
    {
        // 좌/우 이동 입력값 저장만
        moveInput = Input.GetAxisRaw("Horizontal");
        // 점프 입력 버퍼링
        if (Input.GetButtonDown("Jump") && !isDead)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        // 거미줄 쏘기 (마우스 왼쪽)
        if (Input.GetMouseButtonDown(0))
        {
            CancelWeb();
            ShootWeb(false);
        }
        // 로프 쏘기 (마우스 오른쪽)
        if (Input.GetMouseButtonDown(1))
        {
            CancelWeb();
            ShootWeb(true);
        }
        // 거미줄/로프 캔슬 (S키)
        if (Input.GetKeyDown(KeyCode.S))
        {
            CancelWeb();
        }
        // ESC (일시정지)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
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
        isGrounded = (hit.collider != null && hit.collider.gameObject.name == "Block");
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
        // 거미줄/로프 프리팹이 Resources 폴더에 있다고 가정 ("WebPrefab")
        if (currentWeb != null) return;
        GameObject webPrefab = Resources.Load<GameObject>("WebPrefab");
        if (webPrefab == null)
        {
            Debug.LogWarning("WebPrefab을 Resources 폴더에 넣어주세요.");
            return;
        }
        // 마우스 위치(월드 좌표)
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 startPos = transform.position;
        Vector2 targetPos = new Vector2(mouseWorld.x, mouseWorld.y);
        // Web 오브젝트 생성 및 초기화
        GameObject webObj = Instantiate(webPrefab, startPos, Quaternion.identity);
        Web web = webObj.GetComponent<Web>();
        if (web != null)
        {
            web.Initialize(startPos, targetPos, isRope);
            currentWeb = web;
        }
        else
        {
            Debug.LogWarning("Web 컴포넌트가 프리팹에 없습니다.");
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
