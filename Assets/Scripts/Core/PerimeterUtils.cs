using System.Collections.Generic;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>손이 다니는 길: 키보드마다 판 바깥 MARGIN칸 둘레 사각형. 웹 Perimeter를 칸 단위로 옮겼다.</summary>
    public static class PerimeterUtils
    {
        public struct PerimeterRect
        {
            public float X0;
            public float Y0;
            public float X1;
            public float Y1;

            public float Width => X1 - X0;
            public float Height => Y1 - Y0;
            public float Length => 2f * (Width + Height);
        }

        public struct PerimeterPath
        {
            public List<Vector2> Points;
            public float Length;
        }

        private const float LOCATE_EPSILON = 0.03f;
        private const float CORNER_EPSILON = 0.0002f;

        public static PerimeterRect GetRect(int board)
        {
            float x = GeometryUtils.BoardOffsetX(board);
            return new PerimeterRect
            {
                X0 = x - GeometryUtils.MARGIN,
                Y0 = -GeometryUtils.MARGIN,
                X1 = x + GeometryUtils.COLS + GeometryUtils.MARGIN,
                Y1 = GeometryUtils.ROWS + GeometryUtils.MARGIN,
            };
        }

        /// <summary>점 p를 둘레에 붙였을 때 시계 방향 거리(왼쪽 위 모서리 = 0)</summary>
        public static float ParamOf(PerimeterRect rect, Vector2 p)
        {
            float x = Mathf.Clamp(p.x, rect.X0, rect.X1);
            float y = Mathf.Clamp(p.y, rect.Y0, rect.Y1);
            float[] dist = { Mathf.Abs(y - rect.Y0), Mathf.Abs(x - rect.X1), Mathf.Abs(y - rect.Y1), Mathf.Abs(x - rect.X0) };
            int edge = 0;
            for (int i = 1; i < 4; i++)
            {
                if (dist[i] < dist[edge])
                {
                    edge = i;
                }
            }

            switch (edge)
            {
                case 0:
                    return x - rect.X0;
                case 1:
                    return rect.Width + (y - rect.Y0);
                case 2:
                    return rect.Width + rect.Height + (rect.X1 - x);
                default:
                    return 2f * rect.Width + rect.Height + (rect.Y1 - y);
            }
        }

        public static Vector2 PointAt(PerimeterRect rect, float t)
        {
            float w = rect.Width;
            float h = rect.Height;
            t = Mathf.Repeat(t, rect.Length);
            if (t <= w)
            {
                return new Vector2(rect.X0 + t, rect.Y0);
            }

            if (t <= w + h)
            {
                return new Vector2(rect.X1, rect.Y0 + (t - w));
            }

            if (t <= 2f * w + h)
            {
                return new Vector2(rect.X1 - (t - w - h), rect.Y1);
            }

            return new Vector2(rect.X0, rect.Y1 - (t - 2f * w - h));
        }

        /// <summary>둘레 위 tA → tB, 짧은 쪽 방향으로 모서리를 거쳐 가는 경로</summary>
        public static PerimeterPath OnRect(PerimeterRect rect, float tA, float tB)
        {
            float w = rect.Width;
            float h = rect.Height;
            float perimeter = rect.Length;
            float[] corners = { 0f, w, w + h, 2f * w + h };
            float clockwise = Mathf.Repeat(tB - tA, perimeter);
            var deltas = new List<float>();
            bool isClockwise = clockwise <= perimeter * 0.5f;
            foreach (float corner in corners)
            {
                float d = isClockwise ? Mathf.Repeat(corner - tA, perimeter) : Mathf.Repeat(tA - corner, perimeter);
                float limit = isClockwise ? clockwise : perimeter - clockwise;
                if (d > CORNER_EPSILON && d < limit)
                {
                    deltas.Add(d);
                }
            }

            deltas.Sort();
            var points = new List<Vector2>();
            foreach (float d in deltas)
            {
                points.Add(PointAt(rect, isClockwise ? tA + d : tA - d));
            }

            points.Add(PointAt(rect, tB));
            return new PerimeterPath { Points = points, Length = Mathf.Min(clockwise, perimeter - clockwise) };
        }

        public static Vector2 Nearest(int board, Vector2 p)
        {
            PerimeterRect rect = GetRect(board);
            return PointAt(rect, ParamOf(rect, p));
        }

        /// <summary>p가 어느 판 둘레 위에 있는가(없으면 -1)</summary>
        public static int Locate(Vector2 p)
        {
            for (int board = 0; board < 2; board++)
            {
                if (Vector2.Distance(Nearest(board, p), p) < LOCATE_EPSILON)
                {
                    return board;
                }
            }

            return -1;
        }

        /// <summary>from(둘레 위, 두 키보드 사이, 또는 판 위) → to(boardB 둘레 위) 경유점 목록</summary>
        public static PerimeterPath FindPath(Vector2 from, int boardB, Vector2 to)
        {
            int boardA = Locate(from);
            PerimeterRect rectB = GetRect(boardB);
            if (boardA == -1)
            {
                PerimeterRect rect0 = GetRect(0);
                PerimeterRect rect1 = GetRect(1);
                bool isInGap = from.x > rect0.X1 && from.x < rect1.X0;
                int via = from.x <= rect0.X1 ? 0 : 1;   // 판 위에 있으면 먼저 그 판의 가장 가까운 둘레로 나간다
                Vector2 q = isInGap
                    ? new Vector2(boardB == 0 ? rectB.X1 : rectB.X0, Mathf.Abs(from.y - rectB.Y0) < Mathf.Abs(from.y - rectB.Y1) ? rectB.Y0 : rectB.Y1)
                    : Nearest(via, from);
                PerimeterPath rest = FindPath(q, boardB, to);
                var points = new List<Vector2> { q };
                points.AddRange(rest.Points);
                return new PerimeterPath { Points = points, Length = Vector2.Distance(q, from) + rest.Length };
            }

            PerimeterRect rectA = GetRect(boardA);
            if (boardA == boardB)
            {
                return OnRect(rectA, ParamOf(rectA, from), ParamOf(rectB, to));
            }

            PerimeterPath best = default;
            bool hasBest = false;
            foreach (bool isTop in new[] { true, false })
            {
                var exit = new Vector2(boardA == 0 ? rectA.X1 : rectA.X0, isTop ? rectA.Y0 : rectA.Y1);
                var entry = new Vector2(boardB == 0 ? rectB.X1 : rectB.X0, exit.y);
                PerimeterPath first = OnRect(rectA, ParamOf(rectA, from), ParamOf(rectA, exit));
                PerimeterPath second = OnRect(rectB, ParamOf(rectB, entry), ParamOf(rectB, to));
                float length = first.Length + Mathf.Abs(entry.x - exit.x) + second.Length;
                if (!hasBest || length < best.Length)
                {
                    var points = new List<Vector2>(first.Points) { entry };
                    points.AddRange(second.Points);
                    best = new PerimeterPath { Points = points, Length = length };
                    hasBest = true;
                }
            }

            return best;
        }

        /// <summary>줄(행·열) 한쪽 끝 바깥의 둘레 위 점. end 0 = 왼쪽/위, 1 = 오른쪽/아래.</summary>
        public static Vector2 LineAnchor(int board, LineAxis axis, int index, int end)
        {
            float x = GeometryUtils.BoardOffsetX(board);
            if (axis == LineAxis.Row)
            {
                return new Vector2(end == 0 ? x - GeometryUtils.MARGIN : x + GeometryUtils.COLS + GeometryUtils.MARGIN, index + 0.5f);
            }

            return new Vector2(x + index + 0.5f, end == 0 ? -GeometryUtils.MARGIN : GeometryUtils.ROWS + GeometryUtils.MARGIN);
        }

        /// <summary>from에서 target 방향으로 쏜 직선이 판 둘레 사각형을 빠져나가는 점</summary>
        public static Vector2 RayExit(int board, Vector2 from, Vector2 target)
        {
            PerimeterRect rect = GetRect(board);
            Vector2 delta = target - from;
            float length = delta.magnitude;
            if (length < 0.0001f)
            {
                length = 1f;
            }

            Vector2 unit = delta / length;
            float tx = unit.x > 0f ? (rect.X1 - from.x) / unit.x : unit.x < 0f ? (rect.X0 - from.x) / unit.x : float.PositiveInfinity;
            float ty = unit.y > 0f ? (rect.Y1 - from.y) / unit.y : unit.y < 0f ? (rect.Y0 - from.y) / unit.y : float.PositiveInfinity;
            float t = Mathf.Max(length, Mathf.Min(tx, ty));
            Vector2 exit = from + unit * t;
            return new Vector2(Mathf.Clamp(exit.x, rect.X0, rect.X1), Mathf.Clamp(exit.y, rect.Y0, rect.Y1));
        }
    }
}
