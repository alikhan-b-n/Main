#!/bin/bash

# Script to test Ollama integration
# This verifies that the Ollama service works correctly

echo "================================================"
echo "  Ollama Integration Test"
echo "================================================"
echo ""

# Test 1: Check if Ollama is installed
echo "[1/4] Checking if Ollama is installed..."
if command -v ollama &> /dev/null; then
    echo "✅ Ollama is installed"
    ollama --version
else
    echo "❌ Ollama is NOT installed"
    echo ""
    echo "To install Ollama:"
    echo "  brew install ollama"
    echo "  OR download from: https://ollama.com/download"
    exit 1
fi
echo ""

# Test 2: Check if Ollama server is running
echo "[2/4] Checking if Ollama server is running..."
if curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo "✅ Ollama server is running on http://localhost:11434"
else
    echo "❌ Ollama server is NOT running"
    echo ""
    echo "To start Ollama:"
    echo "  ollama serve"
    exit 1
fi
echo ""

# Test 3: Check available models
echo "[3/4] Checking available models..."
MODELS=$(curl -s http://localhost:11434/api/tags | grep -o '"name":"[^"]*"' | cut -d'"' -f4)

if [ -z "$MODELS" ]; then
    echo "❌ No models installed"
    echo ""
    echo "To install a model:"
    echo "  ollama pull llama3      # Recommended (4.7GB)"
    echo "  ollama pull mistral     # Faster (4.1GB)"
    echo "  ollama pull phi         # Lightweight (1.6GB)"
    exit 1
else
    echo "✅ Available models:"
    echo "$MODELS" | sed 's/^/  - /'
fi
echo ""

# Test 4: Test Ollama API with sample request
echo "[4/4] Testing Ollama API with sample request..."
TEST_RESPONSE=$(curl -s -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama3",
    "prompt": "Say hello in one word.",
    "stream": false,
    "options": {
      "temperature": 0.7,
      "num_predict": 10
    }
  }')

if echo "$TEST_RESPONSE" | grep -q '"response"'; then
    RESPONSE_TEXT=$(echo "$TEST_RESPONSE" | grep -o '"response":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Ollama API is working"
    echo "  Sample response: $RESPONSE_TEXT"
else
    echo "❌ Ollama API test failed"
    echo "  Response: $TEST_RESPONSE"
    exit 1
fi
echo ""

# Summary
echo "================================================"
echo "  ✅ ALL TESTS PASSED"
echo "================================================"
echo ""
echo "Your Ollama integration is ready to use!"
echo ""
echo "To test with your CRM:"
echo "  1. dotnet run --project Lama.Api"
echo "  2. POST http://localhost:5000/crm/ai/activities/{id}/summarize"
echo ""