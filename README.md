# Fault Diagnosis Assistant (Arıza Teşhis Asistanı)

Bu proje, **RAG (Retrieval-Augmented Generation)** mimarisi kullanarak araç arızalarını teşhis eden bir yapay zeka asistanıdır. Kullanıcı şikayetlerini ve araç bilgilerini alır, ilgili teknik dökümanları tarar ve LLM (Llama 3.1) kullanarak çözüm önerileri sunar.

## 🚀 Teknolojiler

*   **.NET 8**: Backend API ve Data Pipeline.
*   **Ollama**: Lokal LLM (Llama 3.1) ve Embedding (nomic-embed-text) servisi.
*   **Qdrant**: Vektör veritabanı (Docker üzerinde çalışır).
*   **Docker**: Qdrant servisi için.

## ⚙️ Gereksinimler

1.  **.NET 8 SDK**: [İndir](https://dotnet.microsoft.com/download/dotnet/8.0)
2.  **Docker Desktop**: Qdrant'ı çalıştırmak için.
3.  **Ollama**: [İndir](https://ollama.com/)
    *   Gerekli modelleri çekin:
        ```bash
        ollama pull llama3.1
        ollama pull nomic-embed-text
        ```

## 🛠️ Kurulum ve Çalıştırma

### 1. Vektör Veritabanını Başlatın
Proje dizininde terminali açın ve Qdrant'ı ayağa kaldırın:
```bash
docker-compose up -d
```

### 2. Veri Yükleme (Data Pipeline)
Dökümanları işleyip vektör veritabanına yüklemek için:
1.  `docs/` klasörüne `.txt` formatında araç kılavuzlarını veya teknik dökümanları ekleyin (Örnek: `renault_clio_manual.txt`).
2.  Pipeline'ı çalıştırın:
    ```bash
    dotnet run --project FaultDiagnosis.DataPipeline/FaultDiagnosis.DataPipeline.csproj
    ```

### 3. API'yi Başlatın
Web API servisini başlatmak için:
```bash
dotnet run --project FaultDiagnosis.API/FaultDiagnosis.API.csproj --urls=http://localhost:5000
```

## 📡 Kullanım

API çalışırken `POST` isteği göndererek teşhis alabilirsiniz.

**Endpoint:** `http://localhost:5000/api/diagnosis`

**Örnek İstek (JSON):**
```json
{
  "symptom": "Gaz yememe ve titreme var, motor ışığı da yanıyor.",
  "vehicleInfo": "Renault Clio 2017"
}
```

**Örnek Yanıt:**
```json
{
    "diagnosis": "**Olası Sebepler**\n* Ateşleme bobini arızası...\n\n**Çözüm Adımları**\n1. Bujileri kontrol edin...",
    "relatedDocuments": [
        "renault_clio_manual.txt"
    ]
}
```

## 📂 Proje Yapısı

*   **FaultDiagnosis.Core**: Temel varlıklar (Entities) ve arayüzler (Interfaces).
*   **FaultDiagnosis.Infrastructure**: Ollama ve Qdrant entegrasyonları.
*   **FaultDiagnosis.API**: Dış dünyaya açılan REST API.
*   **FaultDiagnosis.DataPipeline**: Dökümanları okuyup vektörleştiren konsol uygulaması.
