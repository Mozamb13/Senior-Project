using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class IdleState : BaseState
{
    private Squad squad;
    private Enemy enemy;
    private SquadCommandUI scui;

    public IdleState(Base_AI _ai, SquadCommandUI _scui = null) : base(_ai.gameObject, _ai)
    {
        //checks if ai is enemy or squad
        switch (ai)
        {
            case Squad temp:
                squad = temp;
                scui = _scui;
                break;
            case Enemy ai1:
                enemy = ai1;
                break;
        }
    }

    public override Type Tick()
    {
        //activate line of sight
        Scan();
        
        //if dead die
        if (!health.alive)
            return typeof(DeathState);
        //if hurt go to damaged state
        else if (ai.damaged)
            return typeof(DamagedState);
        //if given order and is a squad member, go to order state
        else if (squad != null && squad.currentOrder != null && squad.givenOrder)
            return typeof(OrderState);
        //if found an enemy but out of range, chase
        else if (enemy == null && ai.enemyFound && Vector3.Distance(ai.currentTarget.transform.position, ai.transform.position) > ai.stats.range)
            return typeof(ChaseState);
        //if found an enemy and in range, attack
        else if (enemy == null && ai.enemyFound && Vector3.Distance(ai.currentTarget.transform.position, ai.transform.position) <= ai.stats.range)
            return typeof(AttackState);
        //if squad member nd too far from player, follow player
        else if (squad != null && Vector3.Distance(squad.transform.position, squad.player.transform.position) > squad.followDistance)
            return typeof(FollowState);
        //else if (enemy != null)
            //return typeof(WanderState);
        else
        {
            if(squad != null && scui != null)
                scui.ChangeOrder(3);
            return typeof(IdleState);
        }
    }
}
