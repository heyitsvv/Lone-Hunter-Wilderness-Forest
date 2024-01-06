using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BasicAI : MonoBehaviour
{
    public Transform player; // Player's transform
    public NavMeshAgent agent;
    private Animator anim;

    [Header("Health Settings")]
    public AIHealth aiHealth; // Reference to AIHealth script
    public bool dead;

    [Header("Attack Settings")]
    public float damage = 5f;
    public float attackCooldownTime = 10.0f; // Time between attacks

    [Header("Movement")]
    public float wanderWaitTime = 10f;
    public float walkSpeed = 2f;
    public float runSpeed = 3.5f;
    public float stoppingDistance = 8.0f; // Adjust this stopping distance

    [Header("Drop Settings")]
    public GameObject gatherableItemPrefab;

    private bool isAttacking;
    private bool isWandering = true;
    private Vector3 currentDestination;

    private float currentWanderTime;

    private SphereCollider detectionCollider; // SphereCollider for detecting the player
    private float maxChaseDistance; // Maximum chase distance based on collider radius

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        detectionCollider = GetComponent<SphereCollider>(); // Get the SphereCollider component
        aiHealth = GetComponent<AIHealth>(); // Get the AIHealth component

        // Use the collider's radius to determine the maximum chase distance
        maxChaseDistance = detectionCollider.radius;

        currentWanderTime = wanderWaitTime;

        // Set the initial destination within the wander area
        SetRandomDestinationInSphere();
    }

    private void Update()
    {
        // Update the dead boolean based on AIHealth script
        dead = aiHealth.IsDead();

        if (dead)
        {
            Die();
            return; // Exit the method if the animal is dead
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (isWandering)
        {
            Wander(distanceToPlayer);
        }
        else
        {
            ChaseAndAttack(distanceToPlayer);
        }
    }

    private void Wander(float distanceToPlayer)
    {
        UpdateAnimations();

        // Check if the AI has reached its destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // The AI has reached its destination, so set a new random destination
            SetRandomDestinationInSphere();
        }
    }

    private void ChaseAndAttack(float distanceToPlayer)
    {
        if (player.GetComponent<PlayerHealth>().currentHealth <= 0)
        {
            // Player's health is zero or below, don't chase or attack
            isWandering = true;
            agent.speed = walkSpeed;
            anim.SetBool("Run", false);
            anim.SetBool("Walk", true);
            return; // Exit the method, no further actions needed
        }

        if (distanceToPlayer <= maxChaseDistance)
        {
            // Chase the player
            agent.SetDestination(player.position);
            agent.speed = runSpeed;
            anim.SetBool("Walk", false);
            anim.SetBool("Run", true);

            if (distanceToPlayer <= agent.stoppingDistance)
            {
                // The player is within attack range, so transition to the attack animation
                anim.SetTrigger("Attack");
            }
        }
        else
        {
            // Stop chasing and go back to wandering
            isWandering = true;
            agent.speed = walkSpeed;
            anim.SetBool("Run", false);
            anim.SetBool("Walk", true);
        }
    }

    public void UpdateAnimations()
    {
        anim.SetBool("Walk", isWandering);
        anim.SetBool("Run", !isWandering);
    }

    public void AttackPlayer()
    {
        // Damage the player
        player.GetComponent<PlayerHealth>().TakeDamage((int)damage);
        // You can add more attack-related logic here
    }

    public void StartAttack()
    {
        if (!isAttacking)
        {
            // Implement the attack cooldown timer
            isAttacking = true;
            StartCoroutine(ResetAttackCooldown());
        }
    }

    private IEnumerator ResetAttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldownTime);
        isAttacking = false;
    }

    private void Die()
    {
        DropGatherableItem();
        // You can add more death-related logic here
        Destroy(gameObject); // Destroy the animal GameObject
    }

    private void DropGatherableItem()
    {
     if (gatherableItemPrefab != null && aiHealth != null)
     {
        // Get the position where the animal has died
        Vector3 deathPosition = transform.position;

        // Add a small offset in the y-axis (3 cm up)
        deathPosition.y += 0.03f; // 0.03 meters is 3 cm
        GetComponent<Collider>().enabled = true;

        // Instantiate the gatherable item at the modified position
        GameObject droppedItem = Instantiate(gatherableItemPrefab, deathPosition, Quaternion.identity);

        // Ensure the cloned item has the required components and tag
        GatherableItem gatherableItem = droppedItem.GetComponent<GatherableItem>();
        Collider collider = droppedItem.GetComponent<Collider>();

        if (gatherableItem == null)
        {
            Debug.LogError("GatherableItem script not found on the cloned item.");
            Destroy(droppedItem); // Destroy the cloned item if it doesn't have the script
            return;
        }

        if (collider == null)
        {
            Debug.LogError("Collider not found on the cloned item. Adding a BoxCollider by default.");
            collider = droppedItem.AddComponent<BoxCollider>(); // Add a BoxCollider if not found
        }

        collider.enabled = true; // Enable the collider

        gatherableItem.DropToGround(deathPosition);
      }
      else
      {
        Debug.LogError("Gatherable item prefab or AIHealth reference not found.");
    }
}


    private void SetRandomDestinationInSphere()
    {
        // Generate a random point within the specified sphere collider's bounds
        Vector3 randomPointInSphere = RandomPointInSphere(detectionCollider.transform.position, detectionCollider.radius);

        // Ensure the point is within the sphere collider
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPointInSphere, out hit, detectionCollider.radius, NavMesh.AllAreas))
        {
            currentDestination = hit.position;

            agent.autoTraverseOffMeshLink = true;

            agent.SetDestination(currentDestination);

            float distanceToDestination = Vector3.Distance(transform.position, currentDestination);
            if (distanceToDestination > 5.0f)
            {
                agent.speed = runSpeed;
                isWandering = false;
            }
            else
            {
                agent.speed = walkSpeed;
                isWandering = true;
            }
        }
    }

    private Vector3 RandomPointInSphere(Vector3 center, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;
        return randomDirection;
    }
}
