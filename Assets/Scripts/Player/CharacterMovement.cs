using UnityEngine;
using UnityEngine.InputSystem;


public class CharacterMovement : MonoBehaviour
{
    [SerializeField] SoundDefinition alertSfx;


    // Start is called once before the fir st execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    void PlayerInput()
    {
    }

    public void OnFootstep(AnimationEvent evt)
    {
        AudioManager.Instance.PlaySFX3D(alertSfx, transform.position);
    }

    void Update()
    {   
    }

}
