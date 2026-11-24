using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AStarDijkstra : MonoBehaviour
{
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private float cellHeight = 1f;
    [SerializeField] private float cellWidth = 1f;
    [SerializeField] private float stepDelay = 0.5f;

    public Transform predator;
    public Transform prey;
    public GameObject tilePrefab;
    public TMP_Dropdown algorithmDropdown;

    private Dictionary<Vector2, Cell> cells;
    private Dictionary<Vector2, GameObject> tileObjects;

    private List<Vector2> cellsToSearch;
    private List<Vector2> searchedCells;
    private List<Vector2> finalPath;

    private float stepTimer = 0f;
    private bool isRunning = false;

    private enum Algorithm { AStar, Dijkstra }
    private Algorithm selectedAlgorithm = Algorithm.AStar;

    void Start()
    {
        predator.position = new Vector3(0, 0, 0);
        prey.position = new Vector3(gridWidth / 2f, gridHeight / 2f, 0);
        GenerateGrid();

        algorithmDropdown.ClearOptions();
        algorithmDropdown.AddOptions(new List<string> { "A*", "Dijkstra" });
        algorithmDropdown.onValueChanged.AddListener(OnDropdownChanged);
        algorithmDropdown.interactable = true;
    }

    void Update()
    {
        if (!isRunning && Input.GetKeyDown(KeyCode.Space))
        {
            isRunning = true;
            algorithmDropdown.interactable = false;
        }

        if (isRunning)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepDelay)
            {
                stepTimer = 0f;
                Step();
            }
        }
    }

    void OnDropdownChanged(int index)
    {
        selectedAlgorithm = (index == 0) ? Algorithm.AStar : Algorithm.Dijkstra;
    }

    void Step()
    {
        Vector2 predatorPos = new Vector2(
            Mathf.Clamp(Mathf.RoundToInt(predator.position.x), 0, gridWidth - 1),
            Mathf.Clamp(Mathf.RoundToInt(predator.position.y), 0, gridHeight - 1)
        );

        Vector2 preyPos = new Vector2(
            Mathf.Clamp(Mathf.RoundToInt(prey.position.x), 0, gridWidth - 1),
            Mathf.Clamp(Mathf.RoundToInt(prey.position.y), 0, gridHeight - 1)
        );

    if (predatorPos == preyPos)
        {
            isRunning = false;
            Debug.Log("Prey caught!");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
            return;
        }

        Vector2 escapeTarget = GetEscapeTarget(preyPos, predatorPos);
        findPath(preyPos, escapeTarget);
        List<Vector2> preyPath = new List<Vector2>(finalPath);

        findPath(predatorPos, preyPos);
        List<Vector2> predatorPath = new List<Vector2>(finalPath);

        if (preyPath != null && preyPath.Count > 0)
        {
            Vector2 next = preyPath[0];
            prey.position = new Vector3(next.x, next.y, 0);
        }

        if (predatorPath != null && predatorPath.Count > 0)
        {
            Vector2 next = predatorPath[0];
            predator.position = new Vector3(next.x, next.y, 0);
        }

        foreach (var kvp in tileObjects)
        {
            if (!cells[kvp.Key].isWall)
                kvp.Value.GetComponent<SpriteRenderer>().color = Color.white;
        }

        foreach (Vector2 p in preyPath)
            tileObjects[p].GetComponent<SpriteRenderer>().color = Color.green;

        foreach (Vector2 p in predatorPath)
            tileObjects[p].GetComponent<SpriteRenderer>().color = Color.magenta;
    }

    Vector2 GetEscapeTarget(Vector2 preyPos, Vector2 predatorPos)
    {
        Vector2[] candidates = new Vector2[]
        {
            new Vector2(preyPos.x + 3, preyPos.y),
            new Vector2(preyPos.x - 3, preyPos.y),
            new Vector2(preyPos.x, preyPos.y + 3),
            new Vector2(preyPos.x, preyPos.y - 3),
        };

        Vector2 bestTarget = preyPos;
        float bestDist = -1f;

        foreach (Vector2 candidate in candidates)
        {
            Vector2 clamped = new Vector2(
                Mathf.Clamp(candidate.x, 0, gridWidth - 1),
                Mathf.Clamp(candidate.y, 0, gridHeight - 1)
            );

            if (cells.TryGetValue(clamped, out Cell c) && c.isWall)
                continue;

            float dist = Vector2.Distance(clamped, predatorPos);
            if (dist > bestDist)
            {
                bestDist = dist;
                bestTarget = clamped;
            }
        }

        return bestTarget;
    }

    private void GenerateGrid()
    {
        cells = new Dictionary<Vector2, Cell>();
        tileObjects = new Dictionary<Vector2, GameObject>();

        for (float x = 0; x < gridWidth; x += cellWidth)
        {
            for (float y = 0; y < gridHeight; y += cellHeight)
            {
                Vector2 pos = new Vector2(x, y);
                cells.Add(pos, new Cell(pos));
                GameObject tile = Instantiate(tilePrefab, new Vector3(x, y, -1), Quaternion.identity);
                tileObjects.Add(pos, tile);
            }
        }

        Vector2 predatorPos = new Vector2(
            Mathf.RoundToInt(predator.position.x),
            Mathf.RoundToInt(predator.position.y)
        );

        Vector2 preyPos = new Vector2(
            Mathf.RoundToInt(prey.position.x),
            Mathf.RoundToInt(prey.position.y)
        );

        for (int i = 0; i < 15; i++)
        {
            Vector2 pos = new Vector2(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
            if (pos == predatorPos || pos == preyPos) continue;
            cells[pos].isWall = true;
            tileObjects[pos].GetComponent<SpriteRenderer>().color = Color.black;
        }
    }

    private void findPath(Vector2 startPos, Vector2 endPos)
    {
        foreach (var cell in cells.Values)
        {
            cell.gCost = int.MaxValue;
            cell.hCost = int.MaxValue;
            cell.fcost = int.MaxValue;
            cell.connection = Vector2.zero;
        }

        searchedCells = new List<Vector2>();
        cellsToSearch = new List<Vector2> { startPos };
        finalPath = new List<Vector2>();

        if (!cells.TryGetValue(startPos, out Cell startCell)) return;

        startCell.gCost = 0;
        startCell.hCost = GetDistance(startPos, endPos);

        // A* uses gCost + hCost, Dijkstra uses gCost only
        startCell.fcost = (selectedAlgorithm == Algorithm.AStar)
            ? startCell.gCost + startCell.hCost
            : startCell.gCost;

        while (cellsToSearch.Count > 0)
        {
            Vector2 currentPos = cellsToSearch[0];

            foreach (Vector2 pos in cellsToSearch)
            {
                Cell c = cells[pos];
                if (c.fcost < cells[currentPos].fcost ||
                   (c.fcost == cells[currentPos].fcost && c.hCost < cells[currentPos].hCost))
                {
                    currentPos = pos;
                }
            }

            cellsToSearch.Remove(currentPos);
            searchedCells.Add(currentPos);

            if (currentPos == endPos)
            {
                Cell pathCell = cells[endPos];
                while (pathCell.position != startPos)
                {
                    finalPath.Add(pathCell.position);
                    pathCell = cells[pathCell.connection];
                }
                finalPath.Reverse();
                return;
            }

            SearchNeighbours(currentPos, endPos);
        }
    }

    private void SearchNeighbours(Vector2 cellPos, Vector2 endPos)
    {
        for (float x = cellPos.x - cellWidth; x <= cellWidth + cellPos.x; x += cellWidth)
        {
            for (float y = cellPos.y - cellHeight; y <= cellHeight + cellPos.y; y += cellHeight)
            {
                Vector2 neighborPos = new Vector2(x, y);

                if (neighborPos.x < 0 || neighborPos.x >= gridWidth ||
                    neighborPos.y < 0 || neighborPos.y >= gridHeight) continue;

                if (neighborPos == cellPos) continue;

                if (!cells.TryGetValue(neighborPos, out Cell neighbor)) continue;

                if (neighbor.isWall || searchedCells.Contains(neighborPos)) continue;

                if (!cells.TryGetValue(cellPos, out Cell current)) continue;

                int newGCost = current.gCost + GetDistance(cellPos, neighborPos);

                if (newGCost < neighbor.gCost)
                {
                    neighbor.connection = cellPos;
                    neighbor.gCost = newGCost;
                    neighbor.hCost = GetDistance(neighborPos, endPos);

                    // A* = gCost + hCost  |  Dijkstra = gCost only
                    neighbor.fcost = (selectedAlgorithm == Algorithm.AStar)
                        ? neighbor.gCost + neighbor.hCost
                        : neighbor.gCost;

                    if (!cellsToSearch.Contains(neighborPos))
                        cellsToSearch.Add(neighborPos);
                }
            }
        }
    }

    private int GetDistance(Vector2 a, Vector2 b)
    {
        Vector2Int dist = new Vector2Int(
            Mathf.Abs((int)a.x - (int)b.x),
            Mathf.Abs((int)a.y - (int)b.y));

        int lowest = Mathf.Min(dist.x, dist.y);
        int highest = Mathf.Max(dist.x, dist.y);
        return lowest * 14 + (highest - lowest) * 10;
    }

    private class Cell
    {
        public Vector2 position;
        public int fcost = int.MaxValue;
        public int gCost = int.MaxValue;
        public int hCost = int.MaxValue;
        public Vector2 connection;
        public bool isWall;

        public Cell(Vector2 pos) { position = pos; }
    }
}