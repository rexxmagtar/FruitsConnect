# Graph Theory Concepts in Fruits Connect

## Overview
This document explains the mathematical foundations and graph theory concepts used in the Fruits Connect puzzle game.

---

## Core Graph Theory Problems

### 1. **Bipartite Matching Problem**

**Definition**: Given two distinct sets of vertices (producers and consumers), find a matching that pairs elements from one set to the other.

**In Fruits Connect**:
- **Set A**: Producer nodes (sources)
- **Set B**: Consumer nodes (sinks)
- **Goal**: Match each consumer to a producer through valid paths

**Classic Algorithms**:
- Hungarian Algorithm (O(n³))
- Hopcroft-Karp Algorithm (O(E√V))
- Hall's Marriage Theorem for perfect matchings

---

### 2. **Maximum Flow / Network Flow Problem**

**Definition**: Route maximum possible flow from source nodes to sink nodes through a network with capacity constraints.

**In Fruits Connect**:
- Producers generate "flow"
- Consumers consume "flow"
- Each node has capacity: `maxOutgoingConnections`
- Edges have implicit capacity of 1 (single connection)

**Classic Algorithms**:
- Ford-Fulkerson Method
- Edmonds-Karp Algorithm (O(VE²))
- Dinic's Algorithm (O(V²E))
- Push-Relabel Algorithm

**Key Concepts**:
- **Residual Graph**: Remaining capacity after flow
- **Augmenting Path**: Path with available capacity
- **Min-Cut Max-Flow Theorem**: Minimum cut equals maximum flow

---

### 3. **Steiner Tree Problem**

**Definition**: Connect a set of terminal vertices using minimum cost, optionally including intermediate (Steiner) vertices.

**In Fruits Connect**:
- **Terminal nodes**: Producers + Consumers (must be connected)
- **Steiner nodes**: Neutral nodes (optional intermediates)
- **Goal**: Connect all terminals using optimal Steiner nodes

**Complexity**: **NP-hard** - This is why puzzles can be genuinely challenging!

**Approximation Algorithms**:
- Minimum Spanning Tree based (2-approximation)
- Kou-Markowsky-Berman Algorithm
- Dreyfus-Wagner Algorithm (exact, exponential time)

---

### 4. **Graph Reachability**

**Definition**: Determine if there exists a path from vertex A to vertex B.

**In Fruits Connect**:
- Every consumer must be able to reach at least one producer
- Uses BFS (Breadth-First Search) or DFS (Depth-First Search)

**Algorithms**:
- BFS: O(V + E) - finds shortest path
- DFS: O(V + E) - explores all paths
- Strongly Connected Components (Tarjan's, Kosaraju's)

---

### 5. **Planarity & Path Crossing**

**Definition**: Can a graph be drawn in a plane without edge crossings?

**In Fruits Connect**:
- Paths cannot cross in 2D space
- Related to planar graph embedding
- Euler's formula: V - E + F = 2

**Testing**:
- Kuratowski's Theorem
- Wagner's Theorem
- Boyer-Myrvold Planarity Testing (O(V))

---

## Difficulty Metrics

### Graph Properties That Affect Difficulty

| Property | Easy | Medium | Hard | Expert |
|----------|------|--------|------|--------|
| **Graph Density** | 0.6-0.8 | 0.4-0.6 | 0.2-0.4 | 0.1-0.2 |
| **Avg Path Length** | 2-3 | 3-4 | 4-6 | 6+ |
| **Node Capacity** | 3-4 | 2-3 | 2 | 1-2 |
| **Alternative Paths** | Many | Several | Few | Minimal |
| **Bottleneck Nodes** | None | Few | Several | Many |

### 1. **Graph Density**

**Formula**: `D = |E| / |E_max|`

For directed graphs: `|E_max| = n(n-1)`

**Interpretation**:
- High density (0.7+): Many connection options → Easier
- Low density (0.2-): Few connections → Harder, more constrained

---

### 2. **Average Path Length**

**Formula**: `APL = (1/|P|) Σ shortest_path(consumer_i, nearest_producer)`

Where P is the set of all consumer nodes.

**Interpretation**:
- Short paths (2-3): Quick solutions → Easier
- Long paths (5+): Complex routing → Harder

---

### 3. **Graph Diameter**

**Definition**: Maximum shortest path between any two nodes.

**Interpretation**:
- Small diameter: Nodes well-connected → Easier
- Large diameter: Nodes far apart → Harder

---

### 4. **Betweenness Centrality**

**Formula** (for node v): 
```
BC(v) = Σ(σ_st(v) / σ_st)
```
Where σ_st is the number of shortest paths from s to t, and σ_st(v) is the number passing through v.

**Interpretation**:
- High betweenness nodes are **bottlenecks** (articulation points)
- Removing them disconnects the graph → Harder puzzles

---

### 5. **Edge Connectivity**

**Definition**: Minimum number of edges that must be removed to disconnect the graph.

**Interpretation**:
- High connectivity (3+): Multiple redundant paths → Easier
- Low connectivity (1): Single path, fragile → Harder
- Connectivity = 1: Contains **bridge edges** (critical)

---

### 6. **Chromatic Number**

**Definition**: Minimum colors needed to color vertices so no adjacent vertices share a color.

**Related Concepts**:
- Helps identify path conflicts
- NP-complete to compute exactly
- Approximation: Greedy coloring

---

## Complexity Score Formula

Our composite difficulty metric:

```
ComplexityScore = (APL × 2) + (10 / (AlternativePaths + 1)) + ((1 - Density) × 10)
```

**Components**:
1. **Path complexity**: Longer paths = harder
2. **Alternative penalty**: Fewer alternatives = harder
3. **Density penalty**: Sparser graph = harder

**Score Ranges**:
- Easy: 0-8
- Medium: 8-15
- Hard: 15-25
- Expert: 25+

---

## Advanced Concepts

### Articulation Points (Cut Vertices)

**Definition**: Vertices whose removal disconnects the graph.

**Detection**: Tarjan's Algorithm (O(V + E))

**In Puzzles**: Force players to use specific nodes → Increases difficulty

---

### Hamiltonian Path Problem

**Definition**: Path visiting every vertex exactly once.

**Complexity**: NP-complete

**Relation**: If solution requires visiting all neutral nodes → Very hard!

---

### Travelling Salesman Problem (TSP)

**Definition**: Find shortest route visiting all vertices and returning to start.

**Complexity**: NP-hard

**Relation**: Optimal connection ordering for minimal crossing

---

## Implementation Notes

### Connectivity Validation (BFS)

```
Algorithm: CanConsumerReachProducer(consumer)
1. Initialize queue with consumer
2. Mark consumer as visited
3. While queue not empty:
   a. Dequeue current node
   b. For each node with edge to current:
      - If node is producer: return true
      - If not visited: enqueue and mark visited
4. Return false (no path found)
```

**Complexity**: O(V + E)

---

### Path Finding (Shortest Path)

Uses modified BFS that tracks distances:

```
Algorithm: FindShortestPath(start, end)
1. Initialize distances[start] = 0
2. BFS from start
3. For each neighbor:
   - If distance[neighbor] > distance[current] + 1:
     - Update distance[neighbor]
4. Return distance[end]
```

**Complexity**: O(V + E)

---

## References

### Books
- **Introduction to Algorithms** (CLRS) - Chapters 22-26
- **Graph Theory** by Reinhard Diestel
- **Network Flows** by Ahuja, Magnanti, Orlin

### Papers
- Steiner Tree Problem: "The Steiner Tree Problem" by Hwang, Richards, Winter
- Planarity Testing: "A Simple Planarity Algorithm" by Boyer, Myrvold

### Online Resources
- [Graph Theory - Wikipedia](https://en.wikipedia.org/wiki/Graph_theory)
- [Network Flow Algorithms](https://cp-algorithms.com/graph/max_flow.html)
- [Steiner Tree Problem](https://en.wikipedia.org/wiki/Steiner_tree_problem)

---

## Future Improvements

### Potential Enhancements

1. **Dynamic Difficulty Adjustment**
   - Measure player performance
   - Adjust metrics in real-time

2. **Machine Learning**
   - Train on successful/failed solutions
   - Predict difficulty from graph properties

3. **Path Crossing Detection**
   - Implement line-line intersection tests
   - Penalize levels with required crossings

4. **Symmetry Breaking**
   - Identify symmetric solutions
   - Add constraints to create unique solutions

5. **Heuristic Evaluation**
   - A* search for solution paths
   - Estimate minimum moves to solve

---

*Last Updated: November 2025*

