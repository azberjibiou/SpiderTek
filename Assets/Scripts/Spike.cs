using UnityEngine;

public class Spike : MonoBehaviour
{
    // --- Fields ---
    public Vector2 position;
    public float rotation;
    
    // --- Unity Methods ---
    void Start()
    {
        // 가시의 초기 위치와 회전 설정
        position = transform.position;
        rotation = transform.rotation.z;
    }
    
    void Update()
    {
        // 가시는 기본적으로 정적이므로 특별한 업데이트 없음
        // 필요시 회전 애니메이션이나 특수 효과 추가 가능
    }
    
    // --- Collision Handling ---
    void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌 시 GameManager에게 플레이어 사망 알림
        if (other.gameObject.CompareTag("Player"))
        {
            // GameManager의 PlayerDie 함수 호출
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
    
    public float GetRotation()
    {
        return rotation;
    }
    
    public void SetRotation(float newRotation)
    {
        rotation = newRotation;
        transform.rotation = Quaternion.Euler(0, 0, rotation);
    }
}
