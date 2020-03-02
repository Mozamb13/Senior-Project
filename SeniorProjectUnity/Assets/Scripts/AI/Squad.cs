using System.Collections.Generic;
using UnityEngine;
using System;

public class Squad : Base_AI
{
    public enum UnitType { SPOTTER, SNIPER, STRONG }
    
    [Header("Squad Variables")]
    public UnitType unitType;
    public bool givenOrder, recalled;
    public GameObject currentOrder, ramHurtBox;
    public float followDistance = 10, ramOffset = 1.5f;
    public int healPower;
    public Health healTargetHealth;
    public SquadCommandUI scui;

    protected override void Awake()
    {
        ai.stoppingDistance = followDistance;
        base.Awake();
    }

    //sets the states that the squad member can use
    protected override void InitializeFSM()
    {
        var states = new Dictionary<Type, BaseState>
        {
            { typeof(IdleState), new IdleState(this, scui) },
            { typeof(ChaseState), new ChaseState(this) },
            { typeof(FollowState), new FollowState(this) },
            { typeof(AttackState), new AttackState(this) },
            { typeof(OrderState), new OrderState(this) },
            { typeof(DeathState), new DeathState(this) },
            { typeof(DamagedState), new DamagedState(this) },
            { typeof(SearchState), new SearchState(this) }
        };

        fsm.SetStates(states);
    }

    //sets the speed of the ai
    public override void SetSpeed(float _speed)
    {
        base.SetSpeed(_speed);
    }

    //sets the navmesh destination of the ai
    public override void SetDestination(Vector3 _destination)
    {
        base.SetDestination(_destination);
    }

    //sets the navmesh stopping distance of the ai
    public override void SetStoppingDist(float _stopDist)
    {
        base.SetStoppingDist(_stopDist);
    }

    //kills the ai
    public override void Die()
    {
        StaticVars.DeathAction(gameObject);
        anim.SetBool(StaticVars.dead, true);
    }

    //heals the player
    public void HealOther()
    {
        healTargetHealth.Heal(healPower);
    }
}
