# Connection System - Bidirectional Behavior

## Overview

The connection system treats connections as **bidirectional** during gameplay, even though they are stored with direction (from→to).

## How It Works

### Connection Storage
Internally, connections are stored directionally:
- **FromNode** - Source (uses an outgoing slot)
- **ToNode** - Target (receives via incoming slot)

### Bidirectional Behavior
However, for **pathfinding and validation**, connections work bidirectionally:

```
A → B

During gameplay:
- Can flow from A to B ✓
- Can flow from B to A ✓
- Prevents creating B → A (duplicate)
```

## Why Bidirectional?

### 1. Prevents Duplicate Connections
```csharp
// If A→B exists
CreateConnection(B, A) // Blocked! Connection already exists
```

### 2. Pathfinding Works Both Ways
The BFS win condition algorithm traverses **backwards** from Consumers:
```
Consumer → checks incoming connections
  → finds Neutral node
    → checks Neutral's incoming connections
      → finds Producer ✓
```

But the graph is bidirectional, so paths work in both directions.

### 3. Simplified Gameplay
Players don't need to worry about direction:
- Click Node A, then Node B = creates connection
- Fruit can flow either way along the connection
- One connection = one visual line

## Implementation Details

### ConnectionManager.ConnectionExists()

Checks for connections in **both directions**:

```csharp
private bool ConnectionExists(BaseNode from, BaseNode to)
{
    // Check from→to
    foreach (Connection conn in from.OutgoingConnections)
        if (conn.ToNode == to) return true;
    
    // Check to→from (reverse)
    foreach (Connection conn in to.OutgoingConnections)
        if (conn.ToNode == from) return true;
    
    // Check incoming connections
    foreach (Connection conn in from.IncomingConnections)
        if (conn.FromNode == to) return true;
    
    return false;
}
```

### Level Editor Warnings

The Level Editor shows warnings for bidirectional mappings:

```
Node_A can connect to Node_B
Node_B can connect to Node_A
⚠ Warning: Bidirectional connection defined (redundant)
```

## Slot Usage

### Outgoing Slots (Limited)
The **source node** uses an outgoing slot:
```
Producer (2 outgoing slots) → Neutral1, Neutral2
```

### Incoming Slots (Unlimited)
The **target node** can receive unlimited incoming:
```
HubNode ← Producer
HubNode ← Neutral1  
HubNode ← Neutral2
All valid! No limit.
```

## Consumer Nodes

Consumers are **endpoints only**:
```
Consumer.maxOutgoingConnections = 0

Producer → Neutral → Consumer ✓
Consumer → anywhere ✗ (blocked by validation)
```

## Connection Mapping in Editor

When defining valid connections in the Level Editor:

### Option 1: One-Way Mapping (Recommended)
```
Producer can connect to: [Neutral1, Neutral2]
Neutral1 can connect to: [Consumer]
Neutral2 can connect to: [Consumer]
```

At runtime:
- Producer→Neutral1 ✓
- Neutral1→Producer ✓ (bidirectional behavior)

### Option 2: Two-Way Mapping (Redundant)
```
Producer can connect to: [Neutral1]
Neutral1 can connect to: [Producer]
```

⚠ This creates redundancy but works. The editor warns you.

## Win Condition & Pathfinding

The BFS algorithm traverses connections bidirectionally:

```csharp
bool IsConsumerConnectedToProducer(ConsumerNode consumer)
{
    Queue<BaseNode> queue = new Queue<BaseNode>();
    queue.Enqueue(consumer);
    
    while (queue.Count > 0)
    {
        BaseNode current = queue.Dequeue();
        
        if (current is ProducerNode)
            return true; // Found path!
        
        // Check INCOMING connections (backwards traversal)
        foreach (Connection conn in current.IncomingConnections)
        {
            queue.Enqueue(conn.FromNode);
        }
    }
    
    return false; // No path to producer
}
```

Even though we traverse backwards via `IncomingConnections`, the graph is bidirectional, so any connected path counts.

## Example Level

```
Producer (P)
   ├─→ Neutral1 (N1)
   └─→ Neutral2 (N2)
         ├─→ HubNode (H)
         └─→ Consumer1 (C1)
               
HubNode (H)
   └─→ Consumer2 (C2)
```

### Connection Mappings (One-Way)
```
P can connect to: [N1, N2]
N1 can connect to: [H]
N2 can connect to: [C1, H]
H can connect to: [C2]
C1 can connect to: [] (consumer - no outgoing)
C2 can connect to: [] (consumer - no outgoing)
```

### Valid Gameplay Paths
All of these work due to bidirectional behavior:
```
P → N2 → C1 ✓
P → N2 → H → C2 ✓
P → N1 → H → C2 ✓ (even though N1→H wasn't in mapping, H→N1 exists)
```

## Best Practices

### ✅ DO:
- Define connections in one direction (cleaner)
- Use meaningful node IDs
- Validate levels before saving
- Let consumers have 0 outgoing connections

### ❌ DON'T:
- Define both A→B and B→A (redundant)
- Try to create connections FROM consumers
- Forget to validate level solvability

## Summary

**Connections are stored directionally but behave bidirectionally:**
- Simplifies gameplay
- Prevents duplicates
- Enables flexible pathfinding
- Consumers remain endpoints

This design gives you the best of both worlds: directed graph storage with undirected graph traversal.

