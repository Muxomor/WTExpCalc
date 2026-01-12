# War Thunder Experience Calculator

This project is a web application designed to calculate the research and purchase costs of vehicles in the game War Thunder. It provides an interactive interface for users to select vehicles within a nation's tech tree and view the total Research Points (RP) and Silver Lions (SL) required.

## Features

  * **Interactive Tech Trees**: Browse complete tech trees for every nation and vehicle type in the game.
  * **Cost Calculation**: Select any number of vehicles to get an instant calculation of the total RP and SL costs.
  * **Shareable Links**: Generate a unique URL that saves your current selection, allowing you to share your research plan with others.
  * **Custom RP Values**: Modify the RP cost for any vehicle to account for partially researched modules or personal calculations.
  * **Screenshot Generation**: Provides two distinct modes for capturing the tech tree:
      * **Standard Screenshot**: Creates a high-quality image suitable for most uses.
      * **4000x4000 Screenshot**: Renders the image at maximum quality, then strictly limits its dimensions to 4000x4000 pixels. This function is specifically designed to ensure compatibility with platforms that have fixed size limits, such as FunPay.
  * **RP Limit Planning**: Set a total RP budget, and the calculator will help you plan your research by preventing over-selection and auto-adjusting costs.
  * **Multi-language Support**: The interface is available in both English and Russian.

## Technology Stack

  * **Frontend**: Blazor WebAssembly, .NET 8
  * **Web Server**: Nginx
  * **API**: PostgREST
  * **Database**: PostgreSQL
  * **Containerization**: Docker, Docker Compose

-----

## Core Components

This application relies on several external components to function correctly. The database must be set up and populated before the main application can be run.

  * **Database**: The PostgreSQL database schema and structure are managed in the [WTExpCalc\_db](https://github.com/Muxomor/WTExpCalc_db) repository. This component uses PostgREST to automatically generate a RESTful API from the database schema.
  * **Data Parser**: The database is populated with game data using a specialized Python parser available at [wt\_datamine\_parser\_py](https://github.com/Muxomor/wt_datamine_parser_py).

-----

## Getting Started

### Prerequisites

  * Docker and Docker Compose
  * Git
  * A running PostgreSQL instance

### Installation

1.  **Set up the Database**:

      * Clone the database repository:
        ```bash
        git clone https://github.com/Muxomor/WTExpCalc_db.git
        cd WTExpCalc_db
        ```
      * Follow the instructions in that repository's README to set up the PostgreSQL database and the PostgREST API.

2.  **Populate the Database**:

      * Clone the parser repository:
        ```bash
        git clone https://github.com/Muxomor/wt_datamine_parser_py.git
        cd wt_datamine_parser_py
        ```
      * Follow the instructions in that repository's README to run the parser and populate your database.

3.  **Configure and Run the Web Application**:

      * Clone this repository and navigate into the directory.
      * **For local development**, you can configure the backend API URL in `WTExpCalc/wwwroot/appsettings.Development.json`. Set the `BackendApi:BaseUrl` property to the address of your running PostgREST instance.
        ```json
        {
          "BackendApi": {
            "BaseUrl": "http://localhost:3000"
          }
        }
        ```
      * Build and run the application using Docker Compose:
        ```bash
        docker-compose up --build
        ```

    The application will be available at `http://localhost:8080`.

-----

## Deployment

In a **production environment**, the application is configured to make API calls to its own host address. Therefore, you must use a reverse proxy to route API requests (e.g., `/api/*`) to the PostgREST service and all other requests to the Blazor application.

### Standard Docker

The included `docker-compose.yaml` file is configured for a standard deployment, exposing the Nginx server on port 8080. You will need to configure your reverse proxy separately.

### Traefik

A `docker-compose-traefik.yaml` file is provided for deployments using Traefik.
**Important**: When using this setup, you must also use the Traefik-compatible database and API setup from the `WTExpCalc_db` repository. Look for the similarly named `docker-compose` file in that project. This ensures both the frontend and backend are on the same Traefik network.

### Caddy

If you are using Caddy as a reverse proxy, the following `Caddyfile` provides a working **example configuration**. It correctly routes API traffic to the backend and serves the Blazor application, along with basic security headers.

```
# Caddyfile Example
example.com {
    handle_path /api/* {
        reverse_proxy localhost:3000 # PostgREST API
    }
    
    handle {
        reverse_proxy localhost:8080 # Blazor App
    }
    
    header {
        Strict-Transport-Security "max-age=31536000; includeSubDomains"
        X-Content-Type-Options "nosniff"
        X-Frame-Options "DENY"
        X-XSS-Protection "1; mode=block"
        Referrer-Policy "strict-origin-when-cross-origin"
    }
}

www.example.com {
    redir https://wtmeta.ru{uri} permanent
}
```

-----

## Acknowledgements

Special thanks to the testers from the FunPay store [VolkodaV3214](https://funpay.com/users/1816777/), who have used this project since its earliest versions. Their invaluable feedback, bug reports, and ideas for new features have been instrumental in its development.