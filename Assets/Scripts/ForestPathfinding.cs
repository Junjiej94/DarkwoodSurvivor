using System.Collections.Generic;
using UnityEngine;

public partial class ForestGame
{
    [Header("A* pathfinding")]
    [Min(.1f)] public float pathRefreshSeconds = .75f;
    [Min(100)] public int pathSearchBudget = 6000;
    static readonly Vector2Int[] PathDirections =
    {
        Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down
    };

    struct SearchNode
    {
        public Vector2Int cell;
        public int cost, priority;
    }

    // Binary min-heap keeps the frontier efficient without a package dependency.
    void PushNode(List<SearchNode> heap, SearchNode node)
    {
        heap.Add(node);
        int index = heap.Count - 1;
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (heap[parent].priority <= node.priority) break;
            heap[index] = heap[parent];
            index = parent;
        }
        heap[index] = node;
    }

    SearchNode PopNode(List<SearchNode> heap)
    {
        SearchNode first = heap[0], last = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        if (heap.Count == 0) return first;
        int index = 0;
        while (index * 2 + 1 < heap.Count)
        {
            int child = index * 2 + 1;
            if (child + 1 < heap.Count && heap[child + 1].priority < heap[child].priority) child++;
            if (heap[child].priority >= last.priority) break;
            heap[index] = heap[child];
            index = child;
        }
        heap[index] = last;
        return first;
    }

    static int Manhattan(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    bool EnemyCanStand(Vector2Int cell, float radius)
    {
        if (Blocked(Point(cell), radius)) return false;
        int extent = Mathf.CeilToInt(radius);
        for (int y = -extent; y <= extent; y++)
        for (int x = -extent; x <= extent; x++)
        {
            Vector2Int covered = Cell(Point(cell) + new Vector2(
                Mathf.Clamp(x, -radius, radius), Mathf.Clamp(y, -radius, radius)));
            if (safe.Contains(covered)) return false;
        }
        return true;
    }

    List<Vector2Int> FindPath(Enemy enemy, Vector2Int goal, float attackRange)
    {
        Vector2Int start = Cell(enemy.point);
        var frontier = new List<SearchNode>();
        var costs = new Dictionary<Vector2Int, int> { [start] = 0 };
        var parents = new Dictionary<Vector2Int, Vector2Int>();
        // Reaching any walkable tile inside attack range is sufficient.
        int allowance = Mathf.CeilToInt(attackRange * Mathf.Sqrt(2));
        PushNode(frontier, new SearchNode { cell = start, priority = Mathf.Max(0, Manhattan(start, goal) - allowance) });
        int expanded = 0;
        while (frontier.Count > 0 && expanded < pathSearchBudget)
        {
            SearchNode node = PopNode(frontier);
            if (costs[node.cell] != node.cost) continue;
            expanded++;
            if (Vector2.Distance(Point(node.cell), position) <= attackRange)
            {
                var result = new List<Vector2Int>();
                Vector2Int current = node.cell;
                while (current != start)
                {
                    result.Add(current);
                    current = parents[current];
                }
                result.Reverse();
                return result;
            }
            foreach (Vector2Int direction in PathDirections)
            {
                Vector2Int next = node.cell + direction;
                if (!EnemyCanStand(next, enemy.Size * .4f)) continue;
                int cost = node.cost + 1;
                if (costs.TryGetValue(next, out int previous) && previous <= cost) continue;
                costs[next] = cost;
                parents[next] = node.cell;
                PushNode(frontier, new SearchNode {
                    cell = next, cost = cost,
                    priority = cost + Mathf.Max(0, Manhattan(next, goal) - allowance)
                });
            }
        }
        return new List<Vector2Int>();
    }

    Vector2 FollowPath(Enemy enemy, float reach, float dt)
    {
        Vector2Int goal = Cell(position);
        if (time >= enemy.pathReady || enemy.path == null || enemy.goal != goal)
        {
            enemy.path = FindPath(enemy, goal, reach);
            enemy.goal = goal;
            enemy.pathReady = time + Mathf.Max(.1f, pathRefreshSeconds);
        }
        if (enemy.path.Count == 0) return enemy.point;
        Vector2 waypoint = Point(enemy.path[0]);
        Vector2 target = Vector2.MoveTowards(enemy.point, waypoint, (enemy.kind == 0 ? 2.8f : 2.2f) * dt);
        Vector2 next = Move(enemy.point, target - enemy.point, enemy.Size * .4f);
        if (Vector2.SqrMagnitude(next - waypoint) < .0001f) enemy.path.RemoveAt(0);
        return next;
    }

    void DestroyTree(Vector2Int tile)
    {
        if (!Inside(tile.x, tile.y) || tiles[tile.y, tile.x] != 1 || safeWall.Contains(tile)) return;
        tiles[tile.y, tile.x] = 2;
        centerTile = new Vector2Int(-100, -100);
    }

    void SmashTrees(Vector2 origin, float radius)
    {
        Vector2Int center = Cell(origin);
        int extent = Mathf.CeilToInt(radius);
        for (int y = center.y - extent; y <= center.y + extent; y++)
        for (int x = center.x - extent; x <= center.x + extent; x++)
        {
            var tile = new Vector2Int(x, y);
            if (Vector2.Distance(origin, Point(tile)) <= radius) DestroyTree(tile);
        }
    }
}
