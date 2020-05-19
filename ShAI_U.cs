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
	[SerializeField] private Gun gun;
	public float health;
	public Faction factiton;
	[SerializeField] private LayerMask overlapMask;
	Vector3 lastDestination;
	public Vector3 currentDestination;
	bool standing;

	[Header("Debug")]
	[SerializeField] private List<ShAI_U> nearbyEnemies, nearbyFriends;
	[SerializeField] private List<CoverObjects> nearbyCovers;

	Transform enemyToAttack;
	CoverObjects coverToPersue;
	public bool attacking;
	bool persuingCover;
	bool inCover;
	// Use this for initialization
	void Start () {
		WanderAround ();
	}
	
	// Update is called once per frame
	void Update () {
		CheckHealth ();
		AnimateMovement ();
		LookOut ();
		if (agent.remainingDistance <= agent.stoppingDistance) {
			if (!attacking) {
				standing = true;
				inCover = false;
				persuingCover = false;
				StartCoroutine (StandStill ());
			} else {
				if (persuingCover) {
					inCover = true;
				}

			}

		}
	}

	void LookOut(){
		List<ShAI_U> friendly = new List<ShAI_U> ();
		List<ShAI_U> enemies = new List<ShAI_U> ();
		List<ShAI_U> neutrals = new List<ShAI_U> ();
		List<CoverObjects> covers = new List<CoverObjects> ();

		Collider[] cols = Physics.OverlapSphere (transform.position, 7, overlapMask);
		for (int i = 0; i < cols.Length; i++) {
			if (cols[i].gameObject.layer == 8){ // 8 == Soldier;
				ShAI_U ai = cols [i].GetComponent<ShAI_U> ();
				if (ai.factiton != factiton && ai.factiton != Faction.neutral) {
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
			anim.SetBool ("Aim", attacking);
			print ("No One LEft SiRe ! ! ! !");
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

		Attack (enemies [idealEnemyToAttack_Index]);

	}

	void Attack(ShAI_U enemy){
		attacking = true;
		anim.SetBool ("Aim", attacking);
		//anim.SetLayerWeight (1, 1);
		enemyToAttack = enemy.transform;

		gun.Shoot (enemyToAttack.position - transform.position);
		if (!inCover)
			FindIdealCover();

		Vector3 direction = enemyToAttack.position - transform.position;
		direction.y = 0f;
		transform.rotation = Quaternion.LookRotation (direction);
	}

	void FindIdealCover(){

		int coverIndex = -1;
		for (int i = 0; i < nearbyCovers.Count; i++) {
			float coverDistanceFromMyFaction = AverageDistanceFromSquad (nearbyFriends, nearbyCovers [i]);
			float coverDistanceFromEnemiesFaction = AverageDistanceFromSquad (nearbyEnemies, nearbyCovers [i]);
			if (coverDistanceFromMyFaction < coverDistanceFromEnemiesFaction) {
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
		//// FINDING IDEAL COVER SIDE ////



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

	public void RunForYourLife(){
		this.enabled = false;
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

	void CheckHealth(){
		if (health <= 0) {
			agent.enabled = false;
			this.GetComponent<Collider> ().enabled = false;
			anim.SetFloat ("WalkSpeed", 0);
			anim.SetBool ("Die", true);
			this.gameObject.layer = 0;
			this.enabled = false;

		}
	}

	public Vector3 RandomNavSphere(Vector3 origin, float dist) {
		NavMeshHit navHit;

		Vector3 randDirection = Random.insideUnitSphere * dist;
		randDirection += origin;
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
	public void ReloadToFalse(){
		anim.SetBool ("Reload", false);
	}
}
