using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Microsoft.Z3;
using ColorApp.Models;

namespace ColorApp.Services
{
    public class GraphColoringSolver
    {
        public class ColoringSolution
        {
            public bool IsSatisfiable { get; set; }
            public Dictionary<int, Color> NodeColors { get; set; } = new();
            public string? ErrorMessage { get; set; }
        }

        public ColoringSolution SolveGraphColoring(Graph graph)
        {
            var solution = new ColoringSolution();

            try
            {
                using var ctx = new Context();
                var solver = ctx.MkSolver();

                // Create variables for each node-color combination
                var nodeColorVars = new Dictionary<(int nodeId, int colorIndex), BoolExpr>();
                
                for (int nodeId = 0; nodeId < graph.Nodes.Count; nodeId++)
                {
                    for (int colorIndex = 0; colorIndex < graph.AvailableColors.Count; colorIndex++)
                    {
                        nodeColorVars[(nodeId, colorIndex)] = ctx.MkBoolConst($"node_{nodeId}_color_{colorIndex}");
                    }
                }

                // Constraint 1: Each node must have exactly one color
                foreach (var node in graph.Nodes)
                {
                    var colorVars = new List<BoolExpr>();
                    for (int colorIndex = 0; colorIndex < graph.AvailableColors.Count; colorIndex++)
                    {
                        colorVars.Add(nodeColorVars[(node.Id, colorIndex)]);
                    }
                    
                    // Exactly one color per node
                    solver.Add(ctx.MkOr(colorVars.ToArray())); // At least one
                    
                    // At most one color per node
                    for (int i = 0; i < colorVars.Count; i++)
                    {
                        for (int j = i + 1; j < colorVars.Count; j++)
                        {
                            solver.Add(ctx.MkNot(ctx.MkAnd(colorVars[i], colorVars[j])));
                        }
                    }
                }

                // Constraint 2: Adjacent nodes cannot have the same color
                foreach (var edge in graph.Edges)
                {
                    for (int colorIndex = 0; colorIndex < graph.AvailableColors.Count; colorIndex++)
                    {
                        var fromVar = nodeColorVars[(edge.From.Id, colorIndex)];
                        var toVar = nodeColorVars[(edge.To.Id, colorIndex)];
                        solver.Add(ctx.MkNot(ctx.MkAnd(fromVar, toVar)));
                    }
                }

                // Constraint 3: Color usage limits
                for (int colorIndex = 0; colorIndex < graph.AvailableColors.Count; colorIndex++)
                {
                    var colorConstraint = graph.AvailableColors[colorIndex];
                    var colorUsageVars = new List<ArithExpr>();
                    
                    foreach (var node in graph.Nodes)
                    {
                        var nodeVar = nodeColorVars[(node.Id, colorIndex)];
                        colorUsageVars.Add((ArithExpr)ctx.MkITE(nodeVar, ctx.MkInt(1), ctx.MkInt(0)));
                    }
                    
                    if (colorUsageVars.Count > 0)
                    {
                        var totalUsage = colorUsageVars.Aggregate((a, b) => ctx.MkAdd(a, b));
                        solver.Add(ctx.MkLe(totalUsage, ctx.MkInt(colorConstraint.MaxUsage)));
                    }
                }

                // Constraint 4: Fixed colors from partial coloring
                foreach (var node in graph.Nodes.Where(n => n.IsFixed && n.AssignedColor.HasValue))
                {
                    var assignedColor = node.AssignedColor!.Value;
                    var colorIndex = FindColorIndex(graph.AvailableColors, assignedColor);
                    
                    if (colorIndex >= 0)
                    {
                        solver.Add(nodeColorVars[(node.Id, colorIndex)]);
                    }
                }

                // Solve
                var result = solver.Check();
                solution.IsSatisfiable = result == Status.SATISFIABLE;

                if (solution.IsSatisfiable)
                {
                    var model = solver.Model;
                    
                    foreach (var node in graph.Nodes)
                    {
                        for (int colorIndex = 0; colorIndex < graph.AvailableColors.Count; colorIndex++)
                        {
                            var varValue = model.Evaluate(nodeColorVars[(node.Id, colorIndex)]);
                            if (varValue.IsTrue)
                            {
                                solution.NodeColors[node.Id] = graph.AvailableColors[colorIndex].Color;
                                break;
                            }
                        }
                    }
                }
                else if (result == Status.UNSATISFIABLE)
                {
                    solution.ErrorMessage = "No valid coloring exists with the given constraints.";
                }
                else
                {
                    solution.ErrorMessage = "Solver could not determine satisfiability (unknown result).";
                }
            }
            catch (Exception ex)
            {
                solution.IsSatisfiable = false;
                solution.ErrorMessage = $"Error solving graph coloring: {ex.Message}";
            }

            return solution;
        }

        private int FindColorIndex(List<ColorConstraint> colors, Color targetColor)
        {
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i].Color.R == targetColor.R && 
                    colors[i].Color.G == targetColor.G && 
                    colors[i].Color.B == targetColor.B)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
