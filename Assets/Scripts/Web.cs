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
    private const float extendSpeed = 30f;
    private const float ropeForce = 100f;      // 로프의 제약력 (강함)
    private const float webForce = 60f;       // 거미줄의 장력 (일정함)
    
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
        // 줄의 끝점 발사
        transform.position += (Vector3)(velocity * Time.fixedDeltaTime);
        currentLength = Vector2.Distance(startPoint, transform.position);
        
        // 최대 길이에 도달하면 웹 파괴
        if (currentLength >= maxLength)
        {
            Destroy(gameObject);
            return;
        }
        
        // 충돌 검사 (벽이나 블록에 닿으면 부착)
        Collider2D hitCollider = Physics2D.OverlapCircle(transform.position, 0.1f);
        if (hitCollider != null && 
            (hitCollider.gameObject.CompareTag("Block") || 
             hitCollider.gameObject.name.Contains("Block")))
        {
            isAttached = true;
            velocity = Vector2.zero;
            
            // 블록의 가장 가까운 표면에 부착
            Vector2 webPosition = transform.position;
            Vector2 blockCenter = hitCollider.bounds.center;
            Vector2 blockSize = hitCollider.bounds.size;
            
            // Web이 블록의 어느 면에 가장 가까운지 계산
            float distToLeft = Mathf.Abs(webPosition.x - (blockCenter.x - blockSize.x / 2f));
            float distToRight = Mathf.Abs(webPosition.x - (blockCenter.x + blockSize.x / 2f));
            float distToTop = Mathf.Abs(webPosition.y - (blockCenter.y + blockSize.y / 2f));
            float distToBottom = Mathf.Abs(webPosition.y - (blockCenter.y - blockSize.y / 2f));
            
            float minDist = Mathf.Min(distToLeft, distToRight, distToTop, distToBottom);
            
            // 가장 가까운 면의 가장자리에 부착
            if (minDist == distToLeft)
                endPoint = new Vector2(blockCenter.x - blockSize.x / 2f, webPosition.y);
            else if (minDist == distToRight)
                endPoint = new Vector2(blockCenter.x + blockSize.x / 2f, webPosition.y);
            else if (minDist == distToTop)
                endPoint = new Vector2(webPosition.x, blockCenter.y + blockSize.y / 2f);
            else
                endPoint = new Vector2(webPosition.x, blockCenter.y - blockSize.y / 2f);
            
            // Web 오브젝트 위치도 부착점으로 이동
            transform.position = endPoint;
        }
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
            // 플레이어를 로프 길이 내로 당김
            Vector2 constraintForce = ropeDirection * ropeForce * 2f; // 강한 제약력
            AddForce(constraintForce);
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
    
    void OnDestroy()
    {
        // 웹이 파괴될 때 플레이어의 currentWeb 참조 해제
        if (player != null && player.currentWeb == this)
        {
            player.currentWeb = null;
        }
    }
}
