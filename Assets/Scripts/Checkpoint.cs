using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    // --- Fields ---
    public Vector2 position;
    public bool isActivated = false;
    
    private Animator flagAnimator;
    private SpriteRenderer spriteRenderer;
    private Color inactiveColor = Color.gray;
    private Color activeColor = Color.green;
    
    // --- Unity Methods ---
    void Start()
    {
        // 체크포인트의 초기 위치 설정
        position = transform.position;
        
        // 컴포넌트 초기화
        flagAnimator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 초기 상태 설정
        UpdateVisualState();
    }
    
    void Update()
    {
        // 깃발 올라가는 애니메이션 처리
        if (isActivated && flagAnimator != null)
        {
            flagAnimator.SetBool("IsActivated", true);
        }
    }
    
    // --- Collision Handling ---
    void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌 시 체크포인트 활성화
        if (other.gameObject.CompareTag("Player") && !isActivated)
        {
            ActivateCheckpoint();
            
            // GameManager에게 체크포인트 활성화 알림
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ActivateCheckpoint(this);
            }
            else
            {
                Debug.LogWarning("GameManager를 찾을 수 없습니다!");
            }
        }
    }
    
    // --- Public Methods ---
    public Vector2 GetPosition()
    {
        return position;
    }
    
    public void SetPosition(Vector2 newPosition)
    {
        position = newPosition;
        transform.position = position;
    }
    
    public bool IsActivated()
    {
        return isActivated;
    }
    
    public void ActivateCheckpoint()
    {
        if (!isActivated)
        {
            isActivated = true;
            UpdateVisualState();
            
            // 활성화 효과 (예: 파티클, 사운드 등)
            PlayActivationEffect();
            
            Debug.Log($"체크포인트 {gameObject.name} 활성화!");
        }
    }
    
    public void DeactivateCheckpoint()
    {
        isActivated = false;
        UpdateVisualState();
        
        if (flagAnimator != null)
        {
            flagAnimator.SetBool("IsActivated", false);
        }
    }
    
    // --- Private Methods ---
    private void UpdateVisualState()
    {
        // 스프라이트 색상 변경으로 활성화 상태 표시
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActivated ? activeColor : inactiveColor;
        }
    }
    
    private void PlayActivationEffect()
    {
        // 여기에 파티클 효과나 사운드 재생 로직 추가 가능
        // 예시: 간단한 스케일 애니메이션
        StartCoroutine(ActivationAnimation());
    }
    
    private System.Collections.IEnumerator ActivationAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        // 크기 증가
        float elapsed = 0f;
        float duration = 0.2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        // 크기 복원
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
}
