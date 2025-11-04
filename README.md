# APSIM.Docs

APSIM.Docs is a Blazor Web App that uses ApsimX tutorial and Validation files to display documentation.

## How to use

### Local Development

Local development can be conducted if you have the ApsimX repository as a 'sibling' in the same directory.
NET8.0 is required to run the app locally.

### Running the container

Use the deploy script to build and run the container. Once running the container can be viewed at [http://localhost:8080/](http://localhost:8080/)

### Adding a new doc

1. In `Program.cs` add the required model type to the `modelsToDocument` list.

2. Next create a new DocLink for the model with the required data in `Components/Layout/ValidationLinks.razor`. Just follow the pattern in the file. Models are sorted alphabetically.