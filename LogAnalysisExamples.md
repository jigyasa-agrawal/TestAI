# Log Analysis with Cursor AI - Examples

This document shows you exactly how to use the new log analysis endpoints that automatically format your logs with the perfect prompt for Cursor AI.

## 🎯 What the System Does

When you send logs to the analysis endpoint, it automatically wraps them in this prompt:

```
Analyze these logs and identify the root cause, the likely code location, and the reason for the issue. Suggest a fix with a code snippet and indicate where to apply it.

Logs:
[YOUR_LOGS_HERE]
```

## 📋 Available Endpoints

### 1. Simple Log Analysis - `POST /api/cursorai/analyze-logs`

**What it does:** Takes your raw logs and automatically formats them with the analysis prompt.

**Example Request:**
```bash
curl -X POST "http://localhost:5021/api/cursorai/analyze-logs" \
  -H "Content-Type: application/json" \
  -d "\"2024-01-15 10:30:45 ERROR [DatabaseService] Connection timeout after 30 seconds
2024-01-15 10:30:45 ERROR [DatabaseService] Failed to execute query: SELECT * FROM users WHERE id = 12345
2024-01-15 10:30:46 ERROR [UserController] NullReferenceException at UserController.GetUser() line 45\""
```

**What gets sent to Cursor AI:**
```
Analyze these logs and identify the root cause, the likely code location, and the reason for the issue. Suggest a fix with a code snippet and indicate where to apply it.

Logs:
2024-01-15 10:30:45 ERROR [DatabaseService] Connection timeout after 30 seconds
2024-01-15 10:30:45 ERROR [DatabaseService] Failed to execute query: SELECT * FROM users WHERE id = 12345
2024-01-15 10:30:46 ERROR [UserController] NullReferenceException at UserController.GetUser() line 45
```

### 2. Advanced Log Analysis - `POST /api/cursorai/analyze-logs-advanced`

**What it does:** Same as simple, but with custom parameters and optional custom prompt template.

**Example Request:**
```json
{
  "logs": "Your log content here...",
  "model": "gpt-4",
  "maxTokens": 2000,
  "temperature": 0.3,
  "customPromptTemplate": "Custom prompt with {0} placeholder for logs"
}
```

## 🔥 Real-World Examples

### Example 1: Database Connection Issues
```json
POST /api/cursorai/analyze-logs
Content-Type: application/json

"2024-01-15 10:30:45 ERROR [DatabaseService] Connection timeout after 30 seconds
2024-01-15 10:30:45 ERROR [DatabaseService] Failed to execute query: SELECT * FROM users WHERE id = 12345
2024-01-15 10:30:45 WARN  [ConnectionPool] Pool exhausted, unable to get connection
2024-01-15 10:30:46 ERROR [UserController] NullReferenceException at UserController.GetUser() line 45"
```

### Example 2: Payment Processing Failure
```json
POST /api/cursorai/analyze-logs-advanced
Content-Type: application/json

{
  "logs": "2024-01-15 14:22:33 INFO  [OrderService] Processing order #ORD-2024-001234\n2024-01-15 14:22:34 ERROR [PaymentGateway] Payment failed: Insufficient funds\n2024-01-15 14:22:34 ERROR [OrderService] Order processing failed for order #ORD-2024-001234",
  "model": "gpt-4",
  "maxTokens": 1500,
  "temperature": 0.2
}
```

### Example 3: Custom Analysis Template
```json
POST /api/cursorai/analyze-logs-advanced
Content-Type: application/json

{
  "logs": "Application startup failed\nSystem.InvalidOperationException: Unable to configure services",
  "customPromptTemplate": "As a senior developer, analyze these logs and provide:\n1. Root cause\n2. Quick fix\n3. Prevention tips\n\nLogs:\n{0}"
}
```

## 🚀 How to Use in Your Code

### C# Example (HttpClient)
```csharp
using var client = new HttpClient();
var logs = "Your error logs here...";
var json = JsonSerializer.Serialize(logs);
var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await client.PostAsync("http://localhost:5021/api/cursorai/analyze-logs", content);
var result = await response.Content.ReadAsStringAsync();
```

### JavaScript/TypeScript Example
```javascript
const logs = `2024-01-15 10:30:45 ERROR [Service] Something went wrong
2024-01-15 10:30:46 ERROR [Controller] NullReferenceException`;

const response = await fetch('http://localhost:5021/api/cursorai/analyze-logs', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(logs)
});

const analysis = await response.json();
console.log(analysis.response); // AI analysis
```

### Python Example
```python
import requests
import json

logs = """2024-01-15 10:30:45 ERROR [Service] Database connection failed
2024-01-15 10:30:46 ERROR [Controller] Unable to process request"""

response = requests.post(
    'http://localhost:5021/api/cursorai/analyze-logs',
    headers={'Content-Type': 'application/json'},
    data=json.dumps(logs)
)

analysis = response.json()
print(analysis['response'])  # AI analysis
```

## 💡 Tips for Best Results

1. **Include context:** Add timestamps, service names, and error codes
2. **Keep it focused:** Don't send massive log files - extract relevant sections
3. **Use lower temperature (0.1-0.3):** For more focused, technical analysis
4. **Increase max tokens:** For complex issues that need detailed explanations

## 🔧 Configuration

**Default Settings for Log Analysis:**
- **Model:** gpt-4 
- **Max Tokens:** 2000 (vs 1000 for regular chat)
- **Temperature:** 0.3 (vs 0.7 for regular chat - more focused responses)

These defaults are optimized for technical log analysis but can be customized in the advanced endpoint.

## 📝 Response Format

All endpoints return:
```json
{
  "response": "Detailed AI analysis with root cause, code location, and fix suggestions",
  "success": true,
  "error": null
}
```

The AI will typically provide:
- **Root Cause:** What caused the issue
- **Code Location:** Where to look in your code
- **Suggested Fix:** Code snippets and implementation steps
- **Prevention:** How to avoid this in the future