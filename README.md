# SocialNetworkAnalyser

A web application for analyzing and visualizing social network data with interactive graph rendering.

## Key Features

- **Basic & Deep Analysis:**
  - Display overall statistics (total users, average friends per user).
  - In-depth metrics (average reachable users by distance, maximal clique size).
- **Interactive Graph Visualization:**
  - Render network graphs using Cytoscape.js.
  - Nodes are dynamically distributed based on node count.
  - Nodes with higher connections are positioned farther from the center.
- **Database & Data Access:**
  - Uses EF Core with SQLite.
  - Local database file stored in the project folder (e.g., `SocialNetworkAnalyser.db`).
  - On startup, the app checks for the database and applies migrations automatically if missing.
- **Logging:**
  - Integrated Serilog for structured logging (configured to output to console and files).
- **Unit Testing:**
  - xUnit test project included to verify functionality and ensure code stability.

## Technologies Used

- **ASP.NET Core MVC** – Backend and view rendering.
- **Entity Framework Core (EF Core)** – Data access using SQLite.
- **Cytoscape.js** – Interactive graph rendering.
- **Bootstrap** – Responsive UI design.
- **Serilog** – Advanced logging.
- **xUnit** – Unit testing framework.
- **HTML5, CSS, and JavaScript** – Client-side functionality and styling.

## Installation & Setup

1. **Clone the Repository:**
   ```bash
   git clone https://your-repository-url.git
   cd your-repository-folder
   ```
2. **Restore Dependencies:**
   ```bash
   dotnet restore
   ```
3. **Database Configuration:**
   - The connection string in `appsettings.json` uses SQLite by default.
   - The database file is stored locally in the project folder (e.g., `SocialNetworkAnalyser.db`).
   - On startup, EF Core automatically checks for and applies migrations to create the database if it does not exist.
   - Optionally, you can run:
   ```bash
   dotnet ef database update
   ```
4. **Run the Application:**
   - The app is available at https://localhost:5001 (or a similar URL).
   ```bash
   dotnet run
   ```
5. **Graph Visualization:**
   - Navigate to the Deep Analysis page and click "Show Graph" to load the interactive network graph.
   - Use pan and zoom features to explore the network.

## Running Tests
1. Navigate to the test project folder (e.g., `SocialNetworkAnalyser.Tests`).
2. Run the following command:
   ```bash
   dotnet test
   ```