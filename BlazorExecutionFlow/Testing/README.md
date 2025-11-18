# Node Graph Testing Framework

A fluent API for building and testing node graphs programmatically in C#.

## Overview

The `NodeGraphBuilder` provides an easy way to:
- Wire up nodes without the UI
- Test node execution logic
- Verify data flows between nodes
- Test port-driven flow control

## Quick Start

```csharp
using BlazorExecutionFlow.Testing;
using BlazorExecutionFlow.Flow.BaseNodes;

// Create a graph
var graph = new NodeGraphBuilder();

// Add a node
graph.AddNode("addNode", typeof(BaseNodeCollection), "Add")
    .MapInput("a", "5")           // Map literal value
    .MapInput("b", "10")
    .AutoMapOutputs();            // Auto-map all outputs

// Execute the graph
var result = await graph.ExecuteAsync("addNode");

// Verify the result
var sum = result.GetOutput<int>("addNode", "result");
Console.WriteLine($"Result: {sum}");  // Output: 15
```

## API Reference

### NodeGraphBuilder

#### `AddNode(string nodeName, Type type, string methodName)`
Creates a new node from a static method.

```csharp
graph.AddNode("myNode", typeof(BaseNodeCollection), "Add")
```

#### `AddNode(string nodeName, MethodInfo method)`
Creates a new node from a MethodInfo.

```csharp
var method = typeof(BaseNodeCollection).GetMethod("Add", BindingFlags.Public | BindingFlags.Static);
graph.AddNode("myNode", method)
```

#### `Connect(string fromNode, string toNode, string? outputPort = null)`
Connects the output of one node to the input of another.

```csharp
graph.Connect("node1", "node2");  // Default connection
graph.Connect("forLoop", "loopBody", "loop");  // Specific port
```

#### `ExecuteAsync(string startNodeName)`
Executes the graph starting from the specified node.

```csharp
var result = await graph.ExecuteAsync("startNode");
```

### NodeBuilder

#### `MapInput(string parameterName, string inputPath)`
Maps an input value or path to a method parameter.

```csharp
.MapInput("a", "5")                    // Literal value
.MapInput("name", "input.personName")  // Path from previous node
```

#### `MapOutput(string propertyName, string? outputName = null)`
Maps a property from the method's return value to a node output.

```csharp
.MapOutput("Name", "personName")
.MapOutput("Height", "personHeight")
```

#### `AutoMapOutputs()`
Automatically maps all return properties as outputs.

```csharp
.AutoMapOutputs()
```

#### `WithOutputPorts(params string[] portNames)`
Sets output ports for port-driven flow control.

```csharp
.WithOutputPorts("loop", "done")
```

#### `MergeOutputWithInput(bool merge = true)`
Sets whether to merge output with input payload.

```csharp
.MergeOutputWithInput(true)
```

#### `ConnectTo(string targetNodeName, string? outputPort = null)`
Connects this node to another node.

```csharp
.ConnectTo("nextNode")
.ConnectTo("loopBody", "loop")
```

### GraphExecutionResult

#### `GetOutput<T>(string nodeName, string outputPath)`
Gets a typed output value from a node.

```csharp
var sum = result.GetOutput<int>("addNode", "result");
var name = result.GetOutput<string>("personNode", "Name");
```

#### `GetOutputObject(string nodeName)`
Gets the entire output object from a node as JSON.

```csharp
var output = result.GetOutputObject("myNode");
```

## Examples

### Simple Node Execution

```csharp
var graph = new NodeGraphBuilder();

graph.AddNode("addNode", typeof(BaseNodeCollection), "Add")
    .MapInput("a", "5")
    .MapInput("b", "10")
    .AutoMapOutputs();

var result = await graph.ExecuteAsync("addNode");
var sum = result.GetOutput<int>("addNode", "result");  // 15
```

### Connected Nodes with Data Flow

```csharp
var graph = new NodeGraphBuilder();

// First node: Add 5 + 10
graph.AddNode("add1", typeof(BaseNodeCollection), "Add")
    .MapInput("a", "5")
    .MapInput("b", "10")
    .AutoMapOutputs()
    .ConnectTo("add2");  // Connect to next node

// Second node: Add result + 20
graph.AddNode("add2", typeof(BaseNodeCollection), "Add")
    .MapInput("a", "input.result")  // Get result from previous node
    .MapInput("b", "20")
    .AutoMapOutputs();

var result = await graph.ExecuteAsync("add1");
var add1Result = result.GetOutput<int>("add1", "result");  // 15
var add2Result = result.GetOutput<int>("add2", "result");  // 35
```

### Port-Driven Flow Control

```csharp
var graph = new NodeGraphBuilder();

// For loop: iterate 3 times
graph.AddNode("forLoop", typeof(BaseNodeCollection), "For")
    .MapInput("start", "0")
    .MapInput("end", "3")
    .WithOutputPorts("loop", "done")
    .ConnectTo("loopBody", "loop")      // Connect 'loop' port
    .ConnectTo("completion", "done");    // Connect 'done' port

// Loop body
graph.AddNode("loopBody", typeof(BaseNodeCollection), "Add")
    .MapInput("a", "input.i")  // Loop index
    .MapInput("b", "100")
    .AutoMapOutputs();

// Completion node
graph.AddNode("completion", typeof(BaseNodeCollection), "Multiply")
    .MapInput("a", "2")
    .MapInput("b", "3")
    .AutoMapOutputs();

var result = await graph.ExecuteAsync("forLoop");
var finalResult = result.GetOutput<int>("completion", "result");
```

### Complex Object Mapping

```csharp
var graph = new NodeGraphBuilder();

graph.AddNode("createPerson", typeof(BaseNodeCollection), "CreatePerson")
    .MapOutput("Name", "personName")
    .MapOutput("Height", "personHeight");

var result = await graph.ExecuteAsync("createPerson");
var name = result.GetOutput<string>("createPerson", "personName");
var height = result.GetOutput<double>("createPerson", "personHeight");
```

## Input Path Expressions

When mapping inputs, you can use:

- **Literal values**: `"5"`, `"true"`, `"\"hello\""`
- **Input paths**: `"input.propertyName"` - gets value from previous node's output
- **JSON objects**: `"{\"value\": 42}"`
- **Template expressions**: Supports Scriban template syntax for complex transformations

## Testing Tips

1. **Keep tests focused**: Test one behavior per test method
2. **Use descriptive names**: Name nodes clearly (e.g., "addNode", "multiplyNode")
3. **Verify intermediate results**: Check outputs at each step, not just the final result
4. **Test error cases**: Verify nodes handle invalid inputs correctly
5. **Test port-driven flows**: Ensure conditional branching works as expected

## Integration with Test Frameworks

Works seamlessly with xUnit, NUnit, MSTest, etc.:

```csharp
[Fact]
public async Task AddNode_WithTwoNumbers_ReturnsSum()
{
    // Arrange
    var graph = new NodeGraphBuilder();
    graph.AddNode("addNode", typeof(BaseNodeCollection), "Add")
        .MapInput("a", "5")
        .MapInput("b", "10")
        .AutoMapOutputs();

    // Act
    var result = await graph.ExecuteAsync("addNode");

    // Assert
    var sum = result.GetOutput<int>("addNode", "result");
    Assert.Equal(15, sum);
}
```

## See Also

- [ExampleNodeTests.cs](ExampleNodeTests.cs) - Comprehensive examples
- [NodeGraphBuilder.cs](NodeGraphBuilder.cs) - Full API implementation
