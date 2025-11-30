# Fault Diagnosis Assistant

A .NET 10 application that uses AI to diagnose vehicle faults based on user symptoms and technical documents.

## 🏗️ Architecture

The system follows a **Clean Architecture** pattern and consists of two main workflows:

### 1. Data Ingestion Pipeline
This background process prepares the knowledge base:
1.  **Read:** The `DataPipeline` console app reads text files from the `docs/` folder.
2.  **Chunk & Enrich:** Files are split into smaller segments. The system automatically extracts titles and error codes (e.g., P0123) from the text to create structured metadata.
3.  **Embed:** Each chunk is converted into a vector representation using the `nomic-embed-text` model via Ollama.
4.  **Store:** Vectors and metadata are stored in **Qdrant** for fast similarity search.

### 2. Diagnosis API Flow
When a user sends a diagnosis request:
1.  **Query Expansion:** The API asks the LLM to generate technical synonyms for the user's symptom (e.g., "shaking" -> "misfire, vibration").
2.  **Vector Search:** The expanded query is converted to a vector and used to find the top 10 most relevant document chunks in Qdrant.
3.  **Re-ranking:** The LLM analyzes the 10 retrieved chunks and selects the top 3 that are most relevant to the specific vehicle and symptom.
4.  **Generation:** The selected chunks are fed into the `llama3.1` model as context to generate a final, localized Turkish diagnosis and solution plan.

## 📂 Project Structure

The solution is organized into four projects:

### `FaultDiagnosis.Core`
Contains the domain entities and abstractions. It has no dependencies on external libraries.
*   **Entities:** `DocumentChunk` (stores text, embedding, and metadata).
*   **Interfaces:** `IVectorStore`, `ILLMClient`, `IDocumentProcessor`.
*   **Configuration:** `FaultDiagnosisSettings`.

### `FaultDiagnosis.Infrastructure`
Implements the interfaces defined in Core.
*   **Ollama:** `OllamaClient` handles HTTP communication with the local Ollama instance for embeddings and completion.
*   **Qdrant:** `QdrantVectorStore` manages vector storage and retrieval.
*   **Services:** `TextDocumentProcessor` handles file reading and metadata extraction.

### `FaultDiagnosis.API`
The entry point for the application.
*   **Controllers:** `DiagnosisController` orchestrates the entire RAG flow (Expansion -> Search -> Re-ranking -> Generation).
*   **Program.cs:** Configures Dependency Injection, Logging (Serilog), and Swagger.

### `FaultDiagnosis.DataPipeline`
A console application responsible for populating the vector database.
*   It scans the `docs/` directory, processes new files, and upserts them into Qdrant.

## 🛠️ Technologies

*   **.NET 10**
*   **Ollama** (Llama 3.1 & Nomic Embed Text)
*   **Qdrant** (Vector Database)
*   **Docker** (Containerization)

## 📋 Prerequisites

*   [Docker Desktop](https://www.docker.com/products/docker-desktop/)
*   [Ollama](https://ollama.com/) installed locally.
*   Pull required models:
    ```bash
    ollama pull llama3.1
    ollama pull nomic-embed-text
    ```

## 🚀 How to Run

1.  **Start the System:**
    ```bash
    docker-compose up -d --build
    ```

2.  **Ingest Data:**
    The system automatically processes text files in the `docs/` folder. If you add new files, restart the pipeline:
    ```bash
    docker-compose restart pipeline
    ```

## 📡 Usage

Send a POST request to the API to get a diagnosis.

*   **URL:** `http://localhost:8080/api/diagnosis`
*   **Method:** `POST`
*   **Content-Type:** `application/json`

**Request Body:**
```json
{
  "vehicleInfo": "Renault Clio 2017",
  "symptom": "Engine is shaking and check engine light is on."
}
```

**Response:**
```json
{
    "diagnosis": "**Possible Causes**\n* Ignition coil failure...\n\n**Solution Steps**\n1. Check the spark plugs...",
    "relatedDocuments": [
        "renault_clio_manual.txt"
    ]
}
```
