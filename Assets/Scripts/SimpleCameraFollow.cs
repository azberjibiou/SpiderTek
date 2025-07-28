using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target;          // 따라갈 대상 (플레이어)
    public Vector3 offset = new Vector3(0, 2, -10); // 카메라 오프셋
    public float smoothTime = 0.3f;   // 부드러운 이동 시간
    
    private Vector3 velocity = Vector3.zero; // SmoothDamp용 속도
    
    void Start()
    {
        // 플레이어 자동 찾기
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // 목표 위치 계산
        Vector3 targetPosition = target.position + offset;
        
        // 부드럽게 이동
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
