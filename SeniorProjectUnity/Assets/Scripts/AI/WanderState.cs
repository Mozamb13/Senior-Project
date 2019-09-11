﻿using System;
using UnityEngine;

public class WanderState : BaseState
{
    Squad squad;
    Enemy enemy;
    bool wandering;

    public WanderState(Base_AI _ai) : base(_ai.gameObject, _ai)
    {
        switch (ai)
        {
            case Squad temp:
                squad = temp;
                break;
            case Enemy temp:
                enemy = temp;
                break;
        }
    }

    public override Type Tick()
    {
        Scan();
        if (ai.damaged)
            return typeof(DamagedState);
        else if (ai.enemyFound && Vector3.Distance(ai.currentTarget.transform.position, ai.transform.position) > ai.stats.range)
            return typeof(ChaseState);
        else if (ai.enemyFound && Vector3.Distance(ai.currentTarget.transform.position, ai.transform.position) <= ai.stats.range)
            return typeof(AttackState);
        else
        {
            if (!wandering)
            {
                wandering = true;
                Vector3 randDest = Vector3.zero;
                randDest.x = UnityEngine.Random.Range(-5, 5);
                randDest.z = UnityEngine.Random.Range(-5, 5);
                ai.SetDestination(randDest);
                ai.SetStoppingDist(0);
            }
            else if (ai.ai.remainingDistance <= 0.1f)
                wandering = false;

            return typeof(WanderState);
        }
    }
}
