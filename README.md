# APSIM.Docs

APSIM.Docs is a Blazor Web App that uses ApsimX tutorial and Validation files to display documentation.

## Project structure

This is a quick map of the most important files and folders for first-time developers.

- `Program.cs`: Application startup and service configuration.
- `Components/`: Blazor UI code.
- `Components/Pages/`: Routeable pages (home, model lists, tutorials, error page).
- `Components/Layout/`: Shared layout and link list components used across pages.
- `Components/Custom/`: Reusable custom UI components (for example, doc link cards).
- `Components/State/`: Shared state containers used between components.
- `wwwroot/`: Static assets and CSS files.
- `wwwroot/app.css`: Global app styling.
- `wwwroot/docs.css`: Styling applied to generated documentation content.
- `apsim-docs-html/`: Source HTML documentation files that are displayed by the app.
- `.vscode/tasks.json`: Local development tasks for build, run, and watch.
- `.vscode/launch.json`: VS Code debug launch configuration.
- `dockerfile`, `docker-compose.yml`, `deploy.sh`: Container build and run workflow.

## How to use

### Local Development

- .NET 8.0 is required to run the app locally.
- VS Code tasks are available in Terminal > Run Task...:
  - build: Compile the project.
  - run: Start the app once without file watching.
  - watch: Start the app with hot reloading.

### Debugging in VS Code

- Open Run and Debug in VS Code.
- Select the `Debug: APSIM.Docs` configuration.
- Press F5 to launch the app under the debugger.

### Running the container

Use the deploy script to build and run the container. Once running the container can be viewed at [http://localhost:8080/](http://localhost:8080/)

### Production deployment location

- Production build artifacts are published as a Docker image to Docker Hub: `apsiminitiative/apsim-docs`.
- In deployment, the app runs as the `apsim-docs` container and listens on port `8080`.
- Production documentation content is mounted from the host path `/data/apsim-docs/` into the container at `/app/apsim-docs-html`.
- Any public production URL or reverse-proxy routing is environment-specific and managed outside this repository. However at the time of writing the production version is hosted at: [APSIM Docs](https://docs.apsim.info)

### Adding a new doc

1. Add the html version of the documentation to the project folder.
2. Next create a new DocLink for the model with the required data in the relevent `Components/Layout/*Links.razor` file for the documentation type. Just follow the pattern in the file. Models are sorted alphabetically.
