using UnityEngine;
using System.Collections.Generic;

public class DijkstraPathfinder : MonoBehaviour
{
    private GridGenerator gridGenerator;
    private List<Node> allNodes;
    private bool graphBuilt = false;

    void Awake()
    {
        gridGenerator = FindObjectOfType<GridGenerator>();
        BuildGraph();
    }

    private void BuildGraph()
    {
        if (gridGenerator == null) return;

        allNodes = new List<Node>();
        Debug.Log("Construyendo grafo con " + gridGenerator.GetChainCount() + " tiles");

        // Paso 1: Crear nodos para todas las celdas de path
        for (int tileIdx = 0; tileIdx < gridGenerator.GetChainCount(); tileIdx++)
        {
            var tile = gridGenerator.GetPlacedTile(tileIdx);
            if (tile.layout == null) continue;

            for (int x = 0; x < tile.layout.gridWidth; x++)
            {
                for (int y = 0; y < tile.layout.gridHeight; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (tile.layout.IsPath(cell))
                    {
                        Vector3 worldPos = GetWorldPosition(tile, cell);
                        Node node = new Node(worldPos, cell, tileIdx);
                        allNodes.Add(node);
                    }
                }
            }
        }

        Debug.Log("Creados " + allNodes.Count + " nodos");

        // Paso 2: Crear conexiones entre nodos adyacentes
        CreateConnections();

        graphBuilt = true;
        Debug.Log("Grafo construido exitosamente");
    }

    private Vector3 GetWorldPosition(PlacedTile tile, Vector2Int cell)
    {
        return tile.worldOrigin +
            TileOrientationCalculator.CellToWorldLocal(cell, tile.layout, tile.rotSteps, tile.flipped);
    }

    private void CreateConnections()
    {
        foreach (Node node in allNodes)
        {
            var tile = gridGenerator.GetPlacedTile(node.tileIndex);
            if (tile.layout == null) continue;

            // Conectar con vecinos en las 4 direcciones
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = node.gridPosition + dir;

                // Buscar nodo vecino en la misma posición relativa
                Node neighbor = FindNode(node.tileIndex, neighborPos);
                if (neighbor != null && neighbor.isWalkable)
                {
                    node.edges.Add(new Edge(neighbor, 1f));
                }
            }
        }
    }

    private Node FindNode(int tileIndex, Vector2Int gridPos)
    {
        return allNodes.Find(n => n.tileIndex == tileIndex && n.gridPosition == gridPos);
    }

    public Vector3[] FindPath(Vector3 startPos, Vector3 endPos)
    {
        if (!graphBuilt || allNodes == null || allNodes.Count == 0)
        {
            Debug.LogWarning("Grafo no construido, usando camino directo");
            return new Vector3[] { startPos, endPos };
        }

        // Encontrar nodos más cercanos
        Node startNode = FindClosestNode(startPos);
        Node endNode = FindClosestNode(endPos);

        if (startNode == null || endNode == null)
        {
            Debug.LogWarning("No se pudieron encontrar nodos inicio/fin");
            return new Vector3[] { startPos, endPos };
        }

        Debug.Log($"Buscando camino de {startNode.gridPosition} a {endNode.gridPosition}");

        // Ejecutar Dijkstra
        List<Node> pathNodes = DijkstraAlgorithm(startNode, endNode);

        if (pathNodes == null || pathNodes.Count == 0)
        {
            Debug.LogWarning("Dijkstra no encontro camino");
            return new Vector3[] { startPos, endPos };
        }

        // Convertir nodos a posiciones mundiales
        Vector3[] path = new Vector3[pathNodes.Count];
        for (int i = 0; i < pathNodes.Count; i++)
        {
            path[i] = pathNodes[i].worldPosition;
        }

        Debug.Log("Dijkstra encontro camino con " + path.Length + " puntos");
        return path;
    }

    private List<Node> DijkstraAlgorithm(Node start, Node goal)
    {
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        // Resetear nodos
        foreach (Node node in allNodes)
        {
            node.gCost = float.MaxValue;
            node.hCost = Vector3.Distance(node.worldPosition, goal.worldPosition);
            node.parent = null;
        }

        start.gCost = 0;
        openSet.Add(start);

        while (openSet.Count > 0)
        {
            // Encontrar nodo con menor gCost
            Node current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].gCost < current.gCost)
                {
                    current = openSet[i];
                }
            }

            openSet.Remove(current);
            closedSet.Add(current);

            // Si llegamos al objetivo
            if (current == goal)
            {
                return RetracePath(start, goal);
            }

            // Examinar vecinos
            foreach (Edge edge in current.edges)
            {
                Node neighbor = edge.targetNode;

                if (closedSet.Contains(neighbor)) continue;

                float newGCost = current.gCost + edge.cost;

                if (newGCost < neighbor.gCost)
                {
                    neighbor.gCost = newGCost;
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null; // No se encontro camino
    }

    private List<Node> RetracePath(Node start, Node end)
    {
        List<Node> path = new List<Node>();
        Node current = end;

        while (current != start && current != null)
        {
            path.Add(current);
            current = current.parent;
        }

        path.Add(start);
        path.Reverse();

        return path;
    }

    private Node FindClosestNode(Vector3 worldPos)
    {
        Node closest = null;
        float minDistance = float.MaxValue;

        foreach (Node node in allNodes)
        {
            float distance = Vector3.Distance(worldPos, node.worldPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = node;
            }
        }

        return closest;
    }

    [ContextMenu("Debug Graph")]
    public void DebugGraph()
    {
        if (!graphBuilt)
        {
            Debug.Log("Grafo no construido");
            return;
        }

        Debug.Log($"=== DEBUG GRAFO ===");
        Debug.Log($"Total nodos: {allNodes.Count}");

        foreach (Node node in allNodes)
        {
            Debug.Log($"Nodo: Tile{node.tileIndex} Pos({node.gridPosition.x},{node.gridPosition.y}) " +
                     $"Edges: {node.edges.Count} World: {node.worldPosition}");
        }
    }
}