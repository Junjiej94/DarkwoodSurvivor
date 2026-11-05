using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public int hp = 3;
    public float aggroRange = 8f;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color originalColor;
    private float lastAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist < aggroRange)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Interaction 1: Enemy damages player on contact
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && Time.time > lastAttackTime + 1f)
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(2);
                lastAttackTime = Time.time;
            }
        }
    }

    // Interaction 2: Enemy takes damage from player bullets, flashes, and dies
    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        if (sr != null)
        {
            sr.color = Color.white;
            Invoke(nameof(ResetColor), 0.1f);
        }

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    void ResetColor()
    {
        if (sr != null) sr.color = originalColor;
    }
}
