# MLoop

**MLoop** is an ML Ops service built on **.NET 8.0**, designed to streamline the management, training, and deployment of machine learning models. Leveraging the power of [ML.NET](https://www.nuget.org/profiles/MLNET) for model creation and the [mlnet-predict](https://www.nuget.org/packages/mlnet-predict) tool for predictions, MLoop provides a robust set of RESTful APIs for interacting with ML scenarios, models, data submissions, and predictions. This enables efficient and scalable machine learning operations tailored to your project's needs.

## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [API Documentation](#api-documentation)
  - [Base URL](#base-url)
  - [Endpoints](#endpoints)
    - [Get All ML Scenarios](#get-all-ml-scenarios)
    - [Get a Specific ML Scenario](#get-a-specific-ml-scenario)
    - [Get Models of a Scenario](#get-models-of-a-scenario)
    - [Create a New Model](#create-a-new-model)
    - [Get a Specific Model](#get-a-specific-model)
    - [Get Model Training Log](#get-model-training-log)
    - [Submit Data for Training or Evaluation](#submit-data-for-training-or-evaluation)
    - [Submit Data for Prediction](#submit-data-for-prediction)
    - [Get Prediction Result](#get-prediction-result)
- [Usage Examples](#usage-examples)

## Features

- **Scenario Management:** Create, retrieve, and manage ML scenarios.
- **Model Management:** Train new models using [ML.NET](https://www.nuget.org/profiles/MLNET), retrieve existing models, and access model training logs.
- **Data Submission:** Submit data in JSON or CSV formats for training or prediction.
- **Prediction:** Generate predictions using the [mlnet-predict](https://www.nuget.org/packages/mlnet-predict) tool and retrieve prediction results.
- **File Monitoring:** Automatically process input files for predictions.
- **Error Handling:** Comprehensive error handling with global exception filters.

## Prerequisites

- **ML.NET Packages**: MLoop utilizes the [ML.NET](https://www.nuget.org/profiles/MLNET) suite for model creation and management. These packages are included in the project dependencies and will be restored automatically.

- **ML.NET CLI Tool**: Install the ML.NET command-line interface (CLI) tool to enable advanced model management and training capabilities.
  
  ```bash
  dotnet tool search mlnet
  # Verify installation
  mlnet --version
  ```

- **mlnet-predict Tool**: Install the `mlnet-predict` tool globally to enable prediction capabilities.
  
  ```bash
  dotnet tool install -g mlnet-predict
  # Verify installation
  mlnet-predict --version
  ```

## Configuration

MLoop uses `MLoopOptions` for configuration, which can be set in the `appsettings.json` file or through environment variables.

### Example `appsettings.json`:
```json
{
  "MLoopOptions": {
    "Path": "C:\\MLoopData"
  },
  ...
}
```

- **Path:** Base directory for storing scenarios and models.

## API Documentation

### Base URL

```
https://localhost:7039/api
```

### Endpoints

#### Get All ML Scenarios

**Endpoint:**
```
GET /api/scenarios
```

**Description:**
Retrieves a list of all available ML scenarios.

**Example Request:**
```http
GET https://localhost:7039/api/scenarios
```

#### Get a Specific ML Scenario

**Endpoint:**
```
GET /api/scenarios/{scenarioName}
```

**Description:**
Retrieves details of a specific ML scenario by name.

**Example Request:**
```http
GET https://localhost:7039/api/scenarios/scenario_1
```

#### Get Models of a Scenario

**Endpoint:**
```
GET /api/scenarios/{scenarioName}/models
```

**Description:**
Retrieves all models associated with a specific scenario.

**Example Request:**
```http
GET https://localhost:7039/api/scenarios/scenario_1/models
```

#### Create a New Model

**Endpoint:**
```
POST /api/scenarios/{scenarioName}/train
```

**Description:**
Initiates the training of a new model within the specified scenario using [ML.NET](https://www.nuget.org/profiles/MLNET).

**Example Request:**
```http
POST https://localhost:7039/api/scenarios/scenario_1/train
```

#### Get a Specific Model

**Endpoint:**
```
GET /api/scenarios/{scenarioName}/models/{modelName}
```

**Description:**
Retrieves details of a specific model within a scenario.

**Example Request:**
```http
GET https://localhost:7039/api/scenarios/scenario_1/models/nD05HPCA
```

#### Get Model Training Log

**Endpoint:**
```
GET /api/scenarios/{scenarioName}/models/{modelName}/log
```

**Description:**
Retrieves the training log of a specific model.

**Example Request:**
```http
GET https://localhost:7039/api/scenarios/scenario_1/models/nD05HPCA/log
```

#### Submit Data for Training or Evaluation

**Endpoint:**
```
POST /api/scenarios/{scenarioName}/data
```

**Description:**
Submits data in JSON or CSV format for training or evaluation purposes.

**Headers:**
- `Content-Type: application/json` or `Content-Type: text/csv`

**Example Requests:**

- **JSON:**
  ```http
  POST https://localhost:7039/api/scenarios/scenario_1/data
  Content-Type: application/json

  [
      {
          "userId": 900,
          "movieId": 240,
          "rating": 4
      },
      {
          "userId": 900,
          "movieId": 241,
          "rating": 5
      },
      {
          "userId": 900,
          "movieId": 242,
          "rating": 3.5
      }
  ]
  ```

- **CSV:**
  ```http
  POST https://localhost:7039/api/scenarios/scenario_1/data
  Content-Type: text/csv

  userId,movieId,rating
  900,240,4
  900,241,5
  900,242,3.5
  ```

#### Submit Data for Prediction

**Endpoint:**
```
POST /api/scenarios/{scenarioName}/predict
```

**Description:**
Submits data in JSON or CSV format to generate predictions using the [mlnet-predict](https://www.nuget.org/packages/mlnet-predict) tool.

**Headers:**
- `Content-Type: application/json` or `Content-Type: text/csv`

**Example Requests:**

- **JSON:**
  ```http
  POST https://localhost:7039/api/scenarios/scenario_1/predict
  Content-Type: application/json

  [
      {
          "userId": 900,
          "movieId": 240
      },
      {
          "userId": 900,
          "movieId": 241
      },
      {
          "userId": 900,
          "movieId": 242
      }    
  ]
  ```

- **CSV:**
  ```http
  POST https://localhost:7039/api/scenarios/scenario_1/predict
  Content-Type: text/csv

  userId,movieId
  1,101
  1,110
  1,151
  ```

#### Get Prediction Result

**Endpoint:**
```
GET /api/scenarios/{scenarioName}/predict/{inputName}
```

**Description:**
Retrieves the prediction result for a previously submitted input.

**Example Request:**
```http
GET https://localhost:7039/api/scenarios/scenario_1/predict/input_8
```

## Usage Examples

You can interact with the MLoop API using tools like [Postman](https://www.postman.com/) or `curl`. Below are some example requests.

### Example 1: Submit Data in JSON

```bash
curl -X POST https://localhost:7039/api/scenarios/scenario_1/data \
     -H "Content-Type: application/json" \
     -d '[
         { "userId": 900, "movieId": 240, "rating": 4 },
         { "userId": 900, "movieId": 241, "rating": 5 },
         { "userId": 900, "movieId": 242, "rating": 3.5 }
     ]'
```

### Example 2: Submit Data in CSV

```bash
curl -X POST https://localhost:7039/api/scenarios/scenario_1/data \
     -H "Content-Type: text/csv" \
     -d 'userId,movieId,rating
     900,240,4
     900,241,5
     900,242,3.5'
```

### Example 3: Submit Data for Prediction and Retrieve Result

1. **Submit Prediction Data:**
   ```bash
   curl -X POST https://localhost:7039/api/scenarios/scenario_1/predict \
        -H "Content-Type: application/json" \
        -d '[
            { "userId": 900, "movieId": 240 },
            { "userId": 900, "movieId": 241 },
            { "userId": 900, "movieId": 242 }
        ]'
   ```

   **Response:**
   ```json
   "input_8"
   ```

2. **Retrieve Prediction Result:**
   ```bash
   curl -X GET https://localhost:7039/api/scenarios/scenario_1/predict/input_8
   ```

   **Response:**
   ```json
   {
       "predictions": [
           { "userId": 900, "movieId": 240, "predictedRating": 4.5 },
           { "userId": 900, "movieId": 241, "predictedRating": 3.7 },
           { "userId": 900, "movieId": 242, "predictedRating": 4.2 }
       ]
   }
   ```