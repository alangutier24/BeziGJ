using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider sliderMusic;
    [SerializeField] Slider sliderSfx;
    [SerializeField] string musicParam = "MusicVol";
    [SerializeField] string sfxParam = "SfxVolume";
    [SerializeField] GameObject pauseMenu;

    private bool pause = false;

    void Update()
    {
        if(Keyboard.current.pKey.wasPressedThisFrame)
            pause = !pause;

        if(pause)
        {
            Time.timeScale = 0f;
            pauseMenu.SetActive(true);
        }
        else
        {
            pauseMenu.SetActive(false);
            Time.timeScale = 1f;
        }
        
    }

    void Start()
    {

    }

    public void SetMusicVolume(float value)
    {
        mixer.SetFloat(musicParam, Mathf.Log10(sliderMusic.value)*20);
    }

    public void SetSfxVolume(float value)
    {
        Debug.Log("Set SFX");
        mixer.SetFloat(sfxParam, Mathf.Log10(sliderSfx.value)*20);
    }
}
