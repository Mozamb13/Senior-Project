using UnityEngine;
using System;

public class OrderState : BaseState
{
    private Squad squad;
    private bool endRam;
    
    public OrderState(Squad _ai) : base(_ai.gameObject, _ai)
    {
        //makes sure the ai is saved as a squad member
        squad = _ai;
    }

    public override Type Tick()
    {
        //if ai is damaged go to that state
        if (squad.damaged)
        {
            ai.anim.SetBool(StaticVars.walk, false);
            return typeof(DamagedState);
        }
        //if the squad mate doesn't have anything to do or has been recalled, follow the player
        else if(!squad.givenOrder || squad.currentOrder == null || squad.recalled)
        {
            squad.givenOrder = false;
            squad.currentOrder = null;
            return typeof(FollowState);
        }
        //if the squad's currentOrder is an enemy, chase the enemy
        else if(squad.currentOrder.name == squad.enemyTags[0])
        {
            squad.enemyFound = true;
            squad.currentTarget = squad.currentOrder;
            squad.currentOrder = null;
            squad.givenOrder = false;
            return typeof(ChaseState);
        }
        //if the current order is the player, go to and heal the player
        else if (squad.currentOrder.name == "Player")
        {
            squad.SetStoppingDist(5);
            squad.SetDestination(squad.currentOrder.transform.position);
            //checks that the squad member is near to the player
            if (Vector3.Distance(gameObject.transform.position, squad.currentOrder.transform.position) <= 5f)
            {
                squad.anim.SetBool(StaticVars.heal, true);
                squad.currentOrder = null;
                squad.givenOrder = false;
            }

            return typeof(OrderState);
        }
        //if the order is ram, ram
        else if (squad.currentOrder.name.Contains("Ram"))
        {
            //if the ram isn't over, move the the position of the order
            if(!endRam)
                squad.SetDestination(squad.currentOrder.transform.position);
            squad.SetStoppingDist(0);
            //if the memeber is at the order
            if(Vector3.Distance(squad.transform.position, squad.ai.destination) <= squad.ramOffset)
            {
                //start ram animation
                endRam = true;
                squad.anim.SetBool(StaticVars.walk, false);
                squad.anim.SetBool(StaticVars.ram, true);
                OrderController order = squad.currentOrder.GetComponent<OrderController>();

                //find the end location of the ram, run to it
                if(order.endLocation != null && Vector3.Distance(order.endLocation.position, squad.ai.destination) > squad.ramOffset)
                {
                    squad.SetDestination(order.endLocation.position);
                    squad.SetSpeed(10);
                    squad.ramHurtBox.SetActive(true);
                    return typeof(OrderState);
                }
                //if the ram doesn't have an end location end the ram
                else if(order.endLocation == null)
                {
                    Debug.Log("Ram didn't have end location");
                    return typeof(IdleState);
                }
                //if the squad has reaced the end of the ram go idle and reset order data
                else if (Vector3.Distance(squad.transform.position, squad.ai.destination) <= squad.ramOffset)
                {
                    squad.anim.SetBool(StaticVars.ram, false);
                    order.inProgress = false;
                    squad.currentOrder = null;
                    squad.givenOrder = false;
                    order.endLocation.gameObject.SetActive(false);
                    order.gameObject.SetActive(false);
                    squad.ramHurtBox.SetActive(false);
                    endRam = false;
                    squad.SetSpeed(3.5f);
                    return typeof(IdleState);
                }
                else
                {
                    Debug.Log("UNKNOWN ERROR: squad not heading to ram");
                    return typeof(OrderState);
                }
            }
            //if the member isn't at the order, make sure the walk animation is playing
            else
            {
                ai.anim.SetBool(StaticVars.walk, true);
                return typeof(OrderState);
            }
        }
        //if the squad member isn't at the order go to it
        else if (squad.transform.position != squad.currentOrder.transform.position)
        {
            ai.anim.SetBool(StaticVars.walk, true);
            squad.SetDestination(squad.currentOrder.transform.position);
            squad.ai.stoppingDistance = 0;
            return typeof(OrderState);
        }
        //if the squad is at the order
        else if (squad.transform.position == squad.currentOrder.transform.position)
        {
            return typeof(OrderState);
        }
        else
            return null;
    }
}
