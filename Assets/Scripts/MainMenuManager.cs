using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // 게임 시작 버튼을 누르면 호출될 함수
    public void StartGame()
    {
        // "SampleScene"이라는 이름의 씬을 불러옵니다.
       // 만약 게임 플레이 씬의 이름이 다르다면, 이 부분을 수정해야 합니다.
        SceneManager.LoadScene("Demo");
    }

    // 게임 종료 버튼을 누르면 호출될 함수
    public void QuitGame()
    {
        // 에디터에서 테스트할 때는 로그를 출력하고,
        // 실제 빌드된 게임에서는 프로그램을 종료합니다.
        Debug.Log("게임을 종료합니다.");
        Application.Quit();
    } 
}