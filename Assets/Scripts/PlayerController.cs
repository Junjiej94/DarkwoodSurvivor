using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public int hp = 20;
    public GameObject bulletPrefab;
    public float bulletSpeed = 12f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 1. WASD Movement (Works out-of-the-box in Unity 6)
        moveInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1;
            moveInput = moveInput.normalized;
        }

        // 2. Left Click to Shoot towards Mouse (Interaction!)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && bulletPrefab != null)
        {
            Shoot();
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    void Shoot()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
        mouseWorldPos.z = 0f;

        Vector2 shootDir = ((Vector2)(mouseWorldPos - transform.position)).normalized;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = shootDir * bulletSpeed;
        }
    }

    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        Debug.Log("Player Hit! HP remaining: " + hp);
        // Flash yellow/white briefly to visually show interaction
        if (sr != null) Invoke(nameof(ResetColor), 0.15f);
        if (sr != null) sr.color = Color.yellow;
    }

    void ResetColor()
    {
        if (sr != null) sr.color = Color.blue; // Or Color.white if using a sprite image!
    }

    // Simple instant top-left HP readout for the demo so you don't even have to build a Canvas!
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 28;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(20, 20, 300, 50), "Player HP: " + hp, style);
    }
}
