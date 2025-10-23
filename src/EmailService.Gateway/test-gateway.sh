#!/bin/bash

echo "══════════════════════════════════════════════════"
echo "  API Gateway Test Script"
echo "══════════════════════════════════════════════════"
echo ""

GATEWAY_URL="http://localhost:8080"
API_KEY="test-api-key-12345"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test 1: Gateway Info
echo "Test 1: Gateway Info"
echo "------------------------------"
response=$(curl -s $GATEWAY_URL/)
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Gateway is running${NC}"
    echo "$response" | jq '.'
else
    echo -e "${RED}✗ Gateway is not responding${NC}"
    exit 1
fi
echo ""

# Test 2: Gateway Health
echo "Test 2: Gateway Health Check"
echo "------------------------------"
response=$(curl -s $GATEWAY_URL/gateway-health)
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Gateway health check passed${NC}"
else
    echo -e "${RED}✗ Gateway health check failed${NC}"
fi
echo ""

# Test 3: API Health through Gateway
echo "Test 3: API Health (through Gateway)"
echo "------------------------------"
response=$(curl -s $GATEWAY_URL/health/api)
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ API health check passed${NC}"
    echo "$response"
else
    echo -e "${RED}✗ API health check failed${NC}"
fi
echo ""

# Test 4: Send Email through Gateway
echo "Test 4: Send Email (through Gateway)"
echo "------------------------------"
response=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST "$GATEWAY_URL/email/send?sendImmediately=true" \
     -H "X-API-Key: $API_KEY" \
     -H "Content-Type: application/json" \
     -d '{
       "toEmail": "test@example.com",
       "subject": "Test via Gateway",
       "body": "<h1>Sent through API Gateway!</h1>",
       "isHtml": true
     }')

http_code=$(echo "$response" | grep "HTTP_CODE" | cut -d':' -f2)
body=$(echo "$response" | grep -v "HTTP_CODE")

if [ "$http_code" == "200" ] || [ "$http_code" == "201" ] || [ "$http_code" == "202" ]; then
    echo -e "${GREEN}✓ Email sent successfully (HTTP $http_code)${NC}"
    echo "$body" | jq '.'
else
    echo -e "${RED}✗ Failed to send email (HTTP $http_code)${NC}"
    echo "$body"
fi
echo ""

# Test 5: Swagger Docs
echo "Test 5: Swagger Documentation"
echo "------------------------------"
response=$(curl -s -o /dev/null -w "%{http_code}" $GATEWAY_URL/docs/index.html)
if [ "$response" == "200" ]; then
    echo -e "${GREEN}✓ Swagger docs accessible${NC}"
    echo "   URL: $GATEWAY_URL/docs/index.html"
else
    echo -e "${YELLOW}⚠ Swagger docs not accessible (HTTP $response)${NC}"
fi
echo ""

# Test 6: Gateway Statistics
echo "Test 6: Gateway Statistics"
echo "------------------------------"
response=$(curl -s $GATEWAY_URL/gateway-stats)
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Stats retrieved${NC}"
    echo "$response" | jq '.'
else
    echo -e "${RED}✗ Failed to get stats${NC}"
fi
echo ""

echo "══════════════════════════════════════════════════"
echo "  Test Summary"
echo "══════════════════════════════════════════════════"
echo ""
echo "Gateway URL: $GATEWAY_URL"
echo "API Key: $API_KEY"
echo ""
echo "All tests completed!"
echo ""
