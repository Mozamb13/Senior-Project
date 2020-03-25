using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FollowState : BaseState
{
    private Squad squad;
    private GameObject player;
    private bool offsetCalculated;

    public FollowState(Squad _ai) : base(_ai.gameObject, _ai)
    {
        //makes sure the ai is a squad
        squad = _ai;
        player = ai.player;
    }

    public override Type Tick()
    {
        //if dead die
        if (!health.alive)
            return typeof(DeathState);
        //if damaged go to that state
        else if (squad.damaged)
            return typeof(DamagedState);
        //if given order go to that state and make sure they're not still being recalled
        else if (squad.givenOrder && squad.currentOrder != null)
        {
            squad.recalled = false;
            return typeof(OrderState);
        }
        //if ai found enemy and not in range, chase
        else if (squad.enemyFound && !squad.recalled && Vector3.Distance(squad.currentTarget.transform.position, squad.transform.position) > squad.stats.range)
            return typeof(ChaseState);
        //if ai found enemy and in range, attack
        else if (squad.enemyFound && !squad.recalled && Vector3.Distance(squad.currentTarget.transform.position, squad.transform.position) <= squad.stats.range)
        {
            ai.anim.SetBool(StaticVars.walk, false);
            return typeof(AttackState);
        }
        //if ai is close to player and has nothing to do, idle
        else if (Vector3.Distance(squad.transform.position, player.transform.position) <= squad.followDistance)
        {
            squad.recalled = false;
            ai.anim.SetBool(StaticVars.walk, false);
            return typeof(IdleState);
        }
        //if ai is too far from player, follow
        else if (Vector3.Distance(squad.transform.position, player.transform.position) > squad.followDistance)
        {
            squad.anim.SetBool(StaticVars.walk, true);
            squad.SetSpeed(squad.runSpeed);
            squad.SetStoppingDist(squad.followDistance);
            squad.SetDestination(player.transform.position);
            return typeof(FollowState);
        }
        else
            return null;
    }
}
