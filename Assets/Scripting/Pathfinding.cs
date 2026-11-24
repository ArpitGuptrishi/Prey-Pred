
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private float cellHeight = 1f;
    [SerializeField] private float cellWidth =1f;
    [SerializeField] private bool generatePath;
    [SerializeField] private bool visualiseGrid;

    private bool pathGenerated;

    private Dictionary<Vector2,Cell> cells;
    [SerializeField] private List<Vector2> cellsToSearch;
    [SerializeField] private List<Vector2> searchedCells;
    [SerializeField] private List<Vector2> finalPath;

    private void Update()
    {
        if(generatePath && !pathGenerated)
        {
            GenerateGrid();
            findPath(new Vector2(0,1),new Vector2(5,7));
            pathGenerated = true;

        }
        else if(!generatePath)
        {
            pathGenerated = false;
        }
    }

    private void GenerateGrid()
    {
        cells = new Dictionary<Vector2, Cell>();
        for(float x= 0;x< gridWidth;x+=cellWidth)
        {
            for(float y=0; y<gridHeight; y+=cellHeight)
            {
               Vector2 pos = new Vector2(x,y);
               cells.Add(pos, new Cell(pos)); 
            }
        }
        for(int i =0; i<40; i++)
        {
            Vector2 pos = new Vector2(Random.Range(0,gridWidth),Random.Range(0,gridHeight));
            cells[pos].isWall = true;
        }
    }
    private void findPath(Vector2 startPos, Vector2 endPos)
    {
      searchedCells = new List<Vector2>();
      cellsToSearch = new List<Vector2>{startPos};
      finalPath = new List<Vector2>(); 
      
      Cell startCell = cells[startPos];
      startCell.gCost=0;
      startCell.hCost= GetDistance(startPos,endPos);
      startCell.fcost = GetDistance(startPos,endPos);

      while(cellsToSearch.Count>0)
        {
            Vector2 cellToSearch = cellsToSearch[0];
            foreach (Vector2 pos in cellsToSearch)
            {
                Cell c = cells[pos];
                if(c.fcost < cells[cellToSearch].fcost || c.fcost == cells[cellToSearch].fcost && c.hCost ==cells[cellToSearch].hCost)
                {
                    cellToSearch = pos;
                }
            }
            cellsToSearch.Remove(cellToSearch);
            searchedCells.Add(cellToSearch);

            if (cellToSearch == endPos)
            {
                Cell pathCell = cells[endPos];
                while(pathCell.position != startPos)
                {
                    finalPath.Add(pathCell.position);
                    pathCell = cells[pathCell.connection];
                }
            }
            SearchCellNeighbours(cellToSearch,endPos);
        }
    }
    private void SearchCellNeighbours(Vector2 cellPos, Vector2 endPos)
    {
      for(float x =cellPos.x-cellWidth; x<=cellWidth + cellPos.x; x += cellWidth)
        {
            for(float y =cellPos.y-cellHeight; y<=cellHeight + cellPos.y; y += cellHeight)
            {
                Vector2 neighborPos = new Vector2(x,y);
                if(cells.TryGetValue(neighborPos,out Cell c) && !searchedCells.Contains(neighborPos) && !cells[neighborPos].isWall)
                {
                    int GcostToNeighbour = cells[cellPos].gCost + GetDistance(cellPos,neighborPos);
                    if(GcostToNeighbour < cells[neighborPos].gCost)
                    {
                        Cell neighbourNode = cells[neighborPos];

                        neighbourNode.connection = cellPos;
                        neighbourNode.gCost=GcostToNeighbour;
                        neighbourNode.hCost=GetDistance(neighborPos,endPos);
                        neighbourNode.fcost=neighbourNode.gCost +neighbourNode.hCost;

                        if (!cellsToSearch.Contains(neighborPos))
                        {
                            cellsToSearch.Add(neighborPos);
                        }
                    }
                }
            }
        }  
    }

    private int GetDistance(Vector2 pos1,Vector2 pos2)
    {
        Vector2Int dist = new Vector2Int(Mathf.Abs((int)pos1.x - (int)pos2.x),Mathf.Abs((int)pos1.y - (int)pos2.y));
        int lowest = Mathf.Min(dist.x,dist.y);
        int highest = Mathf.Max(dist.x,dist.y);
        int horizontalMovesRequired = highest-lowest;
        return lowest * 14 +horizontalMovesRequired * 10;
    }

    private void OnDrawGizmos()
    {
      if(!visualiseGrid || cells ==null)
        {
            return;
        }  
        foreach(KeyValuePair<Vector2,Cell> kvp in cells)
        {
            if (!kvp.Value.isWall)
            {
                Gizmos.color = Color.white;
            }
            else
            {
                Gizmos.color = Color.black;
            }
            if (finalPath.Contains(kvp.Key))
            {
                Gizmos.color = Color.magenta;
            }
            Gizmos.DrawCube(kvp.Key+ (Vector2)transform.position,new Vector3(cellWidth,cellHeight));
        }
    }

    private class Cell
    {
        public Vector2 position;
        public int fcost=int.MaxValue;
        public int gCost=int.MaxValue;
        public int hCost=int.MaxValue;
        public Vector2 connection;
        public bool isWall;

        public Cell(Vector2 pos)
        {
            position=pos;
        }
    }
}
