# Quick Start - Local Development Server

## Start the Server

**Option 1: Using the script (Mac/Linux)**
```bash
cd /Users/antonioli/Desktop/Vrtest/WebContent
./start-dev-server.sh
```

**Option 2: Direct command**
```bash
cd /Users/antonioli/Desktop/Vrtest/WebContent
python3 -m http.server 8000
```

**Option 3: Using Node.js (if you have it)**
```bash
cd /Users/antonioli/Desktop/Vrtest/WebContent
npx http-server -p 8000
```

## Access the Website

Once the server is running, open in your browser:

- **Timeline Editor**: http://localhost:8000/timeline-editor.html
- **Index (JSON Display)**: http://localhost:8000/index.html

## Stop the Server

Press `Ctrl+C` in the terminal where the server is running.

## Notes

- The server runs on port **8000**
- Make changes to HTML/CSS/JS files and **refresh the browser** to see updates
- No need to restart the server for code changes
- The server is now running in the background

