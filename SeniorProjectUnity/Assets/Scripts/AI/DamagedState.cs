using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DamagedState : BaseState
{

    public DamagedState(Base_AI _ai) : base(_ai.gameObject, _ai) {}

    public override Type Tick()
    {
        //if dead die
        if (!health.alive)
            return typeof(DeathState);
        //if no longer hurt and found an enemy, chase
        else if (!ai.damaged)
            return typeof(IdleState);
        else
        {
            ai.anim.SetBool(StaticVars.damaged, true);
            return typeof(DamagedState);
        }
    }
}
