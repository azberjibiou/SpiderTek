using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("포탈 설정")]
    public string nextSceneName = "FirstScene"; // 이동할 씬 이름
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"포탈에 무언가 닿았습니다: {other.name}, 태그: {other.tag}");
        
        // 플레이어가 포탈에 닿으면
        if (other.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 포탈에 닿았습니다! 씬 '{nextSceneName}'으로 이동합니다.");
            // 다음 씬으로 이동
            SceneManager.LoadScene(nextSceneName);
        }
    }
    
    // 추가: OnCollisionEnter2D도 시도해보기
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"포탈에 충돌했습니다: {collision.gameObject.name}, 태그: {collision.gameObject.tag}");
        
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 포탈에 충돌했습니다! 씬 '{nextSceneName}'으로 이동합니다.");
            SceneManager.LoadScene(nextSceneName);
        }
    }
} 