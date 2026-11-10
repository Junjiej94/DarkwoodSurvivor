using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class ForestGame
{
    const int ViewWidth = 61, ViewHeight = 21;
    readonly byte[,] tiles = new byte[Rows, Columns];
    readonly List<Chest> chests = new List<Chest>();
    readonly Dictionary<Vector2Int, Enemy> dormant = new Dictionary<Vector2Int, Enemy>();
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<Shot> shots = new List<Shot>();
    readonly List<Drop> drops = new List<Drop>();
    SpriteRenderer[,] terrain, ground;
    Vector2Int centerTile, bossTile, lastWakeCell;
    RectInt safe, safeWall;
    Enemy boss;
    SpriteRenderer portal;

    Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(-p.y));
    Vector2 Point(Vector2Int cell) => new Vector2(cell.x, -cell.y);
    bool Inside(int x, int y) => x >= 0 && x < Columns && y >= 0 && y < Rows;

    void BuildRoom()
    {
        if (visuals)
        {
            visuals.gameObject.SetActive(false);
            Destroy(visuals.gameObject);
        }
        visuals = new GameObject("Room " + room).transform;
        chests.Clear(); dormant.Clear(); enemies.Clear(); shots.Clear(); drops.Clear();
        opening = null;
        progress = chopEnd = wardEnd = 0;
        portal = null;
        backdrop = null;
        lastWakeCell = new Vector2Int(-100, -100);
        ReadMap();
        var spawn = new Vector2Int(Random.Range(125, 475), Random.Range(100, 300));
        safe = new RectInt(spawn.x - 5, spawn.y - 2, 10, 5);
        safeWall = new RectInt(safe.xMin - 1, safe.yMin - 1, 12, 7);
        bossTile = SelectBossTile();
        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Columns; x++)
        {
            if (tiles[y, x] == 6) tiles[y, x] = 0;
        }

        // Give every seed a walkable route from the clearing to the boss.
        int routeX = Mathf.Abs(spawn.x - bossTile.x) < 7 ? spawn.x + 7 : bossTile.x;
        for (int x = Mathf.Min(spawn.x, routeX); x <= Mathf.Max(spawn.x, routeX); x++)
            tiles[spawn.y, x] = 0;
        for (int y = Mathf.Min(spawn.y, bossTile.y); y <= Mathf.Max(spawn.y, bossTile.y); y++)
            tiles[y, routeX] = 0;
        for (int x = Mathf.Min(routeX, bossTile.x); x <= Mathf.Max(routeX, bossTile.x); x++)
            tiles[bossTile.y, x] = 0;

        for (int y = safeWall.yMin; y < safeWall.yMax; y++)
        for (int x = safeWall.xMin; x < safeWall.xMax; x++)
        {
            bool interior = safe.Contains(new Vector2Int(x, y));
            tiles[y, x] = (byte)(interior || y == spawn.y ? 0 : 1);
        }
        for (int y = bossTile.y - 4; y <= bossTile.y + 4; y++)
        for (int x = bossTile.x - 4; x <= bossTile.x + 4; x++)
            tiles[y, x] = 0;
        tiles[bossTile.y, bossTile.x] = 6;

        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Columns; x++)
        {
            var cell = new Vector2Int(x, y);
            switch (tiles[y, x])
            {
                case 3: chests.Add(new Chest { point = Point(cell), tier = Roll(new[] { 7f, 2.5f, 1f }) }); break;
                case 4: dormant.Add(cell, NewEnemy(cell, Random.Range(0, 2))); break;
                case 5:
                    var footprint = new RectInt(x - 1, y - 1, 3, 3);
                    if (x > 0 && x < Columns - 1 && y > 0 && y < Rows - 1 && !footprint.Overlaps(safeWall))
                    {
                        foreach (Vector2Int nearby in footprint.allPositionsWithin)
                            if (tiles[nearby.y, nearby.x] == 1) tiles[nearby.y, nearby.x] = 0;
                        dormant.Add(cell, NewEnemy(cell, 2));
                    }
                    break;
                case 6: boss = NewEnemy(cell, 3); dormant.Add(cell, boss); break;
            }
        }
        position = Point(spawn);
        if (room == 1)
        {
            // Supply a nearby chest and a spell in every run for the opening loop.
            var intro = spawn + Vector2Int.left * 3;
            chests.Add(new Chest { point = Point(intro), tier = 0 });
            AddDrop("fireball", Point(spawn + Vector2Int.right * 3), 1);
        }
        if (background) backdrop = DrawSprite("Background", background, Color.white, position, 50, -2);
        hero = DrawSprite("Player", playerArt, Color.cyan, position, 1, 3);
        ward = DrawSprite("Ward", ringSprite, Color.white, position, wardRadius * 2, 5);
        ward.color = new Color(.3f, .9f, 1, .8f);
        terrain = new SpriteRenderer[ViewHeight, ViewWidth];
        ground = new SpriteRenderer[ViewHeight, ViewWidth];
        for (int y = 0; y < ViewHeight; y++)
        for (int x = 0; x < ViewWidth; x++)
        {
            terrain[y, x] = DrawSprite("Terrain", grass, Color.green, position, 1, 0);
            ground[y, x] = DrawSprite("Ground Under Tree", grass, new Color(.24f, .47f, .28f), position, 1, -1);
            ground[y, x].enabled = false;
        }
        centerTile = new Vector2Int(-100, -100);
        RefreshView();
    }

    void ReadMap()
    {
        if (forestMap && room == 1)
        {
            try
            {
                using var reader = new StringReader(forestMap.text);
                for (int y = 0; y < Rows; y++)
                {
                    string line = reader.ReadLine();
                    if (line == null) throw new FormatException("Expected 400 rows.");
                    int x = 0;
                    foreach (char c in line)
                    {
                        if (c == ',' || char.IsWhiteSpace(c)) continue;
                        if (c < '0' || c > '6' || x >= Columns)
                            throw new FormatException("Invalid tile or extra column in row " + (y + 1));
                        tiles[y, x++] = (byte)(c - '0');
                    }
                    if (x != Columns) throw new FormatException("Expected 600 columns in row " + (y + 1));
                }
                if (!string.IsNullOrWhiteSpace(reader.ReadToEnd())) throw new FormatException("Extra map rows.");
                return;
            }
            catch (FormatException ex) { Debug.LogWarning("Using generated map. " + ex.Message, this); }
        }

        float[] weights = new float[6];
        Array.Copy(tileWeights, weights, Mathf.Min(weights.Length, tileWeights.Length));
        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Columns; x++)
            tiles[y, x] = (byte)Mathf.Max(0, Roll(weights));
    }

    Vector2Int SelectBossTile()
    {
        var authored = new List<Vector2Int>();
        var eligible = new List<Vector2Int>();
        for (int y = 20; y < Rows - 20; y++)
        for (int x = 20; x < Columns - 20; x++)
        {
            if (x != 20 && y != 20 && x != Columns - 21 && y != Rows - 21) continue;
            var cell = new Vector2Int(x, y);
            eligible.Add(cell);
            if (tiles[y, x] == 6) authored.Add(cell);
        }
        if (authored.Count > 0) return authored[Random.Range(0, authored.Count)];
        var candidates = new List<Vector2Int>();
        float bossWeight = tileWeights.Length > 6 ? tileWeights[6] : 0;
        float total = Mathf.Max(0, bossWeight);
        for (int i = 0; i < Mathf.Min(6, tileWeights.Length); i++) total += Mathf.Max(0, tileWeights[i]);
        foreach (Vector2Int cell in eligible)
            if (bossWeight > 0 && Random.value * total < bossWeight) candidates.Add(cell);
        var pool = candidates.Count > 0 ? candidates : eligible;
        return pool[Random.Range(0, pool.Count)];
    }

    static int Roll(float[] weights)
    {
        float total = 0;
        foreach (float weight in weights) total += Mathf.Max(0, weight);
        if (total <= 0) return -1;
        float roll = Random.Range(0f, total);
        int last = -1;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0) continue;
            last = i;
            roll -= weights[i];
            if (roll < 0) return i;
        }
        return last;
    }

    bool Blocked(Vector2 point, float radius)
    {
        var start = Cell(point + new Vector2(-radius, radius));
        var end = Cell(point + new Vector2(radius, -radius));
        for (int y = start.y; y <= end.y; y++)
        for (int x = start.x; x <= end.x; x++)
            if (!Inside(x, y) || tiles[y, x] == 1) return true;
        return false;
    }

    Vector2 Move(Vector2 point, Vector2 delta, float radius)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .2f));
        delta /= steps;
        for (int i = 0; i < steps; i++)
        {
            var x = point + new Vector2(delta.x, 0);
            if (!Blocked(x, radius)) point = x;
            var y = point + new Vector2(0, delta.y);
            if (!Blocked(y, radius)) point = y;
        }
        return point;
    }

    void RefreshView()
    {
        view.transform.position = new Vector3(position.x, position.y, -10);
        if (backdrop) backdrop.transform.position = position;
        hero.transform.position = position;
        ward.enabled = time < wardEnd;
        ward.transform.position = wardCenter;
        var cell = Cell(position);
        if (cell != centerTile)
        {
            centerTile = cell;
            for (int y = 0; y < ViewHeight; y++)
            for (int x = 0; x < ViewWidth; x++)
            {
                var tile = cell + new Vector2Int(x - ViewWidth / 2, y - ViewHeight / 2);
                SpriteRenderer renderer = terrain[y, x];
                renderer.enabled = Inside(tile.x, tile.y);
                ground[y, x].enabled = false;
                if (!renderer.enabled) continue;
                int code = tiles[tile.y, tile.x];
                if (code == 1 || code == 2)
                {
                    ground[y, x].enabled = true;
                    Paint(ground[y, x], grass, new Color(.24f, .47f, .28f), Point(tile), 1);
                }
                Sprite art = code == 1 ? tree : code == 2 ? stump : grass;
                Color tint = code == 1 ? new Color(.09f, .28f, .15f) :
                    code == 2 ? new Color(.34f, .32f, .16f) : new Color(.24f, .47f, .28f);
                if (background && !art && code != 1 && code != 2) tint.a = .55f;
                Paint(renderer, art, tint, Point(tile), 1);
            }
        }
        foreach (Chest chest in chests)
        {
            bool visible = Vector2.SqrMagnitude(chest.point - position) < 32 * 32;
            if (visible && !chest.art)
                chest.art = DrawSprite("Chest", chestArt, new Color(.77f, .48f, .2f), chest.point, 1, 1);
            if (chest.art)
            {
                chest.art.enabled = visible;
                chest.art.color = chest.open ? Color.gray : chestArt ? Color.white : new Color(.77f, .48f, .2f);
            }
        }
    }
}
