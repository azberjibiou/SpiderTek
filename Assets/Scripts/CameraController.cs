using UnityEngine;

public class CameraController : MonoBehaviour
{
    // --- Fields ---
    public Transform player;           // 플레이어 Transform
    public Vector3 offset = new Vector3(0, 2, -10); // 카메라 오프셋
    public float smoothSpeed = 0.125f; // 부드러운 따라가기 속도
    public bool followPlayer = true;   // 플레이어 따라가기 활성화
    
    // 카메라 경계 설정 (선택사항)
    public bool useBounds = false;
    public float minX, maxX, minY, maxY;
    
    // --- Unity Methods ---
    void Start()
    {
        // 플레이어를 자동으로 찾기
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("Player를 찾을 수 없습니다! Player 태그를 확인하세요.");
            }
        }
    }
    
    void LateUpdate()
    {
        // LateUpdate: 모든 오브젝트 이동이 끝난 후 카메라 이동
        if (followPlayer && player != null)
        {
            FollowPlayer();
        }
    }
    
    // --- Private Methods ---
    void FollowPlayer()
    {
        // 목표 위치 계산
        Vector3 targetPosition = player.position + offset;
        
        // 경계 제한 적용 (활성화된 경우)
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }
        
        // 부드럽게 이동 (Lerp 사용)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
    
    // --- Public Methods ---
    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
    }
    
    public void SetFollowSpeed(float speed)
    {
        smoothSpeed = Mathf.Clamp01(speed); // 0~1 사이로 제한
    }
    
    public void EnableFollow(bool enable)
    {
        followPlayer = enable;
    }
    
    // 즉시 플레이어 위치로 이동 (순간이동)
    public void SnapToPlayer()
    {
        if (player != null)
        {
            transform.position = player.position + offset;
        }
    }
    
    // 카메라 흔들림 효과 (GameManager에서 호출)
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }
    
    private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            
            transform.position = originalPos + new Vector3(offsetX, offsetY, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPos;
    }
}
