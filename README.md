# A Climbing Gym Specific Graph Coloring Problem using Z3

When route setting in small climbing gym the number of hold colors may be limited.
This limitation creates an important problem for the route setters, as they have to make sure that no two adjacent routes contain similar hold color while maximazing the amount of hold usage.
Indeed, it would be a shame to leave holds unused because the hold color assigment on the walls was sub-optimal.

In order to assist route setters, this application allows the user to design the walls, the number of routes on each wall and the available colors.
Then, the route setters can take informed decision on which color place on each wall.

This graph coloring is a special case where:
 - A wall is a fully connected sub-graph which each node representing a route on it;
 - Two adjacent walls are full connected between each-other.
The final graph obtained can be seen as a completet n-partite graph with n the number of walls.

## Features

- **Walls & Routes Interface**: Define graphs using an intuitive wall-based paradigm
- **Color Management**: Define available colors and usage constraints
- **Partial Coloring**: Pre-assign colors to specific nodes/routes
- **Z3 Integration**: Solve graph coloring problems using constraint satisfaction
- **Visual Solution**: Display the computed coloring on the graph
- **Synchronized Views**: Changes between Graph View and Walls & Routes are automatically synchronized

## How to Use

### 1. Building and Running
```bash
cd color_app
dotnet build
dotnet run
```

### 2. Creating a Graph

The application provides two ways to create graphs:

#### Method 1: Graph View (Direct Node/Edge Creation)

##### Adding Nodes
1. Switch to the "Graph View" tab
2. Select "Add Nodes" mode (default)
3. Click anywhere on the white canvas to create nodes
4. Each node will be numbered automatically

##### Adding Edges
1. Select "Add Edges" mode
2. Click on a node to start an edge (it will be highlighted in red)
3. Click on another node to complete the edge
4. Press Escape to cancel edge creation

#### Method 2: Walls & Routes (Structured Graph Definition)

##### Creating Walls
1. Switch to the "Walls and Routes" tab
2. Enter the number of routes for a wall in the text box
3. Click "Add Wall" to create a wall with that many routes
4. Repeat to add more walls

##### Understanding the Wall-to-Graph Translation
- Each **wall** becomes a **complete subgraph** (all routes within a wall are connected to each other)
- **Adjacent walls** are **fully connected** to each other (every route in wall N connects to every route in wall N+1)
- Each **route** corresponds to a **node** in the graph

##### Example: 2 walls with 3 routes each
- Wall 0: Routes W0R0, W0R1, W0R2 (all connected to each other)
- Wall 1: Routes W1R0, W1R1, W1R2 (all connected to each other)
- Cross-connections: Every route in Wall 0 connects to every route in Wall 1
- Total: 6 nodes, 12 edges (3+3 internal + 9 cross-connections)

### 3. Managing Colors

#### Adding Colors
1. Click the color picker button to select a color
2. Enter the maximum usage count (how many nodes can use this color)
3. Click "Add" to add the color to the available colors list

#### Removing Colors
1. Click the "×" button next to any color in the list to remove it

### 4. Setting Partial Coloring

#### In Graph View
1. Select "Set Color" mode or use the context menu
2. Right-click on any node to open the color selection menu
3. Choose a color from the available colors to fix that node's color
4. Select "Clear Color" to remove a fixed color

#### In Walls & Routes View
1. Click on any route's color button (small colored square next to route name)
2. Choose a color from the context menu to fix that route's color
3. Select "Clear Color" to remove a fixed color
4. Changes are automatically synchronized with the Graph View

### 5. Solving the Graph

1. Ensure you have:
   - At least one node in the graph
   - At least one available color
2. Click "Solve with Z3" button
3. The application will use Z3 to find a valid coloring
4. If a solution exists, nodes will be colored according to the constraints
5. If no solution exists, an error message will be displayed

### 6. Other Operations

#### Other Operations (Graph View)

##### Select/Move Mode
1. Select "Select/Move" mode
2. Click and drag nodes to reposition them
3. Press Delete to remove selected nodes

#### Clear Operations
- **Clear Graph**: Removes all nodes, edges, and walls
- **Clear Solution**: Removes all computed colors (keeps manually assigned colors)

### 6. Switching Between Views

- **Graph View**: Traditional node-and-edge graph editor
- **Walls & Routes**: Structured wall-based graph definition
- Views are automatically synchronized - changes in one view appear in the other
- Use the tabs at the top of the main area to switch between views

## Graph Coloring Constraints

The Z3 solver enforces the following constraints:

1. **Color Assignment**: Each node must have exactly one color
2. **Adjacent Constraint**: Adjacent nodes (connected by an edge) cannot have the same color
3. **Usage Limits**: Each color can only be used up to its specified maximum count
4. **Fixed Colors**: Manually assigned colors are preserved in the solution

## Example Usage

### Example 1: Simple Triangle (Graph View)
1. Create a triangle graph (3 nodes, 3 edges connecting them all)
2. Add 3 colors (Red, Blue, Green) with max usage of 1 each
3. Right-click one node and assign it Red color
4. Click "Solve with Z3"
5. The solver will assign Blue and Green to the remaining nodes

### Example 2: Wall Structure (Walls & Routes View)
1. Switch to "Walls and Routes" tab
2. Create Wall 0 with 2 routes
3. Create Wall 1 with 2 routes  
4. This creates a graph with 4 nodes where:
   - W0R0 connects to W0R1 (same wall)
   - W1R0 connects to W1R1 (same wall)
   - W0R0 connects to W1R0 and W1R1 (adjacent walls)
   - W0R1 connects to W1R0 and W1R1 (adjacent walls)
5. Add 2 colors (Red, Blue) with max usage of 2 each
6. Click "Solve with Z3"
7. The solver will color the routes ensuring no adjacent routes have the same color

## Technical Details

- **Framework**: .NET 8.0 WPF
- **Solver**: Microsoft Z3 4.12.2
- **Language**: C#

## Project Structure

```
ColorApp/
├── Models/
│   ├── GraphNode.cs        # Node representation
│   ├── GraphEdge.cs        # Edge representation
│   ├── Graph.cs            # Graph data structure
│   └── WallRouteSystem.cs  # Wall-route system and conversion logic
├── Services/
│   └── GraphColoringSolver.cs  # Z3 integration
├── ViewModels/
│   └── ColorConstraintViewModel.cs  # Color constraint UI binding
├── Controls/
│   ├── ColorPicker.cs      # Custom color picker control
│   ├── WallControl.xaml    # Wall display control
│   └── WallControl.xaml.cs # Wall display logic
├── MainWindow.xaml         # Main UI layout with tabs
├── MainWindow.xaml.cs      # Main UI logic
├── App.xaml               # Application resources
└── App.xaml.cs            # Application entry point
```

## Troubleshooting

- **Z3 not found**: Ensure the Microsoft.Z3 NuGet package is properly installed
- **No solution found**: Check if your graph has a valid coloring with the given constraints
- **Performance issues**: For large graphs (>50 nodes), solving may take longer

## License

This project uses the Microsoft Z3 theorem prover, which is licensed under the MIT License.
