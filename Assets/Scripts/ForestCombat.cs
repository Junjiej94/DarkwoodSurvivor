using UnityEngine;
using Random = UnityEngine.Random;

public partial class ForestGame
{
    class Enemy
    {
        public Vector2 point;
<<<<<<< HEAD
        public int kind, hp, attacks;
        public bool alerted, dying;
        public float ready, barrierEnd, animTimer, attackAnimEnd, deathEnd;
        public int frame;
=======
        public int kind, hp, maxHp, attacks;
        public bool alerted;
        public float ready, barrierEnd, pathReady;
        public Vector2Int goal;
        public System.Collections.Generic.List<Vector2Int> path;
>>>>>>> 7afac70ea5cc00b3aadc85517e6bc93474d6475e
        public SpriteRenderer art;
        public ForestAnimatedActor animation;
        public float Size => kind < 2 ? 1 : kind;
    }

    class Shot
    {
        public Vector2 direction;
        public float remaining;
        public int damage;
        public bool friendly, destroysTrees;
        public SpriteRenderer art;
    }

    Enemy NewEnemy(Vector2Int cell, int kind)
    {
        int health = (kind == 3 ? 120 : kind == 2 ? 42 : kind == 1 ? 10 : 12)
            + (room - 1) * (kind + 1) * 4;
        return new Enemy { point = Point(cell), kind = kind, hp = health, maxHp = health };
    }

    void WakeEnemies()
    {
        var cell = Cell(position);
        if (lastWakeCell == cell) return;
        lastWakeCell = cell;
        float reach = Mathf.Max(22, aggro + 1, fireballRange + 1);
        foreach (Weapon weapon in weapons) reach = Mathf.Max(reach, weapon.range + 1, weapon.bulletRange + 1);
        int radius = Mathf.Min(Columns, Mathf.CeilToInt(reach));
        for (int y = Mathf.Max(0, cell.y - radius); y <= Mathf.Min(Rows - 1, cell.y + radius); y++)
        for (int x = Mathf.Max(0, cell.x - radius); x <= Mathf.Min(Columns - 1, cell.x + radius); x++)
        {
            var key = new Vector2Int(x, y);
            if (!dormant.TryGetValue(key, out Enemy enemy)) continue;
            dormant.Remove(key);
            Sprite art = enemy.kind == 3 ? bossArt : enemy.kind == 2 ? miniArt : enemy.kind == 1 ? rangedArt : meleeArt;
            Color tint = enemy.kind == 3 ? new Color(.7f, .1f, .24f) : enemy.kind == 2 ? new Color(.83f, .3f, .12f) :
                enemy.kind == 1 ? new Color(.68f, .32f, .76f) : new Color(.8f, .22f, .28f);
            ForestAnimatedActor prefab = enemy.kind == 3 ? bossPrefab : enemy.kind == 2 ? minibossPrefab :
                enemy.kind == 1 ? rangedPrefab : meleePrefab;
            enemy.animation = SpawnActor(prefab, enemy.point, enemy.Size, 2);
            enemy.art = enemy.animation ? enemy.animation.mainSprite : DrawSprite("Enemy", art, tint, enemy.point, enemy.Size, 2);
            enemies.Add(enemy);
        }
    }

    void TickEnemies(float dt)
    {
        WakeEnemies();
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];

            // 1. Handle Death Animation before removing the enemy
            if (enemy.dying)
            {
                enemy.animTimer += dt;
                if (enemy.animTimer >= 0.12f)
                {
                    enemy.animTimer = 0f;
                    enemy.frame = Mathf.Min(3, enemy.frame + 1);
                }
                UpdateEnemySprite(enemy, 3); // 3 = Death row
                if (time >= enemy.deathEnd)
                {
                    Destroy(enemy.art.gameObject);
                    enemies.RemoveAt(i);
                }
                continue;
            }

            Vector2 delta = position - enemy.point;
            float distance = delta.magnitude;
            bool sanctuary = safe.Contains(Cell(position));
            enemy.alerted = !sanctuary && distance <= (enemy.alerted ? Mathf.Max(aggro, dropAggro) : aggro);

            if (enemy.kind == 3)
                enemy.art.color = time < enemy.barrierEnd ? new Color(.3f, .75f, 1) :
                    bossArt ? Color.white : new Color(.7f, .1f, .24f);

            bool isWalking = false;

            if (enemy.alerted && !dead)
            {
<<<<<<< HEAD
                // Flip sprite left/right toward the player
                if (Mathf.Abs(delta.x) > 0.05f)
                    enemy.art.flipX = delta.x < 0f;

                float reach = enemy.kind == 0 ? 1.05f : enemy.kind == 1 ? 6 : enemy.kind + .3f;
                if (distance > reach)
                {
                    Vector2 next = Move(enemy.point, delta.normalized * (enemy.kind == 0 ? 2.8f : 2.2f) * dt, enemy.Size * .4f);
                    if (!safe.Contains(Cell(next)))
                    {
                        enemy.point = next;
                        isWalking = true;
                    }
                    enemy.art.transform.position = enemy.point;
                }

                bool canAttack = time >= enemy.ready &&
                    ((enemy.kind < 2 && distance <= reach) || (enemy.kind >= 2 && distance <= 8));

                if (canAttack)
                {
                    enemy.ready = time + (enemy.kind >= 2 ? 1.6f : 1.1f);
                    enemy.attackAnimEnd = time + 0.4f; // Plays 4 attack frames over 0.4s
                    enemy.frame = 0;
                    enemy.animTimer = 0f;
                    enemy.attacks++;

                    int damage = enemy.kind + room + 1;
                    if (enemy.kind == 3 && enemy.attacks % 4 == 0)
                    {
                        enemy.barrierEnd = time + 1.4f;
                        for (int s = 0; s < 8; s++)
                            EnemyShot(enemy, Quaternion.Euler(0, 0, 45 * s) * delta.normalized, damage);
                    }
                    else if (enemy.kind == 0 || (enemy.kind >= 2 && distance <= reach && enemy.attacks % 2 == 1))
                        Hurt(damage);
                    else if (enemy.kind >= 2 && enemy.attacks % 2 == 0)
                    {
                        for (int s = -1; s <= 1; s++)
                            EnemyShot(enemy, Quaternion.Euler(0, 0, s * 22) * delta.normalized, damage);
                    }
                    else EnemyShot(enemy, delta.normalized, damage);
                }
            }

            // 2. Pick animation row: 2 = Attack, 1 = Walk, 0 = Idle
            int state = time < enemy.attackAnimEnd ? 2 : (isWalking ? 1 : 0);
            enemy.animTimer += dt;
            if (enemy.animTimer >= 0.1f)
=======
                Vector2 next = FollowPath(enemy, reach, dt);
                if (!safe.Contains(Cell(next))) enemy.point = next;
                if (enemy.animation) enemy.animation.transform.position = enemy.point;
                else enemy.art.transform.position = enemy.point;
            }
            if (time < enemy.ready) continue;
            if (enemy.kind < 2 && distance > reach) continue;
            if (enemy.kind >= 2 && distance > 8) continue;
            enemy.ready = time + (enemy.kind >= 2 ? 1.6f : 1.1f);
            enemy.attacks++;
            if (enemy.animation) enemy.animation.Trigger("Attack");
            if (enemy.kind >= 2) SmashTrees(enemy.point, enemy.Size / 2 + 1);
            int damage = enemy.kind + room + 1;
            if (enemy.kind == 3 && enemy.attacks % 4 == 0)
>>>>>>> 7afac70ea5cc00b3aadc85517e6bc93474d6475e
            {
                enemy.animTimer = 0f;
                enemy.frame = (enemy.frame + 1) % 4;
            }
            UpdateEnemySprite(enemy, state);
        }
    }

<<<<<<< HEAD
void UpdateEnemySprite(Enemy enemy, int state)
{
    Sprite[] frames = null;
    if (enemy.kind == 3)
        frames = state == 3 ? bossDeath : state == 2 ? bossAttack : state == 1 ? bossWalk : bossIdle;
    else if (enemy.kind == 2)
        frames = state == 3 ? miniDeath : state == 2 ? miniAttack : state == 1 ? miniWalk : miniIdle;
    else if (enemy.kind == 1)
        frames = state == 3 ? rangedDeath : state == 2 ? rangedAttack : state == 1 ? rangedWalk : rangedIdle;
    else
        frames = state == 3 ? meleeDeath : state == 2 ? meleeAttack : state == 1 ? meleeWalk : meleeIdle;

    if (frames != null && frames.Length > 0)
    {
        Sprite s = frames[enemy.frame % frames.Length];
        if (s != null)
        {
            enemy.art.sprite = s;
            Vector3 bounds = s.bounds.size;
            if (bounds.x > 0f && bounds.y > 0f)
                enemy.art.transform.localScale = new Vector3(enemy.Size / bounds.x, enemy.Size / bounds.y, 1f);
        }
    }
}

    void EnemyShot(Enemy enemy, Vector2 direction, int damage)
    {
        // Use arrowArt only for ranged mob (kind == 1); other bosses keep magic orbs
        Sprite projSprite = (enemy.kind == 1 && arrowArt != null) ? arrowArt : null;
        Color projColor = projSprite != null ? Color.white : new Color(1, .2f, .48f);
        float projSize = projSprite != null ? 1.1f : 0.18f;
        SpawnShot(enemy.point, direction, damage, 12, false, projColor, projSprite, projSize);
    }
=======
    void EnemyShot(Enemy enemy, Vector2 direction, int damage) =>
        SpawnShot(enemy.point, direction, damage, 12, false, new Color(1, .2f, .48f), enemy.kind >= 2);
>>>>>>> 7afac70ea5cc00b3aadc85517e6bc93474d6475e

    Enemy Aim(float range, bool priority)
    {
        Enemy closest = null;
        float best = range * range;
        foreach (Enemy enemy in enemies)
        {
            float distance = Vector2.SqrMagnitude(enemy.point - position);
            if (distance > range * range) continue;
            bool elite = enemy.kind >= 2;
            bool currentElite = closest != null && closest.kind >= 2;
            if (priority && currentElite && !elite) continue;
            if (distance > best && (!priority || !elite || currentElite)) continue;
            best = distance;
            closest = enemy;
        }
        return closest;
    }

<<<<<<< HEAD
    void SpawnShot(Vector2 origin, Vector2 direction, int damage, float range, bool friendly, Color color, Sprite customArt = null, float customSize = -1f)
    {
        if (direction == Vector2.zero) direction = Vector2.right;
        float size = customSize > 0f ? customSize : (damage == fireballDamage ? .35f : .18f);
        SpriteRenderer sr = DrawSprite("Projectile", customArt, color, origin, size, 4);

        if (customArt != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        shots.Add(new Shot { direction = direction, remaining = range, damage = damage, friendly = friendly, art = sr });
=======
    void SpawnShot(Vector2 origin, Vector2 direction, int damage, float range, bool friendly, Color color, bool destroysTrees = false)
    {
        if (direction == Vector2.zero) direction = Vector2.right;
        shots.Add(new Shot { direction = direction, remaining = range, damage = damage, friendly = friendly, destroysTrees = destroysTrees,
            art = DrawSprite("Projectile", null, color, origin, damage == fireballDamage ? .35f : .18f, 4) });
>>>>>>> 7afac70ea5cc00b3aadc85517e6bc93474d6475e
    }

    void TickShots(float dt)
    {
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            Shot shot = shots[i];
            float distance = Mathf.Min(shot.remaining, dt * (shot.friendly ? 20 : 8));
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / .15f));
            Vector2 delta = shot.direction * (distance / steps);
            Vector2 point = shot.art.transform.position;
            bool hit = false;
            for (int j = 0; j < steps && !hit; j++)
            {
                point += delta;
                if (shot.destroysTrees) DestroyTree(Cell(point));
                hit = Blocked(point, 0);
                if (hit || shot.damage == 0) continue;
                if (!shot.friendly)
                {
                    hit = safe.Contains(Cell(point)) || Vector2.SqrMagnitude(position - point) < .16f;
                    if (hit && !safe.Contains(Cell(point))) Hurt(shot.damage);
                    continue;
                }
                for (int k = 0; k < enemies.Count; k++)
                {
                    Enemy enemy = enemies[k];
                    Vector2 offset = point - enemy.point;
                    if (Mathf.Abs(offset.x) > enemy.Size / 2 || Mathf.Abs(offset.y) > enemy.Size / 2) continue;
                    DamageEnemy(enemy, shot.damage);
                    hit = true;
                    break;
                }
            }
            shot.art.transform.position = point;
            shot.remaining -= distance;
            if (!hit && shot.remaining > 0) continue;
            Destroy(shot.art.gameObject);
            shots.RemoveAt(i);
        }
    }

    void DamageEnemy(Enemy enemy, int damage)
    {
        if (enemy.dying || time < enemy.barrierEnd) return;
        enemy.hp -= damage;
        if (enemy.animation) enemy.animation.Trigger("Hurt");
        if (enemy.hp > 0) return;
<<<<<<< HEAD

        enemy.dying = true;
        enemy.frame = 0;
        enemy.animTimer = 0f;
        enemy.deathEnd = time + 0.48f; // Plays all 4 death frames before disappearing

=======
        enemies.Remove(enemy);
        if (enemy.animation) enemy.animation.Die(true);
        else Destroy(enemy.art.gameObject);
>>>>>>> 7afac70ea5cc00b3aadc85517e6bc93474d6475e
        AddDrop("xp", enemy.point, enemy.kind == 3 ? 250 : enemy.kind == 2 ? 60 : 16);
        AddDrop("gold", enemy.point + Vector2.right * .23f, (enemy.kind + 1) * 3);
        if (Random.value < .55f) AddDrop("mana", enemy.point + Vector2.up * .25f, 1);
        if (Random.value < .25f) AddDrop("heart", enemy.point + Vector2.left * .25f, 1);
        if (enemy != boss) return;
        portal = DrawSprite("Exit", ringSprite, Color.white, Point(bossTile), 2, 1);
        portal.color = new Color(.4f, .8f, 1);
    }
}
