using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static GameManager Instance { get; private set; }
    
    // --- Static Fields ---
    public static float gravity = -45f; // 게임 전체 중력값
    
    // --- Fields ---
    public Checkpoint currentCheckpoint;
    public Vector2 playerStartPosition;
    public bool isPaused = false;
    public bool isPlayerDead = false;
    public int playerRespawnFrame = 0;
    
    [Header("Audio Settings")]
    public float musicVolume = 0.5f;
    public float sfxVolume = 0.7f;
    
    private Player player;
    private GameObject pauseUI;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private const int respawnDelay = 60; // 60프레임 = 1초 (60FPS 기준)
    
    // --- Unity Methods ---
    void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 플레이어 찾기 및 시작 위치 설정
        player = FindObjectOfType<Player>();
        if (player != null)
        {
            playerStartPosition = player.transform.position;
        }

        // 효과음용 AudioSource 설정
        SetupSFXAudioSource();

        // 씬별 배경음악 시작
        StartBackgroundMusicForCurrentScene();

        // 일시정지 UI 찾기 (Canvas 하위의 PausePanel 등)
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            pauseUI = canvas.transform.Find("PausePanel")?.gameObject;
            if (pauseUI != null)
            {
                pauseUI.SetActive(false);
            }
        }
    }
    
    void Update()
    {
        // ESC 처리 (Player에서도 처리하지만 GameManager에서도 처리)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
        
        // 플레이어 부활 카운트다운
        if (isPlayerDead && playerRespawnFrame > 0)
        {
            playerRespawnFrame--;
            if (playerRespawnFrame <= 0)
            {
                RespawnPlayer();
            }
        }
    }
    
    // --- Public Methods ---
    public void PlayerDie()
    {
        if (isPlayerDead) return; // 이미 죽은 상태면 무시
        
        Debug.Log("플레이어 사망!");
        isPlayerDead = true;
        playerRespawnFrame = respawnDelay;
        
        // 죽음 효과음 재생
        PlayDeathSound();
        
        // 플레이어 상태 변경
        if (player != null)
        {
            player.isDead = true;
            player.velocity = Vector2.zero;
            
            // 현재 웹/로프 제거
            if (player.currentWeb != null)
            {
                Destroy(player.currentWeb.gameObject);
                player.currentWeb = null;
            }
        }
        
        // 사망 효과 (예: 화면 흔들림, 사운드 등)
        PlayDeathEffect();
    }
    
    public void RespawnPlayer()
    {
        if (!isPlayerDead) return;
        
        Debug.Log("플레이어 부활!");
        isPlayerDead = false;
        playerRespawnFrame = 0;
        
        // 체크포인트 위치로 복원 (체크포인트가 없으면 시작 위치)
        Vector2 respawnPosition = currentCheckpoint != null ? 
            currentCheckpoint.GetPosition() : playerStartPosition;
        
        if (player != null)
        {
            player.isDead = false;
            player.transform.position = respawnPosition;
            player.position = respawnPosition;
            player.velocity = Vector2.zero;
            player.acceleration = Vector2.zero;
            player.isGrounded = false;
        }
        
        // 부활 효과
        PlayRespawnEffect();
    }
    
    public void ActivateCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null) return;
        // 이전 체크포인트 비활성화
        if (currentCheckpoint != null && currentCheckpoint != checkpoint)
        {
            currentCheckpoint.DeactivateCheckpoint();
        }
        
        // 새 체크포인트 활성화
        currentCheckpoint = checkpoint;
        checkpoint.ActivateCheckpoint();
        
        Debug.Log($"체크포인트 활성화: {checkpoint.gameObject.name}");
    }
    
    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f; // 게임 시간 정지
        
        if (pauseUI != null)
        {
            pauseUI.SetActive(true);
        }
        
        Debug.Log("게임 일시정지");
    }
    
    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f; // 게임 시간 복원
        
        if (pauseUI != null)
        {
            pauseUI.SetActive(false);
        }
        
        Debug.Log("게임 재개");
    }
    
    public void RestartLevel()
    {
        // 현재 레벨 재시작
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void LoadNextLevel()
    {
        // 다음 레벨 로드 (씬 인덱스 + 1)
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("마지막 레벨입니다!");
        }
    }
    
    public void LoadMainMenu()
    {
        // 메인 메뉴로 돌아가기
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // 첫 번째 씬이 메인 메뉴라고 가정
    }
    
    // --- Private Methods ---
    private void PlayDeathEffect()
    {
        // 여기에 사망 효과 구현 (파티클, 사운드, 화면 흔들림 등)
        // CameraController를 사용한 카메라 흔들림
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            cameraController.Shake(0.5f, 0.1f);
        }
        else
        {
            // 기존 방식 (CameraController가 없는 경우)
            StartCoroutine(CameraShake(0.5f, 0.1f));
        }
    }
    
    private void PlayRespawnEffect()
    {
        // 여기에 부활 효과 구현 (파티클, 사운드 등)
        Debug.Log("부활 효과 재생");
    }
    
    private System.Collections.IEnumerator CameraShake(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        
        Vector3 originalPos = cam.transform.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            
            cam.transform.position = originalPos + new Vector3(offsetX, offsetY, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cam.transform.position = originalPos;
    }
    
    // --- Simple Music Management ---
    void StartBackgroundMusicForCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        string musicFileName = "";
        
        // 씬별 음악 파일 설정
        switch (currentSceneName)
        {
            case "FirstScene":
                musicFileName = "Audio/Music/Reach for the Summit";
                break;
            case "Demo":
                musicFileName = "Audio/Music/First step";
                break;
            default:
                Debug.Log($"[MUSIC] No background music configured for scene: {currentSceneName}");
                return;
        }
        
        StartBackgroundMusic(musicFileName, currentSceneName);
    }
    
    void StartBackgroundMusic(string musicPath, string sceneName)
    {
        // AudioSource 컴포넌트 추가
        musicSource = gameObject.AddComponent<AudioSource>();
        
        // 음악 파일 로드
        AudioClip gameMusic = Resources.Load<AudioClip>(musicPath);
        if (gameMusic != null)
        {
            musicSource.clip = gameMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.spatialBlend = 0f; // 2D 오디오
            musicSource.Play();
            
            Debug.Log($"[MUSIC] Background music '{gameMusic.name}' started in {sceneName}");
        }
        else
        {
            Debug.LogWarning($"[MUSIC] Music file not found: {musicPath}");
            Debug.LogWarning($"[MUSIC] Check if file exists at: Assets/Resources/{musicPath}.mp3");
        }
    }
    
    // --- Sound Effects Management ---
    void SetupSFXAudioSource()
    {
        // 효과음 전용 AudioSource 생성
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.spatialBlend = 0f; // 2D 오디오
        sfxSource.playOnAwake = false;
    }
    
    void PlayDeathSound()
    {
        // Resources에서 죽음 효과음 로드
        AudioClip deathSound = Resources.Load<AudioClip>("Audio/SFX/death");
        if (deathSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(deathSound);
            Debug.Log("[SFX] Death sound played");
        }
        else
        {
            Debug.LogWarning("[SFX] Death sound not found at: Audio/SFX/death");
            Debug.LogWarning("[SFX] Check if file exists at: Assets/Resources/Audio/SFX/death.mp3");
        }
    }
}
