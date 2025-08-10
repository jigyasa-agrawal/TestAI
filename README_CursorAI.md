# Cursor AI Integration

This project includes endpoints to interact with the Cursor AI API, allowing you to send prompts and receive AI-generated responses.

## Setup

### 1. Configure Your API Key

Update your `appsettings.json` or `appsettings.Development.json` file with your actual Cursor AI credentials:

```json
{
  "CursorAI": {
    "ApiKey": "your-actual-api-key-here",
    "BaseUrl": "https://api.openai.com/v1"
  }
}
```

**Important**: Replace `"your-actual-api-key-here"` with your real Cursor AI API key.

### 2. Environment Variables (Recommended for Production)

For production environments, it's recommended to use environment variables:

```bash
export CursorAI__ApiKey="your-actual-api-key"
export CursorAI__BaseUrl="https://api.openai.com/v1"
```

Or in `appsettings.Production.json`:
```json
{
  "CursorAI": {
    "ApiKey": "${CursorAI__ApiKey}",
    "BaseUrl": "${CursorAI__BaseUrl}"
  }
}
```

## Available Endpoints

### 1. Quick Chat (GET)
**Endpoint**: `GET /api/cursorai/quick-chat`

Simple endpoint for quick testing with query parameters.

**Example**:
```
GET /api/cursorai/quick-chat?prompt=Hello, what is machine learning?
```

### 2. Simple Chat (POST)
**Endpoint**: `POST /api/cursorai/chat`

Send a prompt as JSON string in the request body.

**Example**:
```json
POST /api/cursorai/chat
Content-Type: application/json

"What are the benefits of using ASP.NET Core?"
```

### 3. Advanced Chat (POST)
**Endpoint**: `POST /api/cursorai/chat-advanced`

Send a prompt with custom parameters for more control over the AI response.

**Example**:
```json
POST /api/cursorai/chat-advanced
Content-Type: application/json

{
  "prompt": "Write a short poem about programming",
  "model": "gpt-4",
  "maxTokens": 500,
  "temperature": 0.8
}
```

**Parameters**:
- `prompt` (required): Your question or prompt for the AI
- `model` (optional): AI model to use (default: "gpt-4")
- `maxTokens` (optional): Maximum tokens in response (default: 1000)
- `temperature` (optional): Creativity level 0.0-1.0 (default: 0.7)

## Response Format

All endpoints return a consistent response format:

```json
{
  "response": "AI generated response text",
  "success": true,
  "error": null
}
```

**Success Response**:
- `response`: The AI-generated text response
- `success`: `true` if the request was successful
- `error`: `null` when successful

**Error Response**:
- `response`: Empty string or null
- `success`: `false`
- `error`: Description of what went wrong

## Testing

Use the provided `TestAI.http` file to test the endpoints. Make sure to:

1. Update the API key in your configuration
2. Start the application
3. Use the HTTP client in your IDE or tools like Postman/cURL

## Error Handling

The API includes comprehensive error handling for:
- Missing or invalid API keys
- Network connectivity issues
- Invalid requests
- API rate limits
- Server errors

## Security Notes

- Never commit your API key to version control
- Use environment variables or secure configuration for production
- Consider implementing rate limiting for public-facing endpoints
- Add authentication/authorization as needed for your use case

## Dependencies

- `Newtonsoft.Json` - JSON serialization/deserialization
- `Microsoft.Extensions.Http` - HTTP client factory (already included in ASP.NET Core)
- `Microsoft.Extensions.Options` - Configuration binding (already included in ASP.NET Core)