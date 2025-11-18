# Test Suite Documentation

## Overview
This test suite provides comprehensive coverage to catch serialization, type conversion, and workflow persistence issues before they occur in production.

## Test Files

### 1. JsonTypeHandlingTest.cs
**Purpose**: Catch type conversion issues between JsonNode types and plain CLR types

**Test Coverage**:
- **Test 1: Direct Primitive Assignments** - Ensures direct assignment of primitives (int, bool, string, etc.) converts correctly to plain objects
- **Test 2: Serialized Assignments** - Validates that `JsonSerializer.SerializeToNode()` produces values that convert properly
- **Test 3: Mixed Assignment Methods** - Simulates real-world scenarios (like ForEachString) where both direct and serialized assignments are mixed
- **Test 4: Array Handling** - Verifies arrays maintain proper element types through conversion
- **Test 5: Nested Object Handling** - Tests deeply nested object structures maintain type integrity
- **Test 6: Edge Cases** - Covers edge cases like empty strings, zero values, false booleans, min/max integers, infinity
- **Test 7: Template Access Patterns** - Validates GetByPath + CoerceToType (used by Scriban templates) work correctly

**What It Catches**:
- JsonElement vs CLR type confusion (like the boolean handling bug)
- JsonValuePrimitive<T> vs JsonValue<JsonElement> issues (like the ForEachString + Log bug)
- Template rendering failures
- Type coercion errors

---

### 2. WorkflowSerializationTest.cs
**Purpose**: Catch workflow persistence and method deserialization issues

**Test Coverage**:
- **Test 1: Simple Method Serialization** - Basic method round-trip serialization/deserialization
- **Test 2: Generic Method Serialization** - Methods with generic parameters (e.g., List<string>)
- **Test 3: Multiple Parameters with Complex Types** - Methods with multiple parameters of varying complexity
- **Test 4: Nested Generics Serialization** - Methods with deeply nested generic types (e.g., List<List<int>>)
- **Test 5: Version Mismatch Handling** - Validates workflows saved with different .NET versions can be loaded
- **Test 6: Assembly Qualified Name Parsing** - Tests the bracket-aware comma parsing for complex type names

**What It Catches**:
- Assembly qualified name parsing errors (like the ForEachString deserialization bug)
- Version mismatch issues between saved workflows and current runtime
- Generic type parameter resolution failures
- Method signature matching errors

---

### 3. IterationOutputTest.cs
**Purpose**: Diagnose iteration node output structure issues

**Test Coverage**:
- Simulates ForEachString output generation
- Checks property types (currentItem, currentIndex, isFirst, etc.)
- Tests ToPlainObject conversion
- Validates GetByPath access
- Tests type coercion for template access

**What It Catches**:
- Iteration node output format issues
- Type mismatches in iteration metadata
- Template access failures for iteration properties

---

### 4. SerializationTest.cs
**Purpose**: Validate method serialization with real workflow data

**Test Coverage**:
- Tests ForEachString method serialization/deserialization
- Validates against actual saved workflow JSON
- Ensures cross-version compatibility

**What It Catches**:
- Real-world workflow persistence issues
- Version-specific serialization problems

---

### 5. BooleanTest.cs
**Purpose**: Validate boolean value handling throughout the system

**Test Coverage**:
- Boolean serialization
- Boolean in node outputs
- Boolean retrieval and coercion
- Comparison node → If node simulation
- ToPlainObject conversion for booleans

**What It Catches**:
- Boolean type preservation issues
- JsonElement vs bool confusion
- Conditional logic failures

---

### 6. NodeTests.cs
**Purpose**: Performance testing and optimization validation

**Test Coverage**:
- GetDownstreamNodes caching performance
- ForEachString, ForEachNumber, ForEachJson benchmarks
- Repeat node performance
- Map node variants performance

**What It Catches**:
- Performance regressions
- O(n²) complexity issues
- Memory allocation problems

---

## Running the Tests

```bash
cd TestRunner
dotnet run
```

## Test Results Summary

All tests should pass with output like:
```
✓ All JSON type handling tests PASSED
✓ All workflow serialization tests PASSED
✓ All tests completed!
```

## Issues Prevented by These Tests

### Historical Issues (Now Prevented):
1. **Boolean Handling Bug** - If node not recognizing true/false values
   - Caught by: JsonTypeHandlingTest (Test 1, 2, 3), BooleanTest

2. **Method Deserialization Bug** - "ForEachString not found" error on workflow load
   - Caught by: WorkflowSerializationTest (Test 4, 6)

3. **ForEachString + Log Error** - "Int32 cannot convert to JsonElement"
   - Caught by: JsonTypeHandlingTest (Test 3, 7), IterationOutputTest

### Future Issues Prevented:
- Version mismatch errors across .NET versions
- Type coercion failures in templates
- Generic type parameter resolution issues
- Nested object/array serialization problems
- Edge case value handling (null, zero, false, empty, infinity)
- Performance regressions in iteration nodes

## Best Practices

When adding new features:
1. Add tests to JsonTypeHandlingTest if introducing new JsonNode assignment patterns
2. Add tests to WorkflowSerializationTest if creating new node types with complex method signatures
3. Run the full test suite before commits
4. If a bug is found in production, add a test to reproduce it before fixing

## Continuous Integration

These tests should be run:
- Before every commit
- In CI/CD pipelines
- Before releasing new package versions
- After dependency updates (especially System.Text.Json)
