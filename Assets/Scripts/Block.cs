using UnityEngine;

public class Block : MonoBehaviour
{
    // --- Fields ---
    public Vector2 position;
    
    // --- Unity Methods ---
    void Start()
    {
        // 블록의 초기 위치 설정
        position = transform.position;
    }
    
    void Update()
    {
        // 블록은 기본적으로 정적이므로 특별한 업데이트 없음
        // 필요시 애니메이션이나 특수 효과 추가 가능
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
}