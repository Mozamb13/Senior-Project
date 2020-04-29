using System.Collections.Generic;
using UnityEngine;
using System;

public class Enemy : Base_AI
{
    public RagdollController ragdoll;
    
    protected override void InitializeFSM()
    {
        //sets the states that the enemy can use
        var states = new Dictionary<Type, BaseState>
        {
            { typeof(IdleState), new IdleState(this) },
            //{ typeof(ChaseState), new ChaseState(this) },
            //{ typeof(AttackState), new AttackState(this, enemyManager) },
            { typeof(DeathState), new DeathState(this) },
            //{ typeof(WanderState), new WanderState(this) },
            { typeof(DamagedState), new DamagedState(this) },
            //{ typeof(SearchState), new SearchState(this) }
        };

        fsm.SetStates(states);
    }

    //sets the ai's speed
    public override void SetSpeed(float _speed)
    {
        base.SetSpeed(_speed);
    }

    //sets the navmesh destination
    public override void SetDestination(Vector3 _destination)
    {
        base.SetDestination(_destination);
    }

    //sets the navmesh stopping distance
    public override void SetStoppingDist(float _stopDist)
    {
        base.SetStoppingDist(_stopDist);
    }

    //kills the ai
    public override void Die()
    {
        StaticVars.DeathAction(gameObject);
        ragdoll.ToggleColliders();
        ragdoll.ToggleRigidbodies();
        anim.enabled = false;
    }
}