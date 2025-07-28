using UnityEngine;

public class Web : MonoBehaviour
{
    // --- Fields ---
    public Vector2 endPoint;
    public Vector2 velocity;
    public bool isAttached = false;
    public bool isRope = false;
    public const float maxLength = 20f;
    
    private Vector2 startPoint;
    private Player player;
    private LineRenderer lineRenderer;
    private float currentLength = 0f;
    private const float extendSpeed = 100f;
    private const float ropeForce = 100f;      // 로프의 제약력 (강함)
    private const float webForce = 300f;       // 거미줄의 장력 (강함)
    private const float minWebLength = 2f;     // 최소 거미줄 길이 (이하면 파괴)
    private const float reducedTangentSpeed = 0.2f; // 접선 속도 감소 비율 (20% 유지)
    
    // 새로운 예측 충돌 시스템
    private bool hasPredictedCollision = false;
    private Vector2 predictedHitPoint;
    private float timeToCollision = 0f;
    private float collisionTimer = 0f;
    
    // --- Unity Methods ---
    void Start()
    {
        // LineRenderer 컴포넌트 추가 (거미줄/로프 시각화)
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        // 색상 설정 (startColor와 endColor 사용)
        Color webColor = isRope ? Color.red : Color.blue; // 임시로 파란색(거미줄), 빨간색(로프)
        lineRenderer.startColor = webColor;
        lineRenderer.endColor = webColor;
        
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        
        // Player 찾기
        player = FindObjectOfType<Player>();
    }
    
    void Update()
    {
        // 줄이 화면에 나타나는 것을 업데이트
        if (lineRenderer != null && player != null)
        {
            // 시작점은 항상 현재 플레이어 위치
            Vector2 currentPlayerPos = player.transform.position;
            lineRenderer.SetPosition(0, currentPlayerPos);
            
            // 끝점은 부착된 경우 endPoint, 아닌 경우 현재 발사체 위치
            Vector2 currentEndPos = isAttached ? endPoint : (Vector2)transform.position;
            lineRenderer.SetPosition(1, currentEndPos);
            
            // 현재 길이 업데이트 (스윙 물리에 필요)
            if (isAttached)
            {
                currentLength = Vector2.Distance(currentPlayerPos, endPoint);
                
                // 거미줄이 너무 짧아지면 파괴
                if (currentLength < minWebLength)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
    
    void FixedUpdate()
    {
        // 줄의 이동 구현
        if (!isAttached)
        {
            Extend();
        }
        else if (player != null)
        {
            ApplyTension();
        }
    }
    
    // --- Public Methods ---
    public void Initialize(Vector2 startPos, Vector2 targetPos, bool rope)
    {
        startPoint = startPos;
        endPoint = targetPos;
        isRope = rope;
        transform.position = startPos;
        
        // 발사 방향 계산
        Vector2 direction = (targetPos - startPos).normalized;
        velocity = direction * extendSpeed;
        
        // 충돌 예측 수행
        PredictCollision();
    }
    
    public void AddForce(Vector2 force)
    {
        // 사용자에게 장력을 작용
        if (player != null && isAttached)
        {
            player.velocity += force * Time.fixedDeltaTime;
        }
    }
    
    public void Extend()
    {
        // 충돌 예측이 있는 경우 타이머 업데이트
        if (hasPredictedCollision)
        {
            collisionTimer += Time.fixedDeltaTime;
            
            // 예상 충돌 시간에 도달하면 부착
            if (collisionTimer >= timeToCollision)
            {
                AttachToSurface(predictedHitPoint);
                return;
            }
        }
        
        // 웹 발사체 이동
        transform.position += (Vector3)(velocity * Time.fixedDeltaTime);
        currentLength = Vector2.Distance(startPoint, transform.position);
        
        // 최대 길이에 도달하면 웹 파괴
        if (currentLength >= maxLength)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    // 발사 시 충돌을 미리 예측하는 함수
    private void PredictCollision()
    {
        Vector2 direction = velocity.normalized;
        float speed = velocity.magnitude;
        
        // 모든 충돌체를 검사하고 "Block" 태그만 필터링
        RaycastHit2D[] allHits = Physics2D.RaycastAll(startPoint, direction, maxLength);
        RaycastHit2D hit = new RaycastHit2D(); // 빈 hit 구조체
        
        foreach (var hitInfo in allHits)
        {
            if (hitInfo.collider.gameObject.CompareTag("Block"))
            {
                hit = hitInfo;
                break; // 첫 번째 블록에서 중단
            }
        }
        
        if (hit.collider != null)
        {
            hasPredictedCollision = true;
            predictedHitPoint = hit.point;
            
            // 충돌까지의 거리와 시간 계산
            float distanceToHit = Vector2.Distance(startPoint, hit.point);
            timeToCollision = distanceToHit / speed;
            
            Debug.Log($"Block collision predicted at {predictedHitPoint} in {timeToCollision:F2} seconds");
        }
        else
        {
            hasPredictedCollision = false;
            Debug.Log("No block collision predicted within range");
        }
    }
    
    // 웹을 표면에 부착하는 함수
    private void AttachToSurface(Vector2 attachPoint)
    {
        isAttached = true;
        velocity = Vector2.zero;
        endPoint = attachPoint;
        transform.position = attachPoint;
        
        // 웹 부착 시 플레이어 속도 조정
        AdjustPlayerVelocityOnAttach();
        
        Debug.Log($"Web attached at predicted point: {attachPoint}");
    }
    
    // --- Private Methods ---
    private void ApplyTension()
    {
        if (player == null) return;
        
        Vector2 playerPos = player.transform.position;
        Vector2 ropeDirection = (endPoint - playerPos).normalized;
        float distance = Vector2.Distance(endPoint, playerPos);
        
        // 거미줄과 로프는 다른 물리 법칙 적용
        if (isRope)
        {
            // 로프: 원운동 - 구심력 = mv²/r
            ApplyRopePhysics(playerPos, ropeDirection, distance);
        }
        else
        {
            // 거미줄: 일정한 장력
            ApplyWebPhysics(playerPos, ropeDirection, distance);
        }
    }
    
    private void ApplyWebPhysics(Vector2 playerPos, Vector2 ropeDirection, float distance)
    {
        // 거미줄: 항상 중심(부착점) 방향으로 일정한 힘
        // 거리와 무관하게 항상 당기는 힘 적용
        Vector2 tensionForce = ropeDirection * webForce;
        AddForce(tensionForce);
        
        Debug.Log($"Web tension applied: {tensionForce}, distance: {distance}, currentLength: {currentLength}");
    }
    
    private void ApplyRopePhysics(Vector2 playerPos, Vector2 ropeDirection, float distance)
    {
        // 로프: 원운동 물리 (구심력 = mv²/r)

        // 로프 길이 제한 (플레이어가 로프 길이보다 멀리 가지 못하게)
        if (distance > currentLength)
        {
            Destroy(gameObject);
            return;
        }
        
        // 원운동을 위한 구심력 계산
        Vector2 playerVelocity = player.velocity;
        
        // 접선 방향 속도 (원운동 속도)
        Vector2 tangent = new Vector2(-ropeDirection.y, ropeDirection.x);
        float tangentialSpeed = Vector2.Dot(playerVelocity, tangent);
        
        // 구심력 = mv²/r (여기서는 질량을 1로 가정)
        if (distance > 0.1f) // 0으로 나누기 방지
        {
            float centripetalForce = (tangentialSpeed * tangentialSpeed) / distance;
            Vector2 centripetalForceVector = ropeDirection * centripetalForce;
            AddForce(centripetalForceVector);
            
            Debug.Log($"Rope centripetal force: {centripetalForceVector}, tangentialSpeed: {tangentialSpeed}, distance: {distance}");
        }
    }
    
    // 웹 부착 시 플레이어의 속도를 조정하는 함수
    private void AdjustPlayerVelocityOnAttach()
    {
        if (player == null) return;
        
        Vector2 playerPos = player.transform.position;
        Vector2 webDirection = (endPoint - playerPos).normalized; // 웹 방향 (중심 방향)
        Vector2 tangentDirection = new Vector2(-webDirection.y, webDirection.x); // 접선 방향 (웹과 수직)
        
        Vector2 playerVelocity = player.velocity;
        
        // 속도를 웹 방향과 접선 방향으로 분해
        float radialVelocity = Vector2.Dot(playerVelocity, webDirection); // 웹 방향 속도
        float tangentialVelocity = Vector2.Dot(playerVelocity, tangentDirection); // 접선 방향 속도
        
        // 접선 방향 속도를 80% 감소 (에너지 손실)
        tangentialVelocity *= reducedTangentSpeed; // 20%만 유지
        
        // 조정된 속도를 다시 합성
        Vector2 newVelocity = radialVelocity * webDirection + tangentialVelocity * tangentDirection;
        
        player.velocity = newVelocity;
        
        Debug.Log($"Web attached - Original velocity: {playerVelocity} → Adjusted velocity: {newVelocity}");
        Debug.Log($"Radial: {radialVelocity:F2}, Tangential: {tangentialVelocity:F2}");
    }

    void OnDestroy()
    {
        // 웹이 파괴될 때 플레이어의 currentWeb 참조 해제
        if (player != null && player.currentWeb == this)
        {
            player.currentWeb = null;
        }
    }
}
