using UnityEngine;

public partial class ForestGame
{
    static readonly Color PanelColor = new Color(.035f, .065f, .06f, .94f);

    void OnGUI()
    {
        if (!view || !hero) return;
        float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
        if (scale <= 0) return;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float width = Screen.width / scale, height = Screen.height / scale;
        DrawStats(width);
        DrawHotbar(width, height);
        if (!choosing && !dead && !won && !paused)
        {
            DrawMarkers(width, height, scale);
            string status = opening != null ? $"Unlocking tier {opening.tier + 1}  {progress:0.0}/{chestSeconds[opening.tier]:0.0}s" :
                chopEnd > 0 ? $"Chopping  {chopEnd - time:0.0}s" :
                portal && Vector2.Distance(position, Point(bossTile)) <= 1.4f ? "F  Enter portal" :
                NearbyPrompt();
            if (status.Length > 0) CenterLabel(new Rect(width / 2 - 200, height - 118, 400, 24), status);
        }
        if (choosing || paused || dead || won) DrawOverlay(width, height);
    }

    void DrawStats(float width)
    {
        Fill(new Rect(12, 12, 240, 126), PanelColor);
        Bar(22, "HP", hp, maxHp, new Color(.75f, .17f, .23f));
        Bar(47, "SHIELD", shield, maxShield, new Color(.49f, .51f, .55f));
        Bar(72, "MANA", mana, 100, new Color(.15f, .37f, .78f));
        GUI.Label(new Rect(22, 100, 220, 25), $"Level {level}    XP {xp:0}/{Needed:0}");
        GUI.Label(new Rect(width - 190, 15, 100, 28), $"Gold  {gold}");
        if (GUI.Button(new Rect(width - 86, 15, 72, 27), paused ? "Resume" : "Pause") && !dead && !won)
            paused = !paused;
        CenterLabel(new Rect(width / 2 - 80, 16, 160, 25), $"Room {room} / 3");
    }

    static void Bar(float y, string name, int value, int max, Color color)
    {
        Fill(new Rect(22, y, 220, 20), new Color(.14f, .18f, .17f));
        Fill(new Rect(22, y, 220f * value / max, 20), color);
        GUI.Label(new Rect(29, y, 210, 20), $"{name}   {value}/{max}");
    }

    void DrawHotbar(float width, float height)
    {
        for (int i = 0; i < 5; i++)
        {
            int id = i == 0 ? passive : i == 4 ? active : slots[i - 1];
            var area = new Rect(width / 2 - 242 + 98 * i, height - 89, 92, 75);
            string label = id < 0 ? "Empty" : i == 0 ? PassiveNames[id] : i == 4 ? ActiveNames[id] : weapons[id].name;
            Sprite art = id < 0 ? null : i == 0 ? passiveArt[id] : i == 4 ? activeArt[id] : weapons[id].sprite;
            bool unavailable = i == 4 && id >= 0 && (time < spellReady[id] || mana < spells[id].mana);
            Color original = GUI.color;
            if (i == selected + 1) Fill(new Rect(area.x - 3, area.y - 3, area.width + 6, area.height + 6), new Color(.95f, .75f, .3f));
            if (unavailable) GUI.color = new Color(.5f, .5f, .5f);
            bool click = GUI.Button(area, GUIContent.none);
            if (click && !choosing && !paused && !dead && !won) tooltip = tooltip == i ? -1 : i;
            Icon(art, new Rect(area.x + 30, area.y + 8, 32, 32));
            CenterLabel(new Rect(area.x, area.y + 40, area.width, 18), label);
            string key = i == 0 ? "Passive" : i == 4 ? "E" : i.ToString();
            if (i == 4 && id >= 0) key += $"  ·  {spells[id].mana} mana";
            CenterLabel(new Rect(area.x, area.y + 57, area.width, 16), key);
            GUI.color = original;
            if (tooltip == i && !choosing && !paused && !dead && !won)
            {
                var tip = new Rect(Mathf.Clamp(area.center.x - 126, 8, width - 260), area.y - 94, 252, 70);
                GUI.Box(tip, GUIContent.none);
                GUI.Label(new Rect(tip.x + 10, tip.y + 7, tip.width - 20, tip.height - 12), Detail(i, id),
                    new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
        }
    }

    string Detail(int slot, int id)
    {
        if (id < 0) return "Empty slot";
        if (slot == 0)
        {
            if (id == 0) return "Magnet\nAutomatic pickups within 4 blocks.";
            if (id == 1) return "Vitality\nHeal 1 extra HP on level-up.";
            return $"Quick Shield\nShield starts recharging after {quickShieldDelay:0.##}s.";
        }
        if (slot == 4)
        {
            string effect = id == 0 ? $"Static ward for {wardSeconds:0.##}s. Invincible inside." :
                id == 1 ? $"{fireballDamage} damage. Prioritizes nearby elites." : "Restores up to 5 HP.";
            return $"{ActiveNames[id]} · {spells[id].mana} mana · {spells[id].cooldown:0.##}s cooldown\n{effect}";
        }
        Weapon weapon = weapons[id];
        return $"{weapon.name} · {weapon.damage} damage\nAim {weapon.range} blocks · Travel {weapon.bulletRange}\n{weapon.attacksPerSecond:0.##} attacks per second";
    }

    string NearbyPrompt()
    {
        foreach (Drop drop in drops)
            if (drop.manual && Vector2.Distance(position, drop.point) <= 1.35f)
            {
                int weapon = WeaponId(drop.id);
                return "F  Pick up " + (weapon >= 0 ? weapons[weapon].name : drop.id);
            }
        Chest chest = NearestChest(1.4f);
        return chest == null ? "" : $"Hold F  ·  Tier {chest.tier + 1} chest";
    }

    void DrawMarkers(float width, float height, float scale)
    {
        if (portal) Marker(Point(bossTile), "EXIT", new Color(.3f, .8f, 1), width, height, scale);
        else if (boss != null) Marker(boss.point, "BOSS", new Color(1, .4f, .47f), width, height, scale);
        Chest chest = NearestChest(50);
        if (chest != null) Marker(chest.point, "CHEST", new Color(1, .8f, .45f), width, height, scale);
        Enemy closest = null;
        float best = 50 * 50;
        foreach (Enemy enemy in dormant.Values) ConsiderMini(enemy, ref closest, ref best);
        foreach (Enemy enemy in enemies) ConsiderMini(enemy, ref closest, ref best);
        if (closest != null) Marker(closest.point, "MINI", new Color(1, .63f, .4f), width, height, scale);
    }

    void ConsiderMini(Enemy enemy, ref Enemy closest, ref float best)
    {
        if (enemy.kind != 2) return;
        float distance = Vector2.SqrMagnitude(enemy.point - position);
        if (distance > best) return;
        best = distance;
        closest = enemy;
    }

    void Marker(Vector2 point, string label, Color color, float width, float height, float scale)
    {
        Vector3 screen = view.WorldToScreenPoint(point + Vector2.up);
        var target = new Vector2(screen.x / scale, height - screen.y / scale);
        var center = new Vector2(width / 2, height / 2);
        Vector2 direction = target - center;
        float ratio = Mathf.Max(Mathf.Abs(direction.x) / (width / 2 - 70),
            Mathf.Abs(direction.y) / (height / 2 - 115));
        bool edge = ratio > 1;
        if (edge) target = center + direction / ratio;
        string arrow = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ?
            (direction.x > 0 ? "▶" : "◀") : (direction.y > 0 ? "▼" : "▲");
        var area = new Rect(target.x - 62, target.y - 12, 124, 24);
        Fill(area, PanelColor);
        Color original = GUI.contentColor;
        GUI.contentColor = color;
        CenterLabel(area, $"{(edge ? arrow : "▼")} {label} {Vector2.Distance(position, point):0}");
        GUI.contentColor = original;
    }

    void DrawOverlay(float width, float height)
    {
        Fill(new Rect(0, 0, width, height), new Color(.01f, .03f, .02f, .84f));
        if (choosing && !paused)
        {
            CenterLabel(new Rect(0, height / 2 - 125, width, 35), "CHOOSE A STARTING WEAPON");
            for (int i = 0; i < 3; i++)
            {
                float x = width / 2 - 305 + i * 208, y = height / 2 - 75;
                Fill(new Rect(x, y, 194, 152), PanelColor);
                Icon(weapons[i].sprite, new Rect(x + 73, y + 10, 48, 48));
                CenterLabel(new Rect(x, y + 62, 194, 30), $"{i + 1}  ·  {weapons[i].name}");
                CenterLabel(new Rect(x, y + 94, 194, 24), $"{weapons[i].damage} damage  ·  {weapons[i].range} range");
                CenterLabel(new Rect(x, y + 118, 194, 24), $"{weapons[i].attacksPerSecond:0.##} attacks / second");
            }
            CenterLabel(new Rect(0, height / 2 + 95, width, 28), "Press 1, 2, or 3");
            return;
        }
        CenterLabel(new Rect(0, height / 2 - 44, width, 28), dead ? "Run ended" : won ? "Forest cleared" : "Paused");
        CenterLabel(new Rect(0, height / 2 - 10, width, 25), dead || won ? "R  Start a new run" : "Esc  Resume");
        if (paused && GUI.Button(new Rect(width / 2 - 55, height / 2 + 26, 110, 28), "Resume"))
            paused = false;
    }

    static void Icon(Sprite sprite, Rect area)
    {
        if (!sprite) return;
        Rect rect = sprite.rect;
        Texture texture = sprite.texture;
        GUI.DrawTextureWithTexCoords(area,
            texture, new Rect(rect.x / texture.width, rect.y / texture.height, rect.width / texture.width, rect.height / texture.height));
    }

    static void Fill(Rect area, Color color)
    {
        Color original = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = original;
    }

    static void CenterLabel(Rect area, string text) =>
        GUI.Label(area, text, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
}
