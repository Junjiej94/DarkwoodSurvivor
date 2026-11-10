using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class ForestGame
{
    class Chest
    {
        public Vector2 point;
        public int tier;
        public bool open;
        public SpriteRenderer art;
    }

    class Drop
    {
        public string id;
        public Vector2 point;
        public int count;
        public bool manual;
        public SpriteRenderer art;
    }

    [Serializable] class LootFile { public LootTier[] tiers = null; }
    [Serializable] class LootTier { public LootEntry[] drops = null; }
    [Serializable] class LootEntry { public string id; public float weight; public int min = 1, max = 1; }

    readonly List<LootEntry[]> loot = new List<LootEntry[]>();

    void ReadLoot()
    {
        TextAsset source = lootTable ? lootTable : Resources.Load<TextAsset>("ForestLootTable");
        try
        {
            var file = source ? JsonUtility.FromJson<LootFile>(source.text) : null;
            for (int i = 0; i < 3; i++)
            {
                var valid = new List<LootEntry>();
                if (file?.tiers != null && i < file.tiers.Length && file.tiers[i]?.drops != null)
                    foreach (LootEntry entry in file.tiers[i].drops)
                        if (entry != null && entry.weight > 0 && !float.IsInfinity(entry.weight) &&
                            entry.min > 0 && entry.max >= entry.min && entry.max < int.MaxValue && KnownItem(entry.id))
                            valid.Add(entry);
                if (valid.Count == 0)
                {
                    Debug.LogWarning("Chest tier " + (i + 1) + " has no valid loot. Using gold.", this);
                    valid.Add(new LootEntry { id = "gold", min = 5, max = 5, weight = 1 });
                }
                loot.Add(valid.ToArray());
            }
        }
        catch (ArgumentException ex)
        {
            Debug.LogWarning("Invalid loot JSON. " + ex.Message, this);
            loot.Clear();
            for (int i = 0; i < 3; i++)
                loot.Add(new[] { new LootEntry { id = "gold", min = 5, max = 5, weight = 1 } });
        }
    }

    static readonly string[] ActiveIds = { "ward", "fireball", "heal" };
    static readonly string[] PassiveIds = { "magnet", "vitality", "quickShield" };

    int WeaponId(string id)
    {
        if (id == "pistol") return weapons.Length > 3 ? 3 : -1;
        if (id != null && id.StartsWith("weapon:", StringComparison.Ordinal) &&
            int.TryParse(id.Substring(7), out int index) && index >= 0 && index < weapons.Length) return index;
        return -1;
    }

    bool KnownItem(string id) => id == "gold" || id == "xp" || id == "heart" || id == "mana" ||
        id == "stamina" || id == "book" || WeaponId(id) >= 0 ||
        Array.IndexOf(ActiveIds, id) >= 0 || Array.IndexOf(PassiveIds, id) >= 0;

    void AddDrop(string id, Vector2 point, int count)
    {
        int weapon = WeaponId(id), ability = Array.IndexOf(ActiveIds, id), trait = Array.IndexOf(PassiveIds, id);
        bool manual = weapon >= 0 || ability >= 0 || trait >= 0 || id == "stamina" || id == "book";
        Sprite art = weapon >= 0 ? weapons[weapon].sprite : ability >= 0 ? activeArt[ability] : trait >= 0 ? passiveArt[trait] :
            id == "gold" ? goldArt : id == "xp" ? xpArt : id == "heart" ? heartArt :
            id == "mana" ? manaArt : id == "book" ? bookArt : staminaArt;
        Color tint = id == "gold" ? Color.yellow : id == "heart" ? Color.red : id == "mana" ? Color.blue :
            id == "xp" ? Color.green : id == "stamina" ? new Color(1, .5f, .1f) : new Color(.9f, .6f, 1);
        drops.Add(new Drop { id = id, point = point, count = count, manual = manual,
            art = DrawSprite(id, art, tint, point, manual ? .65f : .35f, 2) });
    }

    void CollectLoot()
    {
        float radius = passive == 0 ? 4 : .55f;
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            Drop drop = drops[i];
            if (drop.manual || Vector2.SqrMagnitude(drop.point - position) > radius * radius) continue;
            switch (drop.id)
            {
                case "gold": gold += drop.count; break;
                case "xp": GainXp(drop.count); break;
                case "mana": mana = Mathf.Min(100, mana + drop.count); break;
                case "heart": hp = Mathf.Min(maxHp, hp + drop.count); break;
            }
            RemoveDrop(drop);
        }
    }

    void RemoveDrop(Drop drop)
    {
        Destroy(drop.art.gameObject);
        drops.Remove(drop);
    }

    void Equip(Drop drop)
    {
        int weapon = WeaponId(drop.id);
        int ability = Array.IndexOf(ActiveIds, drop.id);
        int trait = Array.IndexOf(PassiveIds, drop.id);
        if (weapon >= 0)
        {
            int empty = Array.IndexOf(slots, -1);
            slots[empty >= 0 ? empty : selected] = weapon;
        }
        else if (ability >= 0) active = ability;
        else if (trait >= 0) passive = trait;
        else if (drop.id == "stamina") staminaEnd = time + staminaSeconds;
        else if (Random.Range(0, 2) == 0) active = Random.Range(0, 3);
        else passive = Random.Range(0, 3);
        RemoveDrop(drop);
    }

    void Interact(bool moving, float dt)
    {
        if (moving) chopEnd = 0;
        if (!moving && Input.GetKeyDown(KeyCode.Q) && chopEnd == 0)
        {
            var cell = Cell(position);
            float nearest = 1;
            for (int y = cell.y - 1; y <= cell.y + 1; y++)
            for (int x = cell.x - 1; x <= cell.x + 1; x++)
            {
                var tile = new Vector2Int(x, y);
                if (!Inside(x, y) || tiles[y, x] != 1 || safeWall.Contains(tile)) continue;
                float distance = Vector2.Distance(position, Point(tile));
                if (distance > nearest) continue;
                nearest = distance;
                chopping = tile;
                chopEnd = time + chopSeconds;
            }
        }
        if (chopEnd > 0)
        {
            ResetShield();
            if (time >= chopEnd)
            {
                tiles[chopping.y, chopping.x] = 2;
                centerTile = new Vector2Int(-100, -100);
                chopEnd = 0;
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (portal && Vector2.Distance(position, Point(bossTile)) <= 1.4f)
            {
                if (room == 3) won = true;
                else { room++; BuildRoom(); }
                return;
            }
            Drop pickup = null;
            float best = 1.35f;
            foreach (Drop drop in drops)
            {
                float distance = Vector2.Distance(position, drop.point);
                if (!drop.manual || distance > best) continue;
                pickup = drop;
                best = distance;
            }
            if (pickup != null)
            {
                Equip(pickup);
                opening = null;
                progress = 0;
                return;
            }
            opening = NearestChest(1.4f);
            progress = 0;
        }

        if (!Input.GetKey(KeyCode.F))
        {
            opening = null;
            progress = 0;
        }
        if (opening == null) return;
        if (moving || Vector2.Distance(position, opening.point) > 1.4f || chopEnd > 0)
        {
            progress = 0;
            return;
        }
        ResetShield();
        progress += dt;
        if (progress < chestSeconds[opening.tier]) return;
        opening.open = true;
        LootEntry[] entries = loot[opening.tier];
        var weights = new float[entries.Length];
        for (int i = 0; i < weights.Length; i++) weights[i] = entries[i].weight;
        LootEntry item = entries[Roll(weights)];
        AddDrop(item.id, opening.point, Random.Range(item.min, item.max + 1));
        opening = null;
        progress = 0;
    }

    Chest NearestChest(float range)
    {
        Chest result = null;
        foreach (Chest chest in chests)
        {
            if (chest.open) continue;
            float distance = Vector2.Distance(position, chest.point);
            if (distance > range) continue;
            range = distance;
            result = chest;
        }
        return result;
    }
}
