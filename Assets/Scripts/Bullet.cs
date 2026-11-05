using UnityEngine;

public class Bullet : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 3f); // Clean up after 3 seconds
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;

        // If it hits an enemy, damage it!
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(1);
            Destroy(gameObject);
        }
        else if (!other.isTrigger) // Hit a tree/wall
        {
            Destroy(gameObject);
        }
    }
}
