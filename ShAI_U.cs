using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ShAI_U : MonoBehaviour {
	[Header("Shooter AI For Unity")]

	[Header("Main")]
	[SerializeField] private Animator anim;
	[SerializeField] private NavMeshAgent agent;
	[SerializeField] private bool aware;
	[SerializeField] private Vector3[] patrolPoints;
	public Faction factiton;
	[SerializeField] private LayerMask overlapMask;
	Vector3 lastDestination, currentDestination;
	bool standing;
	[Header("Debug")]
	[SerializeField] List<ShAI_U> nearbyEnemies;
	Transform enemyToAttack;
	public bool attacking;
	// Use this for initialization
	void Start () {
		WanderAround ();
	}
	
	// Update is called once per frame
	void Update () {
		AnimateMovement ();
		LookOut ();
		if (agent.remainingDistance <= agent.stoppingDistance) {
			if (!attacking) {
				standing = true;
				StartCoroutine (StandStill ());
			} else {
				lastDestination = currentDestination;
				currentDestination = RandomNavSphere (transform.position, 7);
				agent.SetDestination (currentDestination);
			}

		}
	}

	void LookOut(){
		List<ShAI_U> friendly = new List<ShAI_U> ();
		List<ShAI_U> enemies = new List<ShAI_U> ();
		List<ShAI_U> neutrals = new List<ShAI_U> ();

		Collider[] cols = Physics.OverlapSphere (transform.position, 7, overlapMask);
		for (int i = 0; i < cols.Length; i++) {
			ShAI_U ai = cols [i].GetComponent<ShAI_U> ();
			if (ai.factiton != factiton && ai.factiton != Faction.neutral) {
				enemies.Add (ai);
			} // YOU CAN ALSO GATHER INFORMATION ABOUT NEARBY FRIENDS OR NEURALS, BUT AS FOR NOW, I DON'T NEED IT.
		}
		nearbyEnemies = enemies;
		FindIdealEnemyToAttack (nearbyEnemies);

	}

	void FindIdealEnemyToAttack(List<ShAI_U> enemies){
		
		float closestDistance = 999;
		int idealEnemyToAttack_Index = -1;
		for (int i = 0; i < enemies.Count; i++) {
			float enemyDistance = ApproxDistance (enemies [i].transform.position);
			if (enemyDistance < closestDistance) {
				closestDistance = enemyDistance;
				idealEnemyToAttack_Index = i;
			}
		}
		if (enemies.Count > 0)
			Attack (enemies [idealEnemyToAttack_Index]);
		else
			attacking = false;
	}

	void Attack(ShAI_U enemy){
		attacking = true;
		anim.SetBool ("Aim", attacking);
		anim.SetLayerWeight (1, 1);

		agent.enabled = true;
		agent.Stop ();
		agent.ResetPath ();
		lastDestination = currentDestination;
		currentDestination = RandomNavSphere (transform.position, 10);
		agent.SetDestination (currentDestination);

		enemyToAttack = enemy.transform;
		Vector3 direction = enemyToAttack.position - transform.position;
		direction.y = 0f;
		transform.rotation = Quaternion.LookRotation (direction);
	}


	private void OnAnimatorIK(int layerIndex)
	{
		if (attacking) {
			anim.SetLookAtWeight (1, 1, 1, 1, 1);
			anim.SetLookAtPosition (enemyToAttack.position);
		}

	}

	void AnimateMovement(){
		float agentSpeed = agent.velocity.magnitude;
		anim.SetFloat ("WalkSpeed", agentSpeed*0.5f);
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

	float ApproxDistance(Vector3 pos){
		Vector3 thisPos = transform.position;

		float distX = thisPos.x - pos.x;
		distX *= distX;
		float distY = thisPos.y - pos.y;
		distY *= distY;
		float distZ = thisPos.z - pos.z;
		distZ *= distZ;
		float approxDistance = distX + distY + distZ;

		return approxDistance;
	
	}

	public static Vector3 RandomNavSphere(Vector3 origin, float dist) {
		Vector3 randDirection = Random.insideUnitSphere * dist;

		randDirection += origin;

		NavMeshHit navHit;

		NavMesh.SamplePosition (randDirection, out navHit, dist, -1);

		return navHit.position;
	}

	IEnumerator StandStill(){
		agent.enabled = false;
		yield return new WaitForSeconds (1);
		agent.enabled = true;
		if (!attacking)
			WanderAround ();
		standing = false;
	}

	public enum Faction{
		friendly,
		enemy,
		enemy2,
		neutral
	}
}
