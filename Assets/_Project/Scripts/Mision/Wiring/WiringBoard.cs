using System.Collections.Generic;
using UnityEngine;

// Rules of the wiring board (task 43), without any UI. Signed: claude_c.
// Cells are (x = column, y = row), row 0 at the top. A cable is the list of cells from one socket of its color;
// it is connected when it ends on the other socket of that color. The board is SOLVED when every color is
// connected (empty squares are fine).
//   Begin(cell)  press on a socket (erases that cable and starts it again there) or on a cable (cuts it there and goes on).
//   DragTo(cell) moves the cable end toward the cell, one cell at a time (up, down, left or right).
//                Dragging back over your own cable shortens it. Walls, other sockets and other cables block it
//                (cables never cross).
//   End()        releases the mouse (the cable stays on the board).
//   Erase(color) / EraseAt(cell) remove a cable.
public class WiringBoard
{
    public const char Wall = '#';
    public const char Free = '.';
    public const char None = '\0';

    public int Width { get; private set; }
    public int Height { get; private set; }
    public char Active { get; private set; }            // color being drawn, None = not drawing
    public Vector2Int LastBlocked { get; private set; } // last cell that blocked a cable (for the red flash)
    public readonly List<char> Colors = new List<char>(); // in order of first appearance

    char[,] cells;
    readonly Dictionary<char, Vector2Int[]> sockets = new Dictionary<char, Vector2Int[]>();
    readonly Dictionary<char, List<Vector2Int>> paths = new Dictionary<char, List<Vector2Int>>();

    // Builds a board from a text grid. Spaces are ignored; blank lines are skipped.
    // Returns null and an error if the grid is not a valid level.
    public static WiringBoard Parse(string grid, out string error)
    {
        error = "";
        if (string.IsNullOrEmpty(grid))
        {
            error = "empty grid";
            return null;
        }
        List<string> rows = new List<string>();
        foreach (string raw in grid.Replace("\r", "").Split('\n'))
        {
            string row = raw.Replace(" ", "").Replace("\t", "");
            if (row.Length > 0) rows.Add(row);
        }
        if (rows.Count == 0)
        {
            error = "empty grid";
            return null;
        }

        WiringBoard b = new WiringBoard();
        b.Height = rows.Count;
        b.Width = rows[0].Length;
        b.cells = new char[b.Width, b.Height];
        Dictionary<char, List<Vector2Int>> found = new Dictionary<char, List<Vector2Int>>();
        for (int y = 0; y < b.Height; y++)
        {
            if (rows[y].Length != b.Width)
            {
                error = "row " + (y + 1) + " has " + rows[y].Length + " cells, expected " + b.Width;
                return null;
            }
            for (int x = 0; x < b.Width; x++)
            {
                char c = rows[y][x];
                if (c != Wall && c != Free && !(c >= 'A' && c <= 'Z'))
                {
                    error = "unknown cell '" + c + "' at row " + (y + 1) + ", column " + (x + 1);
                    return null;
                }
                b.cells[x, y] = c;
                if (c >= 'A' && c <= 'Z')
                {
                    if (!found.ContainsKey(c))
                    {
                        found[c] = new List<Vector2Int>();
                        b.Colors.Add(c);
                    }
                    found[c].Add(new Vector2Int(x, y));
                }
            }
        }
        foreach (KeyValuePair<char, List<Vector2Int>> pair in found)
        {
            if (pair.Value.Count != 2)
            {
                error = "color " + pair.Key + " has " + pair.Value.Count + " sockets, expected 2";
                return null;
            }
            b.sockets[pair.Key] = pair.Value.ToArray();
            b.paths[pair.Key] = new List<Vector2Int>();
        }
        if (b.Colors.Count == 0)
        {
            error = "no sockets";
            return null;
        }
        b.LastBlocked = new Vector2Int(-1, -1);
        return b;
    }

    // ---------- Queries ----------

    public int PairCount { get { return Colors.Count; } }

    public bool InBounds(Vector2Int c)
    {
        return c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;
    }

    public bool IsWall(Vector2Int c)
    {
        return InBounds(c) && cells[c.x, c.y] == Wall;
    }

    // Color of the socket in that cell, None if there is none
    public char SocketAt(Vector2Int c)
    {
        if (!InBounds(c)) return None;
        char ch = cells[c.x, c.y];
        return ch >= 'A' && ch <= 'Z' ? ch : None;
    }

    // Color of the cable that goes through that cell, None if there is none
    public char CableAt(Vector2Int c)
    {
        foreach (KeyValuePair<char, List<Vector2Int>> pair in paths)
            if (pair.Value.Contains(c)) return pair.Key;
        return None;
    }

    public IList<Vector2Int> PathOf(char color)
    {
        List<Vector2Int> p;
        return paths.TryGetValue(color, out p) ? p.AsReadOnly() : new List<Vector2Int>().AsReadOnly();
    }

    public Vector2Int[] SocketsOf(char color)
    {
        Vector2Int[] s;
        return sockets.TryGetValue(color, out s) ? s : new Vector2Int[0];
    }

    public bool IsConnected(char color)
    {
        List<Vector2Int> p;
        if (!paths.TryGetValue(color, out p) || p.Count < 2) return false;
        Vector2Int[] s = sockets[color];
        Vector2Int a = p[0];
        Vector2Int b = p[p.Count - 1];
        return (a == s[0] && b == s[1]) || (a == s[1] && b == s[0]);
    }

    public int ConnectedCount
    {
        get
        {
            int n = 0;
            foreach (char c in Colors) if (IsConnected(c)) n++;
            return n;
        }
    }

    // Every pair connected (empty squares are fine)
    public bool Solved { get { return ConnectedCount == PairCount; } }

    // ---------- Drawing ----------

    // Press on a cell. True if a cable is now being drawn.
    public bool Begin(Vector2Int cell)
    {
        Active = None;
        char socket = SocketAt(cell);
        if (socket != None)
        {
            List<Vector2Int> p = paths[socket];
            p.Clear();
            p.Add(cell);
            Active = socket;
            return true;
        }
        char cable = CableAt(cell);
        if (cable != None)
        {
            List<Vector2Int> p = paths[cable];
            int i = p.IndexOf(cell);
            p.RemoveRange(i + 1, p.Count - i - 1);
            Active = cable;
            return true;
        }
        return false;
    }

    // Moves the end of the active cable toward a cell. Returns true if it was blocked on the way (LastBlocked says where).
    public bool DragTo(Vector2Int target)
    {
        if (Active == None) return false;
        List<Vector2Int> p = paths[Active];
        for (int guard = 0; guard < Width * Height * 2; guard++)
        {
            Vector2Int last = p[p.Count - 1];
            if (last == target) return false;

            Vector2Int d = target - last;
            Vector2Int main = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? new Vector2Int(System.Math.Sign(d.x), 0) : new Vector2Int(0, System.Math.Sign(d.y));
            Vector2Int side = main.x != 0 ? new Vector2Int(0, System.Math.Sign(d.y)) : new Vector2Int(System.Math.Sign(d.x), 0);

            int step = Step(p, last + main);
            if (step == StepBlocked && side != Vector2Int.zero) step = Step(p, last + side); // try the other way round a corner
            if (step == StepBlocked) return true;
            if (step == StepStop) return false;
        }
        return false;
    }

    public void End()
    {
        Active = None;
    }

    public void Erase(char color)
    {
        List<Vector2Int> p;
        if (paths.TryGetValue(color, out p)) p.Clear();
        if (Active == color) Active = None;
    }

    // Erases the cable on that cell, or the cable of the socket on that cell. True if something was erased.
    public bool EraseAt(Vector2Int cell)
    {
        char color = CableAt(cell);
        if (color == None) color = SocketAt(cell);
        if (color == None || paths[color].Count == 0) return false;
        Erase(color);
        return true;
    }

    public void Clear()
    {
        foreach (List<Vector2Int> p in paths.Values) p.Clear();
        Active = None;
    }

    const int StepOk = 0;
    const int StepBlocked = 1;
    const int StepStop = 2;

    int Step(List<Vector2Int> p, Vector2Int next)
    {
        // Back over your own cable: shorten it
        int i = p.IndexOf(next);
        if (i >= 0)
        {
            p.RemoveRange(i + 1, p.Count - i - 1);
            return StepOk;
        }
        if (IsConnected(Active)) return StepStop; // complete: it only gets shorter

        if (!InBounds(next) || IsWall(next)) return Blocked(next);
        char socket = SocketAt(next);
        if (socket != None && socket != Active) return Blocked(next);
        if (socket == None && CableAt(next) != None) return Blocked(next); // another color's cable: no crossing

        p.Add(next); // free cell, or the other socket of this color (now connected)
        return StepOk;
    }

    int Blocked(Vector2Int cell)
    {
        LastBlocked = cell;
        return StepBlocked;
    }

    // ---------- Helpers ----------

    // Number of ways to connect every pair at once (capped). Used by the tests to prove a board can be solved.
    public int CountSolutions(int cap)
    {
        bool[,] used = new bool[Width, Height];
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                used[x, y] = cells[x, y] != Free;
        int count = 0;
        Route(0, used, ref count, cap);
        return count;
    }

    void Route(int colorIndex, bool[,] used, ref int count, int cap)
    {
        if (count >= cap) return;
        if (colorIndex == Colors.Count)
        {
            count++;
            return;
        }
        Vector2Int[] s = sockets[Colors[colorIndex]];
        Search(s[0], s[1], colorIndex, used, ref count, cap);
    }

    void Search(Vector2Int at, Vector2Int goal, int colorIndex, bool[,] used, ref int count, int cap)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int d in dirs)
        {
            if (count >= cap) return;
            Vector2Int n = at + d;
            if (!InBounds(n)) continue;
            if (n == goal)
            {
                Route(colorIndex + 1, used, ref count, cap);
                continue;
            }
            if (used[n.x, n.y]) continue;
            used[n.x, n.y] = true;
            Search(n, goal, colorIndex, used, ref count, cap);
            used[n.x, n.y] = false;
        }
    }
}
