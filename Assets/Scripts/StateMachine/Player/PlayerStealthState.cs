using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStealthState : PlayerState
{

    private bool isCrouching = false;

    public PlayerStealthState(Player player, PlayerStateMachine playerStateMachine) : base(player, playerStateMachine)
    {
        
    }

    public override void EnterState()
    {
        base.EnterState();
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
    }

    public override void PhysicsUpdate() 
    {
        base.PhysicsUpdate();
    }

    public override void PlayerInput()
    {
        base.PlayerInput();

        if(player.dialogueBlock) return;

        if (Keyboard.current.shiftKey.isPressed) 
        { 
            isCrouching = true; 
            player.animator.SetBool("isCrouching", true); 
            return; 
        }
        else{
            isCrouching = false; 
            player.animator.SetBool("isCrouching", false); 
        }  

        float h = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
        float v = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);

        Vector3 move = new Vector3(h, 0f, v);
        move = Vector3.ClampMagnitude(move, 1f) * player.moveSpeed;

        bool walking = move.sqrMagnitude > 0.01f;
        if(player.animator) player.animator.SetBool(player.IsWalkingHash, walking);

        move.y = -9.81f;

        if(!isCrouching)
            player.cc.Move(move * Time.deltaTime);

        Vector3 s = player.transform.localScale;
        s.x = h >= 0 ? 1 : -1;
        player.transform.localScale = s;

    }
    
    public override void AnimationTriggerEvent(Player.AnimationTriggerType triggeType)
    {
        base.AnimationTriggerEvent(triggeType);
    }

}
