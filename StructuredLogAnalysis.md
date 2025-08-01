# Structured Log Analysis API

## Overview

The log analysis API now supports structured responses that provide three key components for better user experience:

1. **Likely Cause** - Analysis of what caused the issue
2. **Possible Code Fix** - Recommended solution for the issue  
3. **Optional Code Snippet** - Code example demonstrating the fix (when applicable)

## Endpoints

### 1. `/api/cursorai/analyze-logs` (POST)
**Default endpoint - returns structured response**

- Uses GPT-4o by default for better structured output
- Returns `LogAnalysisResponse` with the three components
- Optimized prompt template for consistent formatting

**Request:**
```json
"Your log content here..."
```

**Response:**
```json
{
  "likelyCause": "The database connection pool is exhausted due to...",
  "possibleCodeFix": "Implement proper connection disposal using...",
  "optionalCodeSnippet": "using (var connection = new SqlConnection(...))\n{\n    // Your code here\n}",
  "success": true,
  "error": null,
  "rawResponse": "Full GPT response for reference..."
}
```

### 2. `/api/cursorai/analyze-logs-simple` (POST)
**Simple text response (original format)**

- Returns `CursorAIResponse` with unstructured text
- Maintains backward compatibility

**Response:**
```json
{
  "response": "Analysis: The issue appears to be...",
  "success": true,
  "error": null
}
```

### 3. `/api/cursorai/analyze-logs-advanced` (POST)
**Advanced configuration with structured/simple toggle**

**Request:**
```json
{
  "logs": "Your log content...",
  "model": "gpt-4o",
  "maxTokens": 2000,
  "temperature": 0.3,
  "useStructuredResponse": true,
  "customPromptTemplate": "Optional custom template..."
}
```

**Behavior:**
- `useStructuredResponse: true` + no custom template → Returns structured response
- `useStructuredResponse: false` → Returns simple text response  
- Custom template provided → Always returns simple text response (ignores useStructuredResponse)

## Response Models

### LogAnalysisResponse (Structured)
```csharp
public class LogAnalysisResponse
{
    public string LikelyCause { get; set; }        // What caused the issue
    public string PossibleCodeFix { get; set; }    // Recommended solution
    public string? OptionalCodeSnippet { get; set; } // Code example (nullable)
    public bool Success { get; set; }              // Operation success
    public string? Error { get; set; }             // Error message if failed
    public string? RawResponse { get; set; }       // Full GPT response for reference
}
```

### CursorAIResponse (Simple)
```csharp
public class CursorAIResponse
{
    public string Response { get; set; }    // GPT response text
    public bool Success { get; set; }       // Operation success
    public string? Error { get; set; }      // Error message if failed
}
```

## Example Usage

### Structured Analysis
```http
POST /api/cursorai/analyze-logs
Content-Type: application/json

"2024-01-15 10:30:45 ERROR [DatabaseService] Connection timeout
2024-01-15 10:30:46 ERROR [UserController] NullReferenceException"
```

**Expected Response:**
```json
{
  "likelyCause": "Database connection timeout leading to null reference when trying to access user data. The connection pool appears to be exhausted.",
  "possibleCodeFix": "Implement proper connection management with using statements and consider increasing the connection pool size or timeout values.",
  "optionalCodeSnippet": "// In your service class\nusing (var connection = new SqlConnection(connectionString))\n{\n    await connection.OpenAsync();\n    // Your query here\n}",
  "success": true,
  "error": null,
  "rawResponse": "LIKELY CAUSE:\nDatabase connection timeout..."
}
```

## Benefits

1. **Consistent Structure** - Always get the same format for UI display
2. **Better UX** - Users see clearly categorized information
3. **Backward Compatibility** - Original endpoints still work
4. **Flexible** - Choose structured or simple based on needs
5. **Robust Parsing** - Fallback to raw response if parsing fails