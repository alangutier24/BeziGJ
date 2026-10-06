using UnityEngine;

public class PlayerIdleState : PlayerState
{

  public PlayerIdleState(Player player, PlayerStateMachine playerStateMachine) : base(player, playerStateMachine)
    {
        
    }
    public override void EnterState()
    {
        base.EnterState();
        player.IndoorCameraStart();
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        player.IndoorCameraUpdate();
    }

    public override void PhysicsUpdate() 
    {
        base.PhysicsUpdate();
    }

    public override void PlayerInput()
    {
        base.PlayerInput();
    }

    public override void AnimationTriggerEvent(Player.AnimationTriggerType triggeType)
    {
        base.AnimationTriggerEvent(triggeType);
    }

}
