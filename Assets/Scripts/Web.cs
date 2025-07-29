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
    private float initialLength = 0f;          // 웹 부착 시 초기 길이
    private const float extendSpeed = 100f;
    private const float ropeForce = 100f;      // 로프의 제약력 (강함)
    private const float webForce = 200f;       // 거미줄의 장력 (강함)
    private const float springConstant = 100f;  // 로프의 용수철 상수 (k)
    private const float swingAcceleration = 15f; // 로프 스윙 시 접선 가속도
    private const float minWebLength = 2f;     // 최소 거미줄 길이 (이하면 파괴)
    private const float reducedTangentSpeed = 0.0f; // 접선 속도 감소 비율 (0% 유지)

    // 새로운 예측 충돌 시스템
    private bool hasPredictedCollision = false;
    private Vector2 predictedHitPoint;
    private float timeToCollision = 0f;
    private bool attachableBlock = false; // 블록이 부착 가능한지 여부
    private float collisionTimer = 0f;
    private bool nextDestroy = false; // 다음 프레임에서 파괴 여부
    
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
        Debug.Log($"Web initialized from {startPos} to {targetPos}, isRope: {rope}");
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
            Vector2 oldVelocity = player.velocity;
            player.velocity += force * Time.fixedDeltaTime;
            Vector2 deltaVelocity = player.velocity - oldVelocity;
        }
    }
    
    public void Extend()
    {
        Debug.Log($"Extending web from {startPoint} to {endPoint}, current length: {currentLength:F2}, nextDestroy: {nextDestroy}");
        if (nextDestroy)
        {
            Destroy(gameObject);
            return;
        }
        // 웹 발사체 이동
        if(hasPredictedCollision)
        {
            // 예측된 충돌 시간까지 이동
            transform.position += (Vector3)(velocity * Mathf.Min(Time.fixedDeltaTime, timeToCollision - collisionTimer));
        }
        else
        {
            // 예측된 충돌이 없으면 그냥 이동
            transform.position += (Vector3)velocity * Time.fixedDeltaTime;
        }
        currentLength = Vector2.Distance(startPoint, transform.position);

        // 충돌 예측이 있는 경우 타이머 업데이트
        if (hasPredictedCollision)
        {
            collisionTimer += Time.fixedDeltaTime;
            Debug.Log($"Collision timer: {collisionTimer:F4}, Time to collision: {timeToCollision:F4}");
            // 예상 충돌 시간에 도달하면 부착
            if (collisionTimer > timeToCollision)
            {
                if (attachableBlock)
                {
                    AttachToSurface(predictedHitPoint);
                }
                else
                {
                    nextDestroy = true; // 다음 프레임에 파괴
                }
                return;
            }
        }
        
        
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
        bool nonGrippable = false;
        foreach (var hitInfo in allHits)
        {
            // NonGrippableBlock이면 건너뛰기
            string objectName = hitInfo.collider.gameObject.name;
            if (hitInfo.collider.gameObject.CompareTag("Block"))
            {
                hit = hitInfo;
                attachableBlock = !objectName.Contains("nonGrippable");
                break; // 첫 번째 블록에서 중단
            }
        }

        if (hit.collider != null)
        {
            hasPredictedCollision = true;
            predictedHitPoint = hit.point;
            float distanceToHit = Vector2.Distance(startPoint, hit.point);
            timeToCollision = distanceToHit / speed;
            Debug.Log($"Predicted collision at {predictedHitPoint}, time to collision: {timeToCollision:F4}, attachable: {attachableBlock}");
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
        
        // 부착 시 초기 길이 계산 (플레이어와 부착점 사이의 거리)
        if (player != null)
        {
            initialLength = Vector2.Distance(player.transform.position, attachPoint);
        }
        
        // 웹 부착 시 플레이어 속도 조정
        AdjustPlayerVelocityOnAttach();
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
            // 로프: 팽팽한 실 (거리 제한)
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
        
    }
    
    private void ApplyRopePhysics(Vector2 playerPos, Vector2 ropeDirection, float distance)
    {
        // 로프: 팽팽한 실 (거리 제한) + 회전 가속도
        
        // 1. 거리 제한 (팽팽한 실 효과) - 속도 조정만으로 제약 적용
        if (distance > initialLength)
        {
            // 로프에서 멀어지려는 속도 성분만 제거 (위치는 직접 수정하지 않음)
            Vector2 playerVelocity = player.velocity;
            float radialVelocity = Vector2.Dot(playerVelocity, -ropeDirection); // 로프에서 멀어지는 방향의 속도
            
            if (radialVelocity > 0) // 로프에서 멀어지려는 속도만 제거
            {
                Vector2 radialVelocityVector = -ropeDirection * radialVelocity;
                player.velocity -= radialVelocityVector;
                
                Debug.Log($"[ROPE CONSTRAINT] Removed radial velocity: {radialVelocityVector}, New velocity: {player.velocity}");
            }
        }
        
        // 2. 플레이어가 로프 길이보다 훨씬 멀리 있을 때 추가 보정 (강한 당기는 힘)
        if (distance > initialLength * 1.1f) // 10% 이상 벗어나면 추가 보정력 적용
        {
            Vector2 correctionForce = ropeDirection * ropeForce * (distance - initialLength);
            AddForce(correctionForce);
            
            Debug.Log($"[ROPE CORRECTION] Strong pull force applied: {correctionForce}");
        }
        
        // 2. 회전 가속도 (접선 방향)
        if (player != null)
        {
            Vector2 tangentDirection = new Vector2(ropeDirection.y, -ropeDirection.x);
            float horizontalInput = player.moveInput;
            Vector2 swingForce = tangentDirection * (1.2f * horizontalInput * swingAcceleration);
            
            Debug.Log($"[ROPE SWING] Player moveInput: {horizontalInput:F2}, Tangent dir: {tangentDirection}, Swing force: {swingForce}");
            
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                AddForce(swingForce);
            }
        }
        
        Debug.Log($"[ROPE CONSTRAINT] Current: {distance:F2}, Max allowed: {initialLength:F2}, Constraint active: {distance > initialLength}");
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

        // 거미줄의 접선 방향 속도를 100% 감소 (에너지 손실)
        if(!isRope) tangentialVelocity *= reducedTangentSpeed; // 0%만 유지
        
        // 조정된 속도를 다시 합성
        Vector2 newVelocity = radialVelocity * webDirection + tangentialVelocity * tangentDirection;
        
        player.velocity = newVelocity;
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
