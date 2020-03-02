using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DeathState : BaseState
{
    private bool died;
    public DeathState(Base_AI _ai) : base(_ai.gameObject, _ai)
    {
        
    }

    public override Type Tick()
    {
        //if alive go to idle
        if(health.alive)
        {
            ai.anim.SetBool(StaticVars.dead, false);
            return typeof(IdleState);
        }

        if (!died)
        {
            ai.Die();
            died = true;
        }
        
        return typeof(DeathState);
    }
}
