using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; 
using UnityEngine.UI;

public class PlayerAttackState : PlayerState, IShaderSource
{
    
    protected float healTime = 3f;
    protected float healTimer = 0f;
    private float healthMulti = 2f;

    public float VignetteTotal {get; private set;}
    public float Chroma {get; private set;}
    public float ColorSaturation {get; private set;}
    public float VignetteSmooth {get; private set;}

    public PlayerAttackState(Player player, PlayerStateMachine playerStateMachine) : base(player, playerStateMachine)
    {
        
    }
    public override void EnterState()
    {
        base.EnterState();
         ShaderManager.Instance.Register(this);
        player.SetCameraFPS();
        player.bar.enabled = true;
    }

    public override void ExitState()
    {
        base.ExitState();
        ShaderManager.Instance.Unregister(this);
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        //Debug.Log("Frame Update");

        if(Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }


        float t = player.CurrentHealth / player.MaxHealth;
        player.bar.fillAmount = t;

        if (player.CurrentHealth >= player.MaxHealth)
        {
            healTimer = 0;
            return;
        }

        RecieveDamage();



        Debug.Log("Player health: " + player.CurrentHealth + " t: " + t);
        Debug.Log($"{t} -> {player.bar.fillAmount}", player.bar);
        //healTimer += Time.deltaTime;
        if(healTimer > healTime)
        {
        //    player.CurrentHealth += Time.deltaTime * healthMulti;
        }
    }

    public override void PhysicsUpdate() 
    {
        base.PhysicsUpdate();



    }

    public override void PlayerInput()
    {
        base.PlayerInput();
        
        if(player.dialogueBlock) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) Shoot();

        float h = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
        float v = (Keyboard.current.aKey.isPressed ? 1f : 0f) - (Keyboard.current.dKey.isPressed ? 1f : 0f);

        Vector3 move = new Vector3(h, 0f, v);
        move = Vector3.ClampMagnitude(move, 1f) * player.moveSpeed;

        bool walking = move.sqrMagnitude > 0.01f;
        if(player.animator) player.animator.SetBool(player.IsWalkingHash, walking);

        move.y = -9.81f;

        player.cc.Move(move * Time.deltaTime);

        Vector3 s = player.transform.localScale;
        s.x = h >= 0 ? 1 : -1;
        player.transform.localScale = s;
    }

    public override void AnimationTriggerEvent(Player.AnimationTriggerType triggeType)
    {
        base.AnimationTriggerEvent(triggeType);
    }


    void OnEnable()
    {
        ShaderManager.Instance.Register(this);
    }

    void OnDisable()
    {
        ShaderManager.Instance.Unregister(this);
    }

    void Shoot()
    {
        player.Shoot();
    }

    public void RecieveDamage()
    {
        if(player.CurrentHealth < player.MaxHealth)
        {
            float t = Mathf.Clamp01(player.CurrentHealth);
            float pulse = Mathf.Sin(Time.time * 6f) * 0.5f + 0.5f;
            float peak  = Mathf.Lerp(0.35f, 0.6f, pulse);
            VignetteTotal   = t * peak;
            VignetteSmooth  = t * peak;
            Chroma          = t;
            ColorSaturation = -70f * t;
        }
        else
        {
            VignetteTotal = 0f;
            VignetteSmooth = 0f;
            Chroma = 0f;
            ColorSaturation = 0f;
        }
    }

    public void Damage(float amount)
    {

        player.CurrentHealth -= amount;

        if(player.CurrentHealth <=0)
        {
            player.Die();
        }
    }

}
