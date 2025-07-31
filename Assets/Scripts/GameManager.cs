using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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
            
            // 씬 로드 이벤트 등록
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 초기 씬 설정
        InitializeForCurrentScene();
    }
    
    void OnDestroy()
    {
        // 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"[GAMEMANAGER] Scene loaded: {scene.name}");
        // 새로운 씬이 로드될 때마다 초기화
        InitializeForCurrentScene();
    }
    
    void InitializeForCurrentScene()
    {
        // 플레이어 찾기 및 시작 위치 설정
        player = FindObjectOfType<Player>();
        if (player != null)
        {
            playerStartPosition = player.transform.position;
        }

        // 효과음용 AudioSource 설정 (없는 경우에만)
        if (sfxSource == null)
        {
            SetupSFXAudioSource();
        }

        // 기존 음악 정지 및 새로운 씬의 배경음악 시작
        StopCurrentMusic();
        StartBackgroundMusicForCurrentScene();

        // 일시정지 UI 찾기 또는 생성
        SetupPauseUI();
        
        // 게임 상태 초기화
        isPaused = false;
        Time.timeScale = 1f;
    }
    
    void Update()
    {
        // ESC 키로 일시정지/재개 처리
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
    void StopCurrentMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            Destroy(musicSource);
            musicSource = null;
            Debug.Log("[MUSIC] Previous music stopped");
        }
    }
    
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
        // 기존 음악이 있으면 정지
        StopCurrentMusic();
        
        // 새로운 AudioSource 컴포넌트 추가
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
        // 효과음 전용 AudioSource 생성 (없는 경우에만)
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;
            sfxSource.spatialBlend = 0f; // 2D 오디오
            sfxSource.playOnAwake = false;
        }
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
    
    // --- UI Management ---
    void SetupPauseUI()
    {
        // 기존 일시정지 UI가 있으면 제거
        if (pauseUI != null)
        {
            Destroy(pauseUI);
        }
        
        // Canvas 찾기 또는 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 다른 UI보다 위에 표시
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // EventSystem 확인 및 생성
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            // 새로운 Input System을 위한 InputSystemUIInputModule 사용
            var inputModule = eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            
            Debug.Log("[UI] EventSystem with InputSystemUIInputModule created");
        }
        
        // 일시정지 패널 생성
        pauseUI = new GameObject("PausePanel");
        pauseUI.transform.SetParent(canvas.transform, false);
        
        // 배경 패널 (반투명 검은색)
        UnityEngine.UI.Image backgroundImage = pauseUI.AddComponent<UnityEngine.UI.Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.8f);
        RectTransform backgroundRect = pauseUI.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        
        // 메뉴 컨테이너
        GameObject menuContainer = new GameObject("MenuContainer");
        menuContainer.transform.SetParent(pauseUI.transform, false);
        RectTransform containerRect = menuContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(400, 500);
        
        // 배경 박스 추가 (메뉴 컨테이너용)
        UnityEngine.UI.Image containerBg = menuContainer.AddComponent<UnityEngine.UI.Image>();
        containerBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // 세로 레이아웃 그룹
        UnityEngine.UI.VerticalLayoutGroup layoutGroup = menuContainer.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layoutGroup.spacing = 30;
        layoutGroup.padding = new RectOffset(40, 40, 40, 40);
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        
        // ContentSizeFitter 추가
        UnityEngine.UI.ContentSizeFitter sizeFitter = menuContainer.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        sizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        
        // 제목 텍스트
        CreatePauseMenuText(menuContainer, "게임 일시정지", 28);
        
        // 버튼들 생성
        CreatePauseMenuButton(menuContainer, "계속하기", Resume);
        CreatePauseMenuButton(menuContainer, "재시작", RestartLevel);
        CreatePauseMenuButton(menuContainer, "메인 메뉴", LoadMainMenu);
        
        // 초기에는 비활성화
        pauseUI.SetActive(false);
        
        Debug.Log("[UI] Pause menu created with buttons");
    }
    
    GameObject CreatePauseMenuText(GameObject parent, string text, int fontSize)
    {
        GameObject textObj = new GameObject("Text_" + text);
        textObj.transform.SetParent(parent.transform, false);
        
        UnityEngine.UI.Text textComponent = textObj.AddComponent<UnityEngine.UI.Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(320, fontSize + 20);
        
        // LayoutElement 추가
        UnityEngine.UI.LayoutElement layoutElement = textObj.AddComponent<UnityEngine.UI.LayoutElement>();
        layoutElement.preferredHeight = fontSize + 20;
        
        Debug.Log($"[UI] Created text: {text}");
        return textObj;
    }
    
    GameObject CreatePauseMenuButton(GameObject parent, string buttonText, System.Action onClickAction)
    {
        GameObject buttonObj = new GameObject("Button_" + buttonText);
        buttonObj.transform.SetParent(parent.transform, false);
        
        // 버튼 이미지 (배경)
        UnityEngine.UI.Image buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // 버튼 컴포넌트
        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = buttonImage;
        
        // 클릭 이벤트 등록
        button.onClick.AddListener(() => {
            Debug.Log($"[UI] Button clicked: {buttonText}");
            onClickAction?.Invoke();
        });
        
        // 버튼 크기 설정
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(320, 60);
        
        // LayoutElement 추가
        UnityEngine.UI.LayoutElement layoutElement = buttonObj.AddComponent<UnityEngine.UI.LayoutElement>();
        layoutElement.preferredHeight = 60;
        layoutElement.preferredWidth = 320;
        
        // 버튼 텍스트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        UnityEngine.UI.Text textComponent = textObj.AddComponent<UnityEngine.UI.Text>();
        textComponent.text = buttonText;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 20;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // 버튼 호버 효과
        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        button.colors = colors;
        
        Debug.Log($"[UI] Created button: {buttonText}");
        return buttonObj;
    }
}
