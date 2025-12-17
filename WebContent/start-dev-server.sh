#!/bin/bash

# Start development server for WebContent
# Usage: ./start-dev-server.sh

PORT=8000
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "🚀 Starting development server..."
echo "📁 Serving from: $DIR"
echo "🌐 Open in browser: http://localhost:$PORT/index.html"
echo ""
echo "Press Ctrl+C to stop the server"
echo ""

cd "$DIR"
python3 -m http.server $PORT

