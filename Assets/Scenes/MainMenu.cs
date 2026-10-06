using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    
    [SerializeField] Animator animator;

    public void StartGame()
    {
        animator.enabled = true;  
        GameManager.Instance.StartGame();
        StartCoroutine(CoroutineStartGame(3f));   
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    IEnumerator CoroutineStartGame(float duration) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            yield return null;
        }
        SceneManager.LoadScene("GameLevel");    
    }

}
