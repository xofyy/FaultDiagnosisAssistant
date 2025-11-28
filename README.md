# Fault Diagnosis Assistant

This project is an AI assistant that diagnoses vehicle faults using **RAG (Retrieval-Augmented Generation)** architecture. It takes user complaints and vehicle information, scans relevant technical documents, and provides solution suggestions using an LLM (Llama 3.1).

## 🚀 Technologies

*   **.NET 10**: Backend API and Data Pipeline.
*   **Ollama**: Local LLM (Llama 3.1) and Embedding (nomic-embed-text) service.
*   **Qdrant**: Vector database (runs on Docker).
*   **Docker**: For running the Qdrant service.

## ⚙️ Requirements

1.  **.NET 10 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
2.  **Docker Desktop**: Required to run Qdrant.
3.  **Ollama**: [Download](https://ollama.com/)
    *   Pull the required models:
        ```bash
        ollama pull llama3.1
        ollama pull llama3.1
        ollama pull nomic-embed-text
        ```

### Optional: Cloud LLM (OpenAI)
To use OpenAI instead of Ollama, update `appsettings.json` or use environment variables:
```json
"FaultDiagnosis": {
  "LLMProvider": "OpenAI",
  "ApiKey": "sk-...",
  "GenerationModel": "gpt-4o",
  "EmbeddingModel": "text-embedding-3-small"
}
```

## 🛠️ Installation and Running

### 1. Start the Application (Docker Compose)
Start the entire system (API, Pipeline, Qdrant) with a single command:
```bash
docker-compose up -d --build
```

### 2. Data Ingestion
The Pipeline service automatically processes files in the `docs/` folder. If you add new files, you can restart the pipeline service:
```bash
docker-compose restart pipeline
```

## 📡 Usage

You can get a diagnosis by sending a `POST` request while the API is running.

**Endpoint:** `http://localhost:8080/api/diagnosis`

**Example Request (JSON):**
```json
{
  "symptom": "Engine is misfiring and shaking, check engine light is on.",
  "vehicleInfo": "Renault Clio 2017"
}
```

**Example Response:**
```json
{
    "diagnosis": "**Possible Causes**\n* Ignition coil failure...\n\n**Solution Steps**\n1. Check the spark plugs...",
    "relatedDocuments": [
        "renault_clio_manual.txt"
    ]
}
```

## 📂 Project Structure

*   **FaultDiagnosis.Core**: Core entities and interfaces.
*   **FaultDiagnosis.Infrastructure**: Ollama and Qdrant integrations.
*   **FaultDiagnosis.API**: REST API exposed to the outside world.
*   **FaultDiagnosis.DataPipeline**: Console application that reads and vectorizes documents.
