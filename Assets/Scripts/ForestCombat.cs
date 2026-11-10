using UnityEngine;
using Random = UnityEngine.Random;

public partial class ForestGame
{
    class Enemy
    {
        public Vector2 point;
        public int kind, hp, attacks;
        public bool alerted;
        public float ready, barrierEnd;
        public SpriteRenderer art;
        public float Size => kind < 2 ? 1 : kind;
    }

    class Shot
    {
        public Vector2 direction;
        public float remaining;
        public int damage;
        public bool friendly;
        public SpriteRenderer art;
    }

    Enemy NewEnemy(Vector2Int cell, int kind) => new Enemy {
        point = Point(cell), kind = kind,
        hp = (kind == 3 ? 120 : kind == 2 ? 42 : kind == 1 ? 10 : 12) + (room - 1) * (kind + 1) * 4
    };

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
            enemy.art = DrawSprite("Enemy", art, tint, enemy.point, enemy.Size, 2);
            enemies.Add(enemy);
        }
    }

    void TickEnemies(float dt)
    {
        WakeEnemies();
        foreach (Enemy enemy in enemies)
        {
            Vector2 delta = position - enemy.point;
            float distance = delta.magnitude;
            bool sanctuary = safe.Contains(Cell(position));
            enemy.alerted = !sanctuary && distance <= (enemy.alerted ? Mathf.Max(aggro, dropAggro) : aggro);
            if (enemy.kind == 3)
                enemy.art.color = time < enemy.barrierEnd ? new Color(.3f, .75f, 1) :
                    bossArt ? Color.white : new Color(.7f, .1f, .24f);
            if (!enemy.alerted || dead) continue;
            float reach = enemy.kind == 0 ? 1.05f : enemy.kind == 1 ? 6 : enemy.kind + .3f;
            if (distance > reach)
            {
                Vector2 next = Move(enemy.point, delta.normalized * (enemy.kind == 0 ? 2.8f : 2.2f) * dt, enemy.Size * .4f);
                if (!safe.Contains(Cell(next))) enemy.point = next;
                enemy.art.transform.position = enemy.point;
            }
            if (time < enemy.ready) continue;
            if (enemy.kind < 2 && distance > reach) continue;
            if (enemy.kind >= 2 && distance > 8) continue;
            enemy.ready = time + (enemy.kind >= 2 ? 1.6f : 1.1f);
            enemy.attacks++;
            int damage = enemy.kind + room + 1;
            if (enemy.kind == 3 && enemy.attacks % 4 == 0)
            {
                enemy.barrierEnd = time + 1.4f;
                for (int i = 0; i < 8; i++)
                    EnemyShot(enemy, Quaternion.Euler(0, 0, 45 * i) * delta.normalized, damage);
            }
            else if (enemy.kind == 0 || (enemy.kind >= 2 && distance <= reach && enemy.attacks % 2 == 1))
                Hurt(damage);
            else if (enemy.kind >= 2 && enemy.attacks % 2 == 0)
            {
                for (int i = -1; i <= 1; i++)
                    EnemyShot(enemy, Quaternion.Euler(0, 0, i * 22) * delta.normalized, damage);
            }
            else EnemyShot(enemy, delta.normalized, damage);
        }
    }

    void EnemyShot(Enemy enemy, Vector2 direction, int damage) =>
        SpawnShot(enemy.point, direction, damage, 12, false, new Color(1, .2f, .48f));

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

    void SpawnShot(Vector2 origin, Vector2 direction, int damage, float range, bool friendly, Color color)
    {
        if (direction == Vector2.zero) direction = Vector2.right;
        shots.Add(new Shot { direction = direction, remaining = range, damage = damage, friendly = friendly,
            art = DrawSprite("Projectile", null, color, origin, damage == fireballDamage ? .35f : .18f, 4) });
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
        if (time < enemy.barrierEnd) return;
        enemy.hp -= damage;
        if (enemy.hp > 0) return;
        enemies.Remove(enemy);
        Destroy(enemy.art.gameObject);
        AddDrop("xp", enemy.point, enemy.kind == 3 ? 250 : enemy.kind == 2 ? 60 : 16);
        AddDrop("gold", enemy.point + Vector2.right * .23f, (enemy.kind + 1) * 3);
        if (Random.value < .55f) AddDrop("mana", enemy.point + Vector2.up * .25f, 1);
        if (Random.value < .25f) AddDrop("heart", enemy.point + Vector2.left * .25f, 1);
        if (enemy != boss) return;
        portal = DrawSprite("Exit", ringSprite, Color.white, Point(bossTile), 2, 1);
        portal.color = new Color(.4f, .8f, 1);
    }
}
