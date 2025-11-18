# Workflow Scenarios Test Coverage

This document describes the real-world workflow scenarios covered by the test suite.

## Common Workflows (10 scenarios)

### 1. Simple Data Transformation
**User Story**: As a user, I want to extract data from an object and transform it.

**Example**:
- Input: User object with name, age, email
- Action: Extract name and age fields
- Action: Add new "verified" field
- Output: Modified user object

**Nodes Used**: Input → GetByPath → SetByPath → Output

---

### 2. Conditional Branching
**User Story**: As a user, I want to filter users into adults and minors.

**Example**:
- Input: List of users with ages
- Action: For each user, check if age >= 18
- Action: Route to "adults" or "minors" path
- Output: Two separate lists

**Nodes Used**: ForEach → GreaterThanOrEqual → If → Collect

---

### 3. Iteration with Transformation
**User Story**: As a user, I want to uppercase all names in a list.

**Example**:
- Input: ["alice", "bob", "charlie"]
- Action: For each name, convert to uppercase
- Output: ["ALICE", "BOB", "CHARLIE"]

**Nodes Used**: ForEachString → Transform → Collect

---

### 4. Filtering and Mapping
**User Story**: As a user, I want to filter numbers > 10 and double them.

**Example**:
- Input: [5, 12, 8, 15, 20, 3]
- Action: Filter where n > 10
- Action: Multiply by 2
- Output: [24, 30, 40]

**Nodes Used**: ForEach → GreaterThan → If → Multiply → Collect

---

### 5. Nested Iterations
**User Story**: As a user, I want to process a matrix (rows and columns).

**Example**:
- Input: [[1, 2, 3], [4, 5, 6]]
- Action: For each row, for each column, access value
- Output: Flattened list with positions

**Nodes Used**: ForEach → ForEach → Process → Collect

---

### 6. String Manipulation
**User Story**: As a user, I want to build email addresses from user data.

**Example**:
- Input: { firstName: "John", lastName: "Doe", domain: "example.com" }
- Action: Template: {{firstName}}.{{lastName}}@{{domain}}
- Output: "john.doe@example.com"

**Nodes Used**: Input → Template/Concatenate → Output

---

### 7. Math Operations
**User Story**: As a user, I want to calculate total price with tax.

**Example**:
- Input: { price: 100, quantity: 2, taxRate: 0.1 }
- Action: subtotal = price × quantity
- Action: tax = subtotal × taxRate
- Action: total = subtotal + tax
- Output: 220

**Nodes Used**: Multiply → Multiply → Add → Output

---

### 8. Data Aggregation
**User Story**: As a user, I want to calculate sum, count, and average.

**Example**:
- Input: [10, 20, 30, 40, 50]
- Action: Sum all values
- Action: Count items
- Action: Calculate average
- Output: { sum: 150, count: 5, average: 30 }

**Nodes Used**: ForEach → Accumulator → Divide → Output

---

### 9. Complex Conditionals
**User Story**: As a user, I want to filter with multiple AND conditions.

**Example**:
- Rule: age >= 18 AND premium = true AND country = "US"
- Input: List of users
- Action: Check all three conditions
- Output: Filtered list

**Nodes Used**: ForEach → GreaterThanOrEqual → Equal → And → If → Collect

---

### 10. Variable Storage and Retrieval
**User Story**: As a user, I want to store intermediate results and use them later.

**Example**:
- Action: Process users, store count
- Action: Process data, store in variable
- Action: Later, retrieve stored values
- Output: Combined results

**Nodes Used**: Process → SetVariable → ... → GetVariable → Output

---

## Advanced Workflows (15 scenarios)

### 1. Data Validation Pipeline
**User Story**: As a user, I want to validate input data and reject invalid records.

**Example**:
- Input: User records with email and age
- Action: Validate email contains @ and .
- Action: Validate age is positive and < 150
- Action: Store valid records, log errors
- Output: Valid records + error list

**Nodes Used**: ForEach → Validate → If → Collect/LogError

---

### 2. Null Handling and Defaults
**User Story**: As a user, I want to handle missing data with default values.

**Example**:
- Input: Records with optional fields (some null)
- Action: Check if field is null
- Action: Use default value if null
- Output: Records with no nulls

**Nodes Used**: GetByPath → IsNull → If → SetDefault → Output

---

### 3. List Search and Find
**User Story**: As a user, I want to find the first item matching criteria.

**Example**:
- Input: List of products
- Action: Find first product > $100 that's in stock
- Action: Stop searching after first match
- Output: Found product (or null)

**Nodes Used**: ForEach → GreaterThan → And → If → Break → Output

---

### 4. JSON Object Construction
**User Story**: As a user, I want to build a complex API response.

**Example**:
- Action: Set status, timestamp
- Action: Build nested user object
- Action: Add permissions array
- Action: Add metadata
- Output: Complex JSON structure

**Nodes Used**: SetByPath (multiple) → Output

---

### 5. Data Merging and Joining
**User Story**: As a user, I want to join user data with order data (like SQL JOIN).

**Example**:
- Input: Users table, Orders table
- Action: For each order, find matching user
- Action: Merge user name into order
- Output: Enriched orders with user names

**Nodes Used**: ForEach → Find → Merge → Collect

---

### 6. Accumulator Pattern
**User Story**: As a user, I want running totals, max, min while iterating.

**Example**:
- Input: Transactions [100, -50, 75, -25, 200]
- Action: Track running balance
- Action: Track max transaction
- Action: Track min transaction
- Output: { balance: 300, max: 200, min: -50 }

**Nodes Used**: ForEach → Accumulator (balance, max, min) → Output

---

### 7. Multi-Output Branching
**User Story**: As a user, I want to route items to different paths by category.

**Example**:
- Input: Products with categories
- Action: Check category
- Action: Route to electronics / clothing / other path
- Output: Three separate collections

**Nodes Used**: ForEach → Switch/If → RouteToPath → Collect (3x)

---

### 8. String Parsing and Extraction
**User Story**: As a user, I want to extract domains from email addresses.

**Example**:
- Input: ["alice@example.com", "bob@test.org", "charlie@example.com"]
- Action: Split on @
- Action: Extract domain
- Action: Count occurrences
- Output: { "example.com": 2, "test.org": 1 }

**Nodes Used**: ForEach → Split → Extract → Count → Output

---

### 9. Range Operations
**User Story**: As a user, I want to categorize values by range.

**Example**:
- Input: [5, 15, 25, 35, 45, 55, 65, 75]
- Action: Categorize as low (< 20), medium (20-49), high (>= 50)
- Output: { low: [5,15], medium: [25,35,45], high: [55,65,75] }

**Nodes Used**: ForEach → RangeCheck → If → Collect (3x)

---

### 10. Conditional Accumulation
**User Story**: As a user, I want to sum only positive values.

**Example**:
- Input: [10, -5, 20, -3, 15, -8]
- Action: If value > 0, add to sum
- Action: Otherwise, increment negative count
- Output: { positiveSum: 45, negativeCount: 3 }

**Nodes Used**: ForEach → GreaterThan → If → Accumulate → Output

---

### 11. Batch Processing
**User Story**: As a user, I want to process items in groups of N.

**Example**:
- Input: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
- Action: Group into batches of 3
- Action: Process each batch
- Output: [[1,2,3], [4,5,6], [7,8,9], [10]]

**Nodes Used**: ForEach → BatchAccumulator → Flush → Collect

---

### 12. Error Recovery
**User Story**: As a user, I want to handle parsing errors with fallback values.

**Example**:
- Input: ["123", "abc", "456", "xyz", "789"]
- Action: Try to parse as integer
- Action: On error, use 0 as fallback
- Output: [123, 0, 456, 0, 789]

**Nodes Used**: ForEach → TryParse → If → SetFallback → Collect

---

### 13. Deep Nested Data Access
**User Story**: As a user, I want to access deeply nested JSON fields.

**Example**:
- Input: { company: { department: { team: { member: { name: "John" } } } } }
- Action: Access company.department.team.member.name
- Output: "John"

**Nodes Used**: GetByPath (deep path) → Output

---

### 14. Dynamic Property Access
**User Story**: As a user, I want to access properties using variable names.

**Example**:
- Input: User object, property names: ["firstName", "lastName", "age"]
- Action: For each property name, get value from user
- Output: { firstName: "John", lastName: "Doe", age: 30 }

**Nodes Used**: ForEach → GetByPath (dynamic) → Collect

---

### 15. Array Transformations
**User Story**: As a user, I want to chain filter → map → reduce operations.

**Example**:
- Input: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
- Action: Filter even numbers
- Action: Map: square each
- Action: Reduce: sum all
- Output: 220 (2² + 4² + 6² + 8² + 10²)

**Nodes Used**: ForEach → Filter → ForEach → Map → Accumulator

---

## Coverage Summary

| Category | Scenarios Tested | Key Features |
|----------|------------------|--------------|
| **Data Flow** | 10 | Input/Output, Transformation, GetByPath, SetByPath |
| **Conditionals** | 8 | If nodes, Comparison nodes, Boolean logic |
| **Iteration** | 12 | ForEach variants, Nested loops, Batch processing |
| **Aggregation** | 6 | Sum, Count, Average, Max, Min, Accumulation |
| **String Operations** | 4 | Concatenation, Templates, Parsing, Extraction |
| **Math** | 3 | Add, Multiply, Divide with type preservation |
| **Data Structures** | 8 | Nested objects, Arrays, Dynamic access, Merging |
| **Error Handling** | 3 | Null handling, Defaults, Try/Catch patterns |
| **Advanced Patterns** | 6 | Validation pipelines, Joins, Routing, Batching |

**Total: 25 unique workflow patterns across 48+ tests**
