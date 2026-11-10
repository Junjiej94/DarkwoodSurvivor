using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class ForestGame : MonoBehaviour
{
    const int Rows = 400, Columns = 600;
    static readonly KeyCode[] NumberKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };
    static readonly string[] ActiveNames = { "Ward", "Fireball", "Heal" };
    static readonly string[] PassiveNames = { "Magnet", "Vitality", "Quick Shield" };

    [Serializable] public class Weapon
    {
        public string name;
        [Min(1)] public int damage;
        [Min(0.1f)] public float range, bulletRange, attacksPerSecond;
        public Sprite sprite;
        public Weapon(string name, int damage, float range, float bulletRange, float speed)
        {
            this.name = name;
            this.damage = damage;
            this.range = range;
            this.bulletRange = bulletRange;
            attacksPerSecond = speed;
        }
    }

    [Serializable] public class Spell
    {
        [Min(0)] public int mana;
        [Min(0)] public float cooldown;
        public Spell(int mana, float cooldown) { this.mana = mana; this.cooldown = cooldown; }
    }

    [Header("World")]
    public TextAsset forestMap;
    public TextAsset lootTable;
    [Tooltip("Grass, tree, stump, chest, enemy spawner, miniboss, boss. Boss weight selects its location, never its count.")]
    public float[] tileWeights = { 80, 15, 4, .05f, .15f, .003f, 1 };
    public Sprite background, grass, tree, stump, playerArt, chestArt, bossArt, miniArt, meleeArt, rangedArt;
    public Sprite goldArt, heartArt, xpArt, manaArt, staminaArt, bookArt;
    public Sprite[] activeArt = new Sprite[3], passiveArt = new Sprite[3];
    [Header("Weapons: sword, staff, bow, pistol, then optional loot weapons")]
    public Weapon[] weapons = {
        new Weapon("Sword", 6, 1.6f, 1.6f, 2), new Weapon("Staff", 5, 10, 12, 1.5f),
        new Weapon("Bow", 4, 14, 16, 2), new Weapon("Pistol", 3, 9, 12, 3)
    };
    [Header("Spells: ward, fireball, heal")]
    public Spell[] spells = { new Spell(15, 12), new Spell(8, 3), new Spell(10, 8) };
    [Min(.1f)] public float wardSeconds = 4, wardRadius = 2.5f, fireballRange = 18;
    [Min(1)] public int fireballDamage = 12;
    [Header("Timing")]
    [Min(.05f)] public float shieldDelay = 3, quickShieldDelay = 2, shieldTick = .5f;
    [Min(.05f)] public float chopSeconds = 1.5f, walkSpeed = 5, staminaSeconds = 20;
    [Min(0)] public float staminaSpeedBonus = 2;
    public float[] chestSeconds = { 1, 2.5f, 4 };
    [Header("Enemy behavior")]
    [Min(1)] public float aggro = 10, dropAggro = 16;

    readonly int[] slots = new int[3];
    readonly float[] spellReady = new float[3];
    int hp, maxHp, shield, maxShield, mana, gold, level, selected, active, passive, room, tooltip = -1;
    double xp;
    bool choosing, paused, dead, won;
    float time, nextShot, nextShield, wardEnd, staminaEnd, progress, chopEnd;
    Vector2 position, wardCenter;
    Vector2Int chopping;
    SpriteRenderer hero, ward, backdrop;
    Sprite solid, ringSprite;
    Camera view;
    Chest opening;
    Transform visuals;
    double Needed => Math.Round(10 * Math.Exp(level), MidpointRounding.AwayFromZero) * 5;

    void Awake()
    {
        view = Camera.main;
        if (!view) view = new GameObject("Forest Camera").AddComponent<Camera>();
        view.orthographic = true;
        view.orthographicSize = 8;
        view.backgroundColor = new Color(.09f, .16f, .12f);
        solid = MakeSprite(false);
        ringSprite = MakeSprite(true);
        ReadLoot();
        Restart();
    }

    Sprite MakeSprite(bool ring)
    {
        int size = ring ? 96 : 1;
        var texture = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), Vector2.one * (size - 1) / 2);
            texture.SetPixel(x, y, !ring || (distance > 43 && distance < 47) ? Color.white : Color.clear);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one / 2, size);
    }

    SpriteRenderer DrawSprite(string label, Sprite art, Color tint, Vector2 point, float size, int order)
    {
        var item = new GameObject(label);
        item.transform.SetParent(visuals, false);
        var renderer = item.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = order;
        Paint(renderer, art, tint, point, size);
        return renderer;
    }

    void Paint(SpriteRenderer renderer, Sprite art, Color tint, Vector2 point, float size)
    {
        renderer.sprite = art ? art : solid;
        renderer.color = art ? Color.white : tint;
        Vector3 bounds = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(size / bounds.x, size / bounds.y, 1);
        renderer.transform.position = point;
    }

    void Restart()
    {
        room = 1;
        hp = maxHp = 20;
        shield = maxShield = 10;
        mana = 50;
        gold = 0;
        xp = 0;
        level = 1;
        selected = 0;
        active = passive = tooltip = -1;
        for (int i = 0; i < 3; i++) slots[i] = -1;
        Array.Clear(spellReady, 0, spellReady.Length);
        time = nextShot = nextShield = staminaEnd = 0;
        paused = dead = won = false;
        choosing = true;
        BuildRoom();
    }

    void Update()
    {
        if (dead || won)
        {
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) paused = !paused;
        if (paused) return;
        for (int i = 0; i < 3; i++)
        {
            if (!Input.GetKeyDown(NumberKeys[i])) continue;
            if (choosing) { slots[0] = i; choosing = false; return; }
            if (slots[i] >= 0) selected = i;
        }
        if (choosing) return;

        float dt = Mathf.Min(Time.deltaTime, .05f);
        time += dt;
        if (Input.GetKeyDown(KeyCode.Tab)) tooltip = tooltip == 4 ? -1 : tooltip + 1;
        Vector2 move = new Vector2(Axis(KeyCode.A, KeyCode.D), Axis(KeyCode.S, KeyCode.W)).normalized;
        bool moving = move != Vector2.zero;
        position = Move(position, move * (walkSpeed + (time < staminaEnd ? staminaSpeedBonus : 0)) * dt, .32f);
        if (moving) ResetShield();
        TickEnemies(dt);
        TickShots(dt);
        if (dead) return;
        Interact(moving, dt);
        if (Input.GetKey(KeyCode.Space) && time >= nextShot) FireWeapon();
        if (Input.GetKeyDown(KeyCode.E)) Cast();
        CollectLoot();
        if (time >= nextShield && shield < maxShield)
        {
            shield++;
            nextShield = time + shieldTick;
        }
        RefreshView();
    }

    static int Axis(KeyCode negative, KeyCode positive) =>
        (Input.GetKey(positive) ? 1 : 0) - (Input.GetKey(negative) ? 1 : 0);

    void ResetShield() => nextShield = time + (passive == 2 ? quickShieldDelay : shieldDelay);

    void Hurt(int damage)
    {
        if (time < wardEnd && Vector2.Distance(position, wardCenter) <= wardRadius) return;
        ResetShield();
        progress = 0;
        chopEnd = 0;
        int absorbed = Mathf.Min(shield, damage);
        shield -= absorbed;
        hp = Mathf.Max(0, hp - damage + absorbed);
        dead = hp == 0;
    }

    void GainXp(int amount)
    {
        xp += amount;
        while (xp >= Needed)
        {
            xp -= Needed;
            level++;
            maxHp++;
            hp = Mathf.Min(maxHp, hp + (passive == 1 ? 2 : 1));
            if (level % 5 == 0) { maxShield++; shield++; }
        }
    }

    void FireWeapon()
    {
        Weapon weapon = weapons[slots[selected]];
        Enemy target = Aim(weapon.range, false);
        Vector2 direction = Direction(target);
        nextShot = time + 1 / weapon.attacksPerSecond;
        ResetShield();
        if (slots[selected] == 0)
        {
            if (target != null && Vector2.Distance(position, target.point) <= weapon.bulletRange)
                DamageEnemy(target, weapon.damage);
            SpawnShot(position, direction, 0, .65f, true, new Color(1, .95f, .65f));
        }
        else SpawnShot(position, direction, weapon.damage, weapon.bulletRange, true, Color.yellow);
    }

    void Cast()
    {
        if (active < 0 || time < spellReady[active] || mana < spells[active].mana) return;
        mana -= spells[active].mana;
        spellReady[active] = time + spells[active].cooldown;
        ResetShield();
        switch (active)
        {
            case 0: wardCenter = position; wardEnd = time + wardSeconds; break;
            case 1: SpawnShot(position, Direction(Aim(fireballRange, true)), fireballDamage, fireballRange, true, new Color(1, .35f, .12f)); break;
            case 2: hp = Mathf.Min(maxHp, hp + 5); break;
        }
    }

    static Vector2 Direction(Enemy target, Vector2 origin) => target == null
        ? (Vector2) (Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector3.right)
        : (target.point - origin).normalized;
    Vector2 Direction(Enemy target) => Direction(target, position);
}
