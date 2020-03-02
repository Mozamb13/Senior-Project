using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static AnimController;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{   
	#region VARIABLES
	[HideInInspector] public Animator anim;

	private Health health => GetComponent<Health>();

    //basic movement
    private CharacterController cc;
    
    [Header("Basic Movement")]
    public Camera maincam;

    public GameObject characterArt;
    public bool canMove = true, receiveInput = true, rolling;
	public float verticalVelocity = 0.0f, jogSpeed = 10.0f, runSpeed = 15.0f, walkSpeed = 5.0f, gravity = 30.0f, rollDist = 5f;
	private float speed;
	private Vector3 move = Vector3.zero;

	//variables for being grounded
	public LayerMask ground;

	//variables for slopes
	private Vector3 forward;

	[Header("Attack")]
	//variables for attacking
	public Base_Stats playerStats;

	private int tempStrength;

	public int strengthBonus = 10, powerLossSpeed = 5, powerBuildUpMax = 100;
	[HideInInspector] public int attackNum;
	private int currentPower;
	public float maxAttackDelay = 2; 
	private float lastTimeClicked;
	[HideInInspector] public bool attaking;
	
	//variables for coroutines
	public int powerWaitTime = 5;
	private WaitForSeconds longWait => new WaitForSeconds(powerWaitTime);
	private Coroutine loosePower, playGame, roll;

	//variables for squad orders
	[Header("Squad")]
	public LayerMask squad;
	public List<Squad> squadMembers;
	private OrderController order;

	//variables for attack ram
	public OrderController playerDefinedRam;
	public GameObject playerDefinedRamEnd;
	private bool ramPlaced;

	//variables for shielding
	[Header("Defense")] 
	//public GameObject explosion;
	private bool shielding;
	public Image powerSlider;

	//variables for countering
	[Header("Counter Attack")]
	public GameObject counterSymbol;
	private bool counterWindow;
	private GameObject attackingEnemy;
	public int counterDamage;

	//variables for healing squad
	[Header("Healing")]
	public float healDistance = 5f;
	public int healPower = 20;

	//variables for squad orders UI
	[Header("Order UI")]
	public UnityEvent ramOrderEvent;
	public UnityEvent killOrderEvent;
	public UnityEvent returnOrderEvent;

	#endregion

	private void OnEnable()
	{
		CounterAction += CounterActionHandler;
		StaticVars.DeathAction += DeathActionHandler;
	}

	private void OnDisable()
	{
		CounterAction -= CounterActionHandler;
		StaticVars.DeathAction -= DeathActionHandler;
		
		Reset();
	}

	private void Awake()
	{
		//locks the cursor to the center of the screen and turns it invisible
		Cursor.lockState = CursorLockMode.Locked;
	}

	private void Start()
	{
		tempStrength = playerStats.strength;
		anim = GetComponent<Animator>();
		cc = GetComponent<CharacterController>();
		speed = jogSpeed;
		playGame = StartCoroutine(PlayGame());
	}

	//restarts the PlayGame coroutine
	public void Restart()
	{
		anim.SetBool(StaticVars.dead, false);
		StopCoroutine(playGame);
		playGame = StartCoroutine(PlayGame());
	}
	
	//coroutine that controls the main game
	private IEnumerator PlayGame()
	{
		while(health.alive)
		{
			if (receiveInput)
			{
				//if the player can move, activates the move input
				if (canMove)
					MoveInput();
				else
				{
					anim.SetFloat(StaticVars.moveX, 0);
					anim.SetFloat(StaticVars.moveZ, 0);
				}

				AttackInput();
				DefendInput();
				OrderInput();

				//if an enemy is attacking, activates the counter input
				if (counterWindow && attackingEnemy != null &&
				    Vector3.Distance(transform.position, attackingEnemy.transform.position) <= 5)
					CounterInput();
				else
					CounterActionHandler(false, null);
			}
			yield return StaticVars.oneHundredth;
		}
		Die();
	}

	//coroutine that controls the speed at which power for power attack is lost
	private IEnumerator LoosePower()
	{
		//waits for a bit before starting to loose power
		yield return longWait;
		while (currentPower > 0)
		{
			//slowly removes the power level
			currentPower -= powerLossSpeed;
			UpdatePowerUI();
			yield return StaticVars.oneSec;
		}
		//once the power is gone return the player to normal attack strength
		playerStats.strength = tempStrength;
	}

	//move the character a specified distance for dodge rolling
	private IEnumerator Roll()
	{
		float moveX = Input.GetAxisRaw("Horizontal");
		float moveZ = Input.GetAxisRaw("Vertical");
		health.dodging = true;
		
		//if the player isn't inputing anything roll forward
		if (moveX == 0 && moveZ == 0)
		{
			moveZ = 1;
		}
		
		while (rolling)
		{
			//if the player is on the ground, roll
			if (IsGrounded())
			{
				verticalVelocity = 0;
				move = transform.TransformDirection(new Vector3(moveX,-Mathf.Abs(forward.y), moveZ)) * rollDist;
			}

			//Rotates the character to be facing the input direction
			float angle = Mathf.Atan2(moveX, moveZ) * Mathf.Rad2Deg;
			characterArt.transform.localRotation = Quaternion.Euler(0, angle, 0);
			
			//calculates movement
			verticalVelocity -= gravity * Time.deltaTime;
			Vector3 movement = move + verticalVelocity * Vector3.up;
			cc.Move(movement * Time.deltaTime);
			yield return StaticVars.oneHundredth;
		}

		//end the roll animation
		anim.SetInteger(StaticVars.jump, 0);

		//makes the character face forward again and enables move input
		characterArt.transform.localRotation = Quaternion.identity;
		canMove = true;
		health.dodging = false;
	}

	//input for giving orders
	private void OrderInput()
	{
		//draws the player raycast line so we can see it in scene view
		var camTransform = maincam.transform;
		Debug.DrawRay((camTransform.position + new Vector3(0,0.5f,0)), camTransform.forward * 100, Color.red);

		//sends out a ray to see if it is hitting anything
		if(Physics.Raycast(camTransform.position, camTransform.forward, out var hit, 100f, squad))
		{
			//checks if the hit is an order or an enemy or a squad member
			var found = (hit.collider.CompareTag("order") || hit.collider.CompareTag("Enemy"));
			var downedSquad = (hit.collider.CompareTag("squad"));

			//sends a squad-mate to perform an order on player command
			if(Input.GetKeyDown(KeyCode.F) && found)
			{
				//checks that the order is valid
				order = hit.collider.gameObject.GetComponent<OrderController>();
				var isOrderNotNull = order != null;

				//finds the squadmember that can do the order (the forloop is a remnant of when there was more than one squad member)
				foreach (var member in squadMembers)
				{
					//checks that the current memeber can do the order
					if(isOrderNotNull && member.unitType == order.unitType)
					{
						//if that squad member is already doing the order, order them to stop doing it
						if(order.inProgress)
						{
							member.recalled = true;
							order.inProgress = false;
						}
						//if that squad member is not doing the order, send them to do it
						else
						{
							member.recalled = false;
							member.givenOrder = true;
							member.currentOrder = order.gameObject;
							order.inProgress = true;
							ramOrderEvent.Invoke();
						}
					}
					//if the order is an enemy, send the squand member to attack
					else if (hit.collider.CompareTag("Enemy"))
					{
						member.givenOrder = true;
						member.currentOrder = hit.collider.gameObject;
						killOrderEvent.Invoke();
					}
				}
			}
			//heals a downed squad member if the player is close enough
			else if (Input.GetKeyDown(KeyCode.F) && downedSquad)
			{
				//bug check to make sure the squad member has health componenet
				var squadHealth = hit.collider.gameObject.GetComponent<Health>();
				if(health == null)
				{
					Debug.Log("Squad member doesn't have health assigned!");
				}
				//if they are dead, heal the downed person
				else if(!squadHealth.alive && Vector3.Distance(transform.position, squadHealth.transform.position) <= healDistance)
				{
					//play healing animation
					anim.SetBool(StaticVars.heal, true);
					squadHealth.Heal(healPower);
					squadHealth.alive = true;
				}
			}
		}

		//recalls the squad to the player
		if(Input.GetKeyDown(KeyCode.R))
		{
			//calls each member of the squad, cancels their orders, and sets them to follow the player
			foreach (var member in squadMembers)
			{
				member.recalled = true;
				member.givenOrder = false;
				if (member.currentOrder != null)
					member.currentOrder.GetComponent<OrderController>().inProgress = false;
				member.currentOrder = null;
			}
			returnOrderEvent.Invoke();
		}
		
		//sets the ram start and end location
		if(Input.GetKeyDown(KeyCode.Q))
		{
			//checks if we have placed the start ram location, if not, set it
			if(Physics.Raycast(maincam.transform.position, maincam.transform.forward, out hit, 100f, ground) && !ramPlaced)
			{
				//insert custom vfx ram point whatever (start)
				playerDefinedRam.gameObject.SetActive(true);
				playerDefinedRam.transform.position = hit.point;
				ramPlaced = true;
			}
			//checks if the end location hasn't been placed and the start location has. if so, place the end location
			else if(Physics.Raycast(maincam.transform.position, maincam.transform.forward, out hit, 100f, ground) && ramPlaced)
			{
				//insert custom vfx ram point whatever (end)
				playerDefinedRamEnd.gameObject.SetActive(true);
				playerDefinedRamEnd.transform.position = hit.point;
				playerDefinedRam.inProgress = true;

				//finds the squad member who can ram and sends them to ram from start location to end location
				foreach (var member in squadMembers.Where(member => playerDefinedRam != null && member.unitType == playerDefinedRam.unitType))
				{
					member.recalled = false;
					member.givenOrder = true;
					member.currentOrder = playerDefinedRam.gameObject;
					playerDefinedRam.inProgress = true;
					ramPlaced = false;
					ramOrderEvent.Invoke();
				}
			}
		}
	}

	//input for movement
	private void MoveInput()
    {
		anim.SetBool(StaticVars.grounded, IsGrounded());
		//checks if player is on ground
		if(IsGrounded())
		{
			verticalVelocity = 0;
			
			//makes the player start the roll on player input
			if(Input.GetButtonDown("Jump"))
			{
				anim.SetInteger(StaticVars.jump, 2);
				rolling = true;
				return;
			}

			//sets the speed to run
			if(Input.GetKeyDown(KeyCode.LeftShift))
			{
				speed = runSpeed;
			}
			//sets the speed to jog
			else if (Input.GetKeyUp(KeyCode.LeftShift))
			{
				speed = jogSpeed;
			}
			//sets the speed to walk
			else if(Input.GetKeyDown(KeyCode.LeftControl))
			{
				anim.SetBool(StaticVars.walk, true);
				speed = walkSpeed;
			}
			//sets the speed to jog
			else if (Input.GetKeyUp(KeyCode.LeftControl))
			{
				anim.SetBool(StaticVars.walk, false);
				speed = jogSpeed;
			}

			//this makes the character controller move based off the local rotation and not global
			float moveX = Input.GetAxis("Horizontal");
			float moveZ = Input.GetAxis("Vertical");
			move = transform.TransformDirection(new Vector3(moveX, -Mathf.Abs(forward.y), moveZ)) * speed;
			anim.SetFloat(StaticVars.moveX, moveX);
			anim.SetFloat(StaticVars.moveZ, moveZ);
		}
		
		//Rotates the character to follow the camera
		var eulerAngles = transform.eulerAngles;
		var angles = new Vector3(eulerAngles.x, maincam.transform.eulerAngles.y, eulerAngles.z);
		transform.rotation = Quaternion.Euler(angles);
		
		//calculates movement
		verticalVelocity -= gravity * Time.deltaTime;
		anim.SetFloat(StaticVars.moveY, verticalVelocity);
		Vector3 movement = move + verticalVelocity * Vector3.up;
		cc.Move(movement * Time.deltaTime);
	}

	//Input for attacking
	private void AttackInput()
	{
		//if the player isn't on the ground they can't attack
		if (!IsGrounded())
			return;
		
		//used to make the combo attack animations work, if the delay runs out goes back to the first attack in the combo
		if(Time.time - lastTimeClicked > maxAttackDelay)
		{
			attackNum = 0;
			anim.SetInteger(StaticVars.mouse0, attackNum);
		}
		//starts the attack
		if (Input.GetMouseButtonDown(0) && !rolling)
		{
			health.shielded = true;

			attaking = true;
			//attack num is used by the animator to determine which animation should be used
			attackNum++;
			attackNum = Mathf.Clamp(attackNum, 0, 2);
			
			canMove = false;
			//if it's the first attack, let the animator know to use the first animation
			if(attackNum == 1)
				anim.SetInteger(StaticVars.mouse0, attackNum);
		}
		//starts the delay that resets attack num
		else if (Input.GetMouseButtonUp(0))
		{
			lastTimeClicked = Time.time;
		}
//		//unleashes the power attack
//		if (Input.GetKeyDown(KeyCode.G))
//			ReleasePower();
	}

	//input for defending
	private void DefendInput()
	{
		//if player is not rolling, pull up shield
		if(Input.GetMouseButtonDown(1) && !rolling)
		{
			anim.SetFloat(StaticVars.mouse1, 1);
		}
		//put down shield
		else if(Input.GetMouseButtonUp(1))
		{
			anim.SetFloat(StaticVars.mouse1, 0);
			ToggleShield(false);
		}
	}

	//input for counter attack
	private void CounterInput()
	{
		//prevents glitches
		if (!Input.GetKeyDown(KeyCode.E) || attackingEnemy == null) return;
		print("COUNTER ATTACK");
		//starts the paired animation for counter attacks
		StaticVars.PairCounterAction(this, attackingEnemy.GetComponent<Enemy>());
	}

	//activates the player shield for defense
	public void ToggleShield(bool _state)
	{
		shielding = _state;
		health.shielded = _state;
		
		//prevents the player from countering while their shield is up
		if (_state)
		{
			counterSymbol.SetActive(false);
			counterWindow = false;
		}
	}
	

	//builds the current power loaded
	public void BuildPower(int _amount)
	{
		if (currentPower < powerBuildUpMax)
		{
			currentPower += _amount;
			//causes player strength to constantly increase
			playerStats.strength = tempStrength + strengthBonus;
		}
		else
		{
			currentPower = powerBuildUpMax;
		}
		UpdatePowerUI();
		
		//when the player has power it gets lost if they are not currently building power
		//this restarts that check every time they get more power
		if(loosePower != null)
			StopCoroutine(loosePower);
		loosePower = StartCoroutine(LoosePower());
	}

//	//releases pent up power when it is full
//	private void ReleasePower()
//	{
//		//the power attack can only be used when at max power
//		if (currentPower < powerBuildUpMax) return;
//
//		//returns player strength to normal and unleashes the EXPLOSION
//		playerStats.strength = tempStrength;
//		currentPower = 0;
//		explosion.transform.localScale = new Vector3(20f, 20f, 20f);
//		UpdatePowerUI();
//	}

	//checks if the player is on the ground
	//parameters: none
	//returns: true if isGrounded, false if not
	//there was a glitch where going down/uphill made the character see itself as not grounded when using cc.isGrounded
	private bool IsGrounded()
	{
		if(cc.isGrounded)
			return true;
		
		var bottom = cc.transform.position - new Vector3(0, cc.height / 2, 0);

		//checks via raycast to see if the player is close enough to the ground to count as being grounded
		//also changes the forward vector so player doesn't bounce down slopes
		if(Physics.Raycast(bottom, -Vector3.up, out var hit, 0.2f, ground))
		{
			//calculates the forward movement direction
			forward = Vector3.Cross(transform.right, hit.normal);
			//calculates the side to side movement direction
			var strafeDir = Vector3.Cross(transform.forward, hit.normal);

			//checks to see which direction, forward or strafe, is more correct to prevent slope bouncing
			if(Mathf.Abs(strafeDir.y) > Mathf.Abs(forward.y))
			{
				forward = strafeDir;
			}
			return true;
		}
		return false;
	}

	//calculates how much power the player has after attacking
	//called from Animator Behaviour
	public void CalcAttackPower()
	{
		//reduces the players power level when attacking
		if(currentPower > 0)
			currentPower -= strengthBonus;
		if (currentPower <= 0)
		{
			currentPower = 0;
		}
		UpdatePowerUI();
	}
	
	//updates the power level UI element
	private void UpdatePowerUI()
	{
		if(powerSlider != null)
			powerSlider.fillAmount = (float)currentPower / 100;
		else
			Debug.Log("ERROR: No power slider");
	}

	//summons a squad member to come revive the player
	//this should be changed so the player can choose to have them come or not
	private void Die()
	{
		//set all the animations to zero except death
		anim.SetInteger(StaticVars.mouse0, 0);
		anim.SetFloat(StaticVars.mouse1, 0);
		anim.SetFloat(StaticVars.moveX, 0);
		anim.SetFloat(StaticVars.moveZ, 0);
		anim.SetBool(StaticVars.dead, true);

		StaticVars.DeathAction(gameObject);
		squadMembers[0].currentOrder = gameObject;
		squadMembers[0].givenOrder = true;
		squadMembers[0].healTargetHealth = health;
	}
	
	//called by roll control animator behaviour to make roll movement line up with the animation
	public void StartRoll()
	{
		canMove = false;
		if(roll != null)
			StopCoroutine(roll);
		roll = StartCoroutine(Roll());
	}

	//resets the players strength to normal
	private void Reset()
	{
		playerStats.strength = tempStrength;
	}

	//handles the action that is called when an enemy is open for a counter attack
	private void CounterActionHandler(bool _state, GameObject _enemy)
	{
		attackingEnemy = _enemy;

		//makes sure the player is able to counter
		//if they are shielding or the enemy is too far away they cannot counter
		if (shielding || attackingEnemy == null || Vector3.Distance(transform.position, attackingEnemy.transform.position) > 5)
		{
			counterWindow = false;
			counterSymbol.SetActive(false);
			return;
		}
		//lets the user know that countering is possible
		counterWindow = _state;
		counterSymbol.SetActive(_state);
	}

	//handles the action that is called when an AI dies
	private void DeathActionHandler(GameObject _other)
	{
		//if the dead enemy was attacking the player, remove them from the notice so the attack queue in EnemyManager updates
		if (attackingEnemy == _other)
			attackingEnemy = null;
	}
}