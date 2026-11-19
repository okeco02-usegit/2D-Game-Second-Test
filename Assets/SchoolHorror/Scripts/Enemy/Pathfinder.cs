using System.Collections.Generic;
using UnityEngine;

namespace SchoolHorror
{
    /// <summary>
    /// Tìm đường ngắn nhất bằng A* trên lưới 4 hướng. Cửa đóng đi được nhưng tốn thêm chi phí (vì phải phá);
    /// cửa đã khóa thì coi như tường, buộc phải vòng lối khác.
    /// </summary>
    public sealed class Pathfinder
    {
        const float ClosedDoorCost = 4f;
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        readonly SchoolMap map;
        readonly float[] cost;
        readonly int[] cameFrom;
        readonly bool[] closed;
        readonly List<int> open = new List<int>();

        public Pathfinder(SchoolMap map)
        {
            this.map = map;
            int size = map.Width * map.Height;
            cost = new float[size];
            cameFrom = new int[size];
            closed = new bool[size];
        }

        /// <summary>Ghi đường đi (không gồm ô xuất phát) vào result. Trả về false nếu không có đường.</summary>
        public bool FindPath(Vector2Int start, Vector2Int goal, List<Vector2Int> result, HashSet<Door> blockedDoors = null)
        {
            result.Clear();
            for (int i = 0; i < cost.Length; i++) { cost[i] = float.MaxValue; closed[i] = false; }
            open.Clear();

            int startIndex = Index(start), goalIndex = Index(goal);
            cost[startIndex] = 0f;
            cameFrom[startIndex] = -1;
            open.Add(startIndex);

            while (open.Count > 0)
            {
                int current = PopCheapest(goal);
                if (current == goalIndex)
                {
                    for (int i = goalIndex; i != startIndex; i = cameFrom[i]) result.Add(Tile(i));
                    result.Reverse();
                    return true;
                }
                closed[current] = true;

                Vector2Int tile = Tile(current);
                foreach (Vector2Int dir in Directions)
                {
                    Vector2Int next = tile + dir;
                    if (!map.InBounds(next.x, next.y) || !map.IsWalkableForAngel(next.x, next.y)) continue;
                    Door door = map.GetDoor(next);
                    if (door != null && blockedDoors != null && blockedDoors.Contains(door)) continue;
                    int nextIndex = Index(next);
                    if (closed[nextIndex]) continue;

                    float newCost = cost[current] + 1f + (door != null && !door.IsOpen ? ClosedDoorCost : 0f);
                    if (newCost >= cost[nextIndex]) continue;
                    if (cost[nextIndex] == float.MaxValue) open.Add(nextIndex);
                    cost[nextIndex] = newCost;
                    cameFrom[nextIndex] = current;
                }
            }
            return false;
        }

        int PopCheapest(Vector2Int goal)
        {
            int bestSlot = 0;
            float bestScore = float.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                Vector2Int t = Tile(open[i]);
                float score = cost[open[i]] + Mathf.Abs(t.x - goal.x) + Mathf.Abs(t.y - goal.y);
                if (score < bestScore) { bestScore = score; bestSlot = i; }
            }
            int index = open[bestSlot];
            open[bestSlot] = open[open.Count - 1];
            open.RemoveAt(open.Count - 1);
            return index;
        }

        int Index(Vector2Int t) => t.y * map.Width + t.x;
        Vector2Int Tile(int index) => new Vector2Int(index % map.Width, index / map.Width);
    }
}
