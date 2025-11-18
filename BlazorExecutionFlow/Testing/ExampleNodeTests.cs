using System.Text.Json.Nodes;
using BlazorExecutionFlow.Flow.BaseNodes;

namespace BlazorExecutionFlow.Testing
{
    /// <summary>
    /// Example tests demonstrating how to use NodeGraphBuilder for testing node execution.
    /// </summary>
    public static class ExampleNodeTests
    {
        /// <summary>
        /// Example: Simple node execution test
        /// Tests a single node with input mapping and output verification.
        /// </summary>
        public static async Task Example_SimpleNodeExecution()
        {
            // Arrange: Build a graph with a single node
            var graph = new NodeGraphBuilder();

            graph.AddNode("addNode", typeof(BaseNodeCollection), "Add")
                .MapInput("a", "5")           // Map literal value to parameter 'a'
                .MapInput("b", "10")          // Map literal value to parameter 'b'
                .AutoMapOutputs();            // Automatically map all outputs

            // Act: Execute the graph
            var result = await graph.ExecuteAsync("addNode");

            // Assert: Verify the output
            var sum = result.GetOutput<int>("addNode", "result");
            Console.WriteLine($"5 + 10 = {sum}");  // Should print: 5 + 10 = 15

            if (sum != 15)
                throw new Exception($"Expected 15, got {sum}");
        }

        /// <summary>
        /// Example: Connected nodes with data flow
        /// Tests multiple nodes connected together, passing data between them.
        /// </summary>
        public static async Task Example_ConnectedNodes()
        {
            var graph = new NodeGraphBuilder();

            // Create first node: Add 5 + 10
            graph.AddNode("add1", typeof(BaseNodeCollection), "Add")
                .MapInput("a", "5")
                .MapInput("b", "10")
                .AutoMapOutputs()
                .ConnectTo("add2");           // Connect to next node

            // Create second node: Add result + 20
            graph.AddNode("add2", typeof(BaseNodeCollection), "Add")
                .MapInput("a", "input.result")   // Get result from previous node
                .MapInput("b", "20")
                .AutoMapOutputs();

            // Execute starting from first node
            var result = await graph.ExecuteAsync("add1");

            // Verify intermediate and final results
            var add1Result = result.GetOutput<int>("add1", "result");
            var add2Result = result.GetOutput<int>("add2", "result");

            Console.WriteLine($"First add: {add1Result}");   // 15
            Console.WriteLine($"Second add: {add2Result}");  // 35

            if (add1Result != 15 || add2Result != 35)
                throw new Exception("Node chain produced incorrect results");
        }

        /// <summary>
        /// Example: Port-driven flow control
        /// Tests conditional branching using output ports (like For loops, If conditions).
        /// </summary>
        public static async Task Example_PortDrivenFlow()
        {
            var graph = new NodeGraphBuilder();

            // For loop: iterate 3 times
            graph.AddNode("forLoop", typeof(BaseNodeCollection), "For")
                .MapInput("start", "0")
                .MapInput("end", "3")
                .WithOutputPorts("loop", "done")
                .ConnectTo("loopBody", "loop")     // Connect 'loop' port to body
                .ConnectTo("completion", "done");   // Connect 'done' port to completion

            // Loop body: This would execute 3 times
            graph.AddNode("loopBody", typeof(BaseNodeCollection), "Add")
                .MapInput("a", "input.i")          // Get loop index
                .MapInput("b", "100")
                .AutoMapOutputs();

            // Completion node: Runs after loop finishes
            graph.AddNode("completion", typeof(BaseNodeCollection), "Multiply")
                .MapInput("a", "2")
                .MapInput("b", "3")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("forLoop");

            var finalResult = result.GetOutput<int>("completion", "result");
            Console.WriteLine($"After loop completion: {finalResult}");  // 6
        }

        /// <summary>
        /// Example: Custom object with property mapping
        /// Tests nodes that return complex objects with multiple properties.
        /// </summary>
        public static async Task Example_ComplexObjectMapping()
        {
            var graph = new NodeGraphBuilder();

            // Assuming BaseNodeCollection has a CreatePerson method returning { Name, Height }
            graph.AddNode("createPerson", typeof(BaseNodeCollection), "CreatePerson")
                .MapOutput("Name", "personName")     // Map 'Name' property to 'personName'
                .MapOutput("Height", "personHeight"); // Map 'Height' property to 'personHeight'

            var result = await graph.ExecuteAsync("createPerson");

            var name = result.GetOutput<string>("createPerson", "personName");
            var height = result.GetOutput<double>("createPerson", "personHeight");

            Console.WriteLine($"Person: {name}, Height: {height}");
        }

        /// <summary>
        /// Example: Testing with JSON inputs
        /// Tests passing complex JSON data between nodes.
        /// </summary>
        public static async Task Example_JsonInputs()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("node1", typeof(BaseNodeCollection), "ProcessJson")
                .MapInput("data", "{\"value\": 42, \"name\": \"Test\"}")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("node1");

            // Access the raw JSON output
            var jsonOutput = result.GetOutputObject("node1");
            Console.WriteLine($"Output: {jsonOutput}");
        }

        /// <summary>
        /// Run all example tests
        /// </summary>
        public static async Task RunAllExamples()
        {
            try
            {
                Console.WriteLine("=== Running Example Tests ===\n");

                Console.WriteLine("1. Simple Node Execution:");
                await Example_SimpleNodeExecution();
                Console.WriteLine("✓ Passed\n");

                Console.WriteLine("2. Connected Nodes:");
                await Example_ConnectedNodes();
                Console.WriteLine("✓ Passed\n");

                Console.WriteLine("3. Port-Driven Flow:");
                await Example_PortDrivenFlow();
                Console.WriteLine("✓ Passed\n");

                Console.WriteLine("=== All Examples Passed ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Test Failed: {ex.Message}");
                throw;
            }
        }
    }
}
