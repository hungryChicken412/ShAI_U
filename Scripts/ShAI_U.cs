using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class ShAI_U : MonoBehaviour {
	[Header("Shooter AI For Unity")]

	[Header("Main")]
	[SerializeField] private Animator anim;
	[SerializeField] private NavMeshAgent agent;
	public Transform eyes;
	[SerializeField] private float agentRunSpeed;
	[SerializeField] private float agentWalkSpeed;
	[SerializeField] private bool patroller, goAt;// if this AI is supposed to patrol a set of points or go to specific locations one-by-one
	[SerializeField] private Vector3[] patrolPoints;
	[Tooltip("Put positions in order which you want the AI to reach them.")]
	[SerializeField] private Vector3[] goToPosition;
	[SerializeField] private Gun gun;
	public float health;
	public Faction factiton;
	public LayerMask overlapMask;
	Vector3 lastDestination;
	public Vector3 currentDestination;
	bool standing;

	[Header("IK System")]
	public LayerMask ikMask;
	public float distanceFromTheGround = 0.05f;


	[Header("Debug")]
	[SerializeField] private List<ShAI_U> nearbyEnemies;
	[SerializeField] private List<ShAI_U> nearbyFriends;
	[SerializeField] private List<CoverObjects> nearbyCovers;
	[SerializeField] private LayerMask viewMask;
	[SerializeField] private bool canNotSeeEnemy;

	Transform enemyToAttack;
	CoverObjects coverToPersue;
	public bool attacking;
	bool persuingCover;
	public bool inCover;
	int progress;


	Vector3 lastpos, curpos;
	float velocity;

	// Use this for initialization


	void Start () {

		if (patroller) {
			WanderAround ();
			goAt = false;
		}

		if (goAt) {
			progress = 0; // index of position this AI has reached;
			ProgressAtLevel (); // start progressing at your mission;
		}
	}
	
	// Update is called once per frame
	void Update () {
		LookOut ();
		if (agent.remainingDistance <= agent.stoppingDistance) {
			if (!attacking) {
				standing = true;
				inCover = false;
				persuingCover = false;
				StartCoroutine (StandStill ());
			} else {
				if (persuingCover || (coverToPersue != null && ApproxDistance(transform.position, coverToPersue.transform.position) < 10f)) {
					inCover = true;
				}

			}

		}
		AnimateMovement ();
		CheckHealth ();

	}



	void LookOut(){
		List<ShAI_U> friendly = new List<ShAI_U> ();
		List<ShAI_U> enemies = new List<ShAI_U> ();
		List<ShAI_U> neutrals = new List<ShAI_U> ();
		List<CoverObjects> covers = new List<CoverObjects> ();

		Collider[] cols = Physics.OverlapSphere (transform.position, 30, overlapMask);
		for (int i = 0; i < cols.Length; i++) {
			if (cols[i].gameObject.layer == 8){ // 8 == Soldier;
				ShAI_U ai = cols [i].GetComponent<ShAI_U> ();
				canNotSeeEnemy = Physics.Linecast (eyes.position, ai.eyes.position, viewMask);
				if (ai.factiton != factiton && ai.factiton != Faction.neutral && !canNotSeeEnemy) {
					enemies.Add (ai);
				} else if (ai.factiton == factiton){
					friendly.Add (ai);
				}// YOU CAN ALSO GATHER INFORMATION ABOUT NEARBY NEURALS, BUT AS FOR NOW, I DON'T NEED IT.
			} else { // if it's not a soldier, it's a cover
				covers.Add(cols[i].GetComponent<CoverObjects>()); // add that cover to the list;
			}
		}

		nearbyEnemies = enemies;
		nearbyCovers = covers;
		nearbyFriends = friendly;
		if (nearbyEnemies.Count > 0)
			FindIdealEnemyToAttack (nearbyEnemies);
		else {
			attacking = false;
		}


	}




	void FindIdealEnemyToAttack(List<ShAI_U> enemies){
		
		float closestDistance = 999;
		int idealEnemyToAttack_Index = -1;
		for (int i = 0; i < enemies.Count; i++) {
			float enemyDistance = ApproxDistance (enemies [i].transform.position, transform.position);

			if (enemyDistance < closestDistance) {
				closestDistance = enemyDistance;
				idealEnemyToAttack_Index = i;
			}
		}
		if (idealEnemyToAttack_Index != -1)
			Attack (enemies [idealEnemyToAttack_Index]);
		else
			attacking = false;

	}

	void Attack(ShAI_U enemy){
		attacking = true;

		enemyToAttack = enemy.transform;

		gun.Shoot (enemyToAttack.position - transform.position);
		if (!inCover)
			FindIdealCover();

		Vector3 direction = enemyToAttack.position - transform.position;
		direction.y = 0f;
		transform.rotation = Quaternion.Lerp(transform.rotation,Quaternion.LookRotation (direction), 0.5f);
		FightWithinCover ();
	}

	void FightWithinCover(){
		if (inCover) {
			anim.SetBool ("crouch", true);
			anim.SetLayerWeight (1, 1);

		} else {
			anim.SetBool ("crouch", false);
			anim.SetLayerWeight (1, 0);
			anim.SetLayerWeight (3, 0);
		}
	}

	void FindIdealCover(){

		int coverIndex = -1;
		for (int i = 0; i < nearbyCovers.Count; i++) {
			float coverDistanceFromMyFaction = AverageDistanceFromSquad (nearbyFriends, nearbyCovers [i]);
			float coverDistanceFromEnemiesFaction = AverageDistanceFromSquad (nearbyEnemies, nearbyCovers [i]);

			if (coverDistanceFromMyFaction < coverDistanceFromEnemiesFaction && coverDistanceFromEnemiesFaction > 4) {
				coverIndex = i;
				break;
			}
		}

		if (coverIndex != -1) {
			coverToPersue = nearbyCovers [coverIndex];
			lastDestination = currentDestination;
			currentDestination = coverToPersue.coverPosition.position;
			agent.SetDestination (currentDestination);
			persuingCover = true;
		}


	}



	float AverageDistanceFromSquad(List<ShAI_U> squad, CoverObjects cover){
		List<float> distances = new List<float> ();

		for (int i = 0; i < squad.Count; i++) {
			distances.Add (ApproxDistance (squad [i].transform.position, cover.transform.position));
		}
		float averageDistance = 0;
		for (int i = 0; i < distances.Count; i++) {
			averageDistance += distances [i];
		}
		averageDistance /= distances.Count;

		return averageDistance;

	}


	void AnimateMovement(){
		anim.SetBool ("shooting", attacking);

		lastpos = curpos;
		curpos = transform.position;
		velocity = (curpos - lastpos).magnitude / Time.deltaTime;
		if (attacking)
			agent.speed = agentWalkSpeed;
		else
			agent.speed = agentRunSpeed;
		

		float agentSpeed = agent.velocity.magnitude;
		float animationSpeed = velocity / agentRunSpeed;
		anim.SetFloat ("WalkSpeed", animationSpeed);
		if (agentSpeed > 0) {
			anim.SetBool ("crouch", false);
			anim.SetLayerWeight (1, 0);
		}
	}

	void WanderAround(){
		lastDestination = currentDestination;
		do {
			int x = Random.Range (0, patrolPoints.Length);
			currentDestination = patrolPoints [x];
		} while 
			(currentDestination == lastDestination);


		agent.SetDestination (currentDestination);

	}

	void ProgressAtLevel(){
		if (progress < goToPosition.Length) {
			lastDestination = currentDestination;
			currentDestination = goToPosition [progress];
			agent.SetDestination (currentDestination);
		}
	}

	public void RunForYourLife(){
		this.enabled = false; // CAN ADD SOME MORE FUNCTIONALITY TO THIS, BUT RIGHT NOW THIS WORKS.s
	}

	float ApproxDistance(Vector3 pos1, Vector3 pos2){

		float distX = pos2.x - pos1.x;
		distX *= distX;
		float distY = pos2.y - pos1.y;
		distY *= distY;
		float distZ = pos2.z - pos1.z;
		distZ *= distZ;
		float approxDistance = distX + distY + distZ;

		return approxDistance;
	
	}



	private void OnAnimatorIK(int layerindex){
		anim.SetIKPositionWeight (AvatarIKGoal.LeftFoot, 1);
		anim.SetIKPositionWeight (AvatarIKGoal.RightFoot, 1);
		anim.SetIKRotationWeight (AvatarIKGoal.LeftFoot, 1);
		anim.SetIKRotationWeight (AvatarIKGoal.RightFoot, 1);
		// left Foot;
		RaycastHit hit;

		if (Physics.Raycast (anim.GetIKPosition (AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down, out hit, 1f + distanceFromTheGround, ikMask)) {
			Debug.DrawLine (anim.GetIKPosition (AvatarIKGoal.LeftFoot), hit.point);
			Vector3 footPosition = hit.point;
			footPosition.y += distanceFromTheGround;
			anim.SetIKPosition (AvatarIKGoal.LeftFoot, footPosition);
			anim.SetIKRotation (AvatarIKGoal.LeftFoot, Quaternion.LookRotation (transform.forward,hit.normal));

		}
		if (Physics.Raycast (anim.GetIKPosition (AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down, out hit, 1f + distanceFromTheGround, ikMask)) {
			Debug.DrawLine (anim.GetIKPosition (AvatarIKGoal.RightFoot), hit.point);
			Vector3 footPosition = hit.point;
			footPosition.y += distanceFromTheGround;
			anim.SetIKPosition (AvatarIKGoal.RightFoot, footPosition);
			//// DOING THIS BECAUSE IN THE ANIMATION, WHEN THE CHRCTER IS STANDING, IT'S RIGHT LEG FACES THE RIGHT DIRECTION :P
			/// BUT WHEN RUNNING, IT IS FORWARD, DUH....!!..!!
			if (velocity > agentWalkSpeed)
				anim.SetIKRotation (AvatarIKGoal.RightFoot, Quaternion.LookRotation (transform.forward, hit.normal));
			else 
				
				anim.SetIKRotation (AvatarIKGoal.RightFoot, Quaternion.LookRotation (transform.right, hit.normal));
		}


	}

	void CheckHealth(){
		if (health <= 0) {
			this.GetComponent<Collider> ().enabled = false;
			anim.SetLayerWeight (1, 0);
			anim.SetLayerWeight (2, 0);

			anim.SetFloat ("WalkSpeed", 0);
			anim.SetBool ("Die", true);
			this.gameObject.layer = 0;
			Destroy (agent); // For some reason, It doesn't disable the agent, so I just remove the component.
			this.enabled = false;

		}
	}
		
	public List<Vector3> pointsAlreadyBeenTo = new List<Vector3>();
	IEnumerator StandStill(){
		agent.enabled = false;
		yield return new WaitForSeconds (1);
		agent.enabled = true;
		if (!attacking) {
			if (patroller)
				WanderAround ();
			if (goAt) {
				if (goToPosition.Contains (currentDestination) && !pointsAlreadyBeenTo.Contains (currentDestination) && ApproxDistance(transform.position, currentDestination) <=10f) {
					progress++;
					pointsAlreadyBeenTo.Add (currentDestination);
				}
				ProgressAtLevel ();
			}
				
		}

		standing = false;
	}

	public enum Faction{
		friendly,
		enemy,
		enemy2,
		neutral
	}

	public void ReloadToFalse(){
		gun.reloading = false;
		anim.SetBool ("Reload", gun.reloading);
		anim.SetBool ("crouch", false);
		anim.SetLayerWeight (1, 0);
		anim.SetLayerWeight (2, 0);
	}
}
