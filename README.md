# BlazorFlow

[![CI Build](https://github.com/RoniRaad/BlazorFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/RoniRaad/BlazorFlow/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BlazorFlow.svg)](https://www.nuget.org/packages/BlazorFlow/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BlazorFlow.svg)](https://www.nuget.org/packages/BlazorFlow/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A powerful visual node-based workflow designer for Blazor applications. Create, edit, and execute complex workflows using an intuitive node editor interface.

## Features

- **Visual Workflow Designer** - Interactive node editor with a clean, modern UI
- **100+ Built-in Nodes** - Math, strings, logic, HTTP, JSON, dates, and more
- **Executable Workflows** - Design flows that actually run with real data
- **Conditional Branching** - If/else logic, switches, and loops
- **Port-Driven Flow Control** - Advanced control flow with multiple output ports
- **Template-Based Mapping** - Flexible data transformation using Scriban templates
- **Error Handling** - Graceful error propagation without breaking the entire workflow
- **Testing Framework** - Fluent API for programmatic workflow testing
- **Import/Export** - Save and load workflows as JSON

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package BlazorFlow
```

Or via Package Manager Console:

```powershell
Install-Package BlazorFlow
```

## Quick Start

### 1. Add to your Blazor app

In your `_Imports.razor`:

```razor
@using DrawflowWrapper.Components
@using DrawflowWrapper.Models.NodeV2
```

### 2. Include required CSS and JS

In your `App.razor` or `MainLayout.razor`:

```html
<link href="_content/BlazorFlow/css/drawflowWrapper.css" rel="stylesheet" />
<script src="_content/BlazorFlow/js/drawflowInterop.js"></script>
```

**Important:** You also need to include the Drawflow.js library. Add this to your `index.html` or `_Host.cshtml`:

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/drawflow@0.0.60/dist/drawflow.min.css">
<script src="https://cdn.jsdelivr.net/npm/drawflow@0.0.60/dist/drawflow.min.js"></script>
```

### 3. Use the BlazorFlow component

```razor
@page "/workflow"
@using DrawflowWrapper.Models.NodeV2

<BlazorFlowGraph Graph="@graph" />

@code {
    private Graph graph = new Graph();
}
```

## Usage Examples

### Creating Nodes Programmatically

```csharp
using DrawflowWrapper.Testing;
using DrawflowWrapper.Drawflow.BaseNodes;

// Create a workflow graph
var graph = new NodeGraphBuilder();

// Add an addition node
graph.AddNode("addNode", typeof(BaseNodeCollection), "Add")
    .MapInput("input1", "5")
    .MapInput("input2", "10")
    .AutoMapOutputs();

// Execute the workflow
var result = await graph.ExecuteAsync("addNode");
var sum = result.GetOutput<int>("addNode", "result"); // Returns 15
```

### Chaining Nodes

```csharp
var graph = new NodeGraphBuilder();

// First node: Add 5 + 10
graph.AddNode("add1", typeof(BaseNodeCollection), "Add")
    .MapInput("input1", "5")
    .MapInput("input2", "10")
    .AutoMapOutputs()
    .ConnectTo("multiply");

// Second node: Multiply result by 2
graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
    .MapInput("input1", "input.result") // Get from previous node
    .MapInput("input2", "2")
    .AutoMapOutputs();

var result = await graph.ExecuteAsync("add1");
var finalValue = result.GetOutput<int>("multiply", "result"); // Returns 30
```

### Conditional Branching with Ports

```csharp
var graph = new NodeGraphBuilder();

// If/Else node with two output ports
graph.AddNode("ifNode", typeof(BaseNodeCollection), "If")
    .MapInput("condition", "true")
    .WithOutputPorts("then", "else")
    .ConnectTo("thenNode", "then")
    .ConnectTo("elseNode", "else");

graph.AddNode("thenNode", typeof(BaseNodeCollection), "Multiply")
    .MapInput("input1", "10")
    .MapInput("input2", "2")
    .AutoMapOutputs();

graph.AddNode("elseNode", typeof(BaseNodeCollection), "Multiply")
    .MapInput("input1", "5")
    .MapInput("input2", "3")
    .AutoMapOutputs();

var result = await graph.ExecuteAsync("ifNode");
// Only thenNode executes since condition is true
```

## Built-in Node Categories

- **Math** - Add, Subtract, Multiply, Divide, Min, Max, Clamp, Trigonometry
- **Strings** - Concat, Split, Replace, Substring, ToUpper, ToLower
- **Logic** - And, Or, Not, If/Else, Switch, Ternary
- **Comparison** - Equals, GreaterThan, LessThan, IsNull, IsEmpty
- **Control Flow** - For, While, Gate, Sequence
- **HTTP** - GET, POST with success/error ports
- **JSON** - Parse, Stringify, Merge, GetByPath, SetByPath
- **Date/Time** - UtcNow, AddDays, Format
- **Collections** - Array operations (Join, Slice, Reverse)
- **Conversion** - Parse numbers, ToString, type coercion
- **Utility** - Log, GUID, Delay

## Desktop Application Support

BlazorFlow works seamlessly with:

- **.NET MAUI Blazor Hybrid** apps (Windows, macOS, iOS, Android)
- **WPF with BlazorWebView**
- **Windows Forms with BlazorWebView**

Simply add the package and include the required CSS/JS assets.

## Documentation

- [Testing Framework Guide](BlazorFlow/Testing/README.md)
- [API Documentation](https://github.com/RoniRaad/BlazorFlow/wiki)
- [Examples](https://github.com/RoniRaad/BlazorFlow/tree/master/ExampleApp)

## Requirements

- .NET 8.0 or later
- Blazor Server, WebAssembly, or Hybrid
- Modern web browser with ES6 support

## Development & Deployment

### Building Locally

```bash
# Clone the repository
git clone https://github.com/RoniRaad/BlazorFlow.git
cd BlazorFlow

# Build the solution
dotnet build BlazorFlow.sln

# Run the example app
cd ExampleApp
dotnet run
```

### Creating a NuGet Package

```bash
cd BlazorFlow
dotnet pack -c Release -o .
```

### Publishing to NuGet

#### Automated (Recommended)

The repository includes GitHub Actions for automated deployment:

```bash
# Create a version tag and push
git tag v1.0.0
git push origin v1.0.0
```

This will automatically:
- Build the package
- Publish to NuGet.org
- Create a GitHub Release

See [.github/SETUP.md](.github/SETUP.md) for detailed setup instructions.

#### Manual

```bash
cd BlazorFlow
dotnet pack -c Release -o .
dotnet nuget push BlazorFlow.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Commit your changes (`git commit -m 'Add amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- Report issues: [GitHub Issues](https://github.com/RoniRaad/BlazorFlow/issues)
- Discussions: [GitHub Discussions](https://github.com/RoniRaad/BlazorFlow/discussions)

## Acknowledgments

Built on top of the excellent [Drawflow](https://github.com/jerosoler/Drawflow) JavaScript library by Jero Soler.
