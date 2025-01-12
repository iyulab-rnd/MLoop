# MLoop

MLoop �� �ӽŷ��� ���� �н�, ��, ������ �����ϴ� �����Դϴ�. �ó����� ������� �����Ϳ� ���� �����ϸ�, ȿ������ ML ��ũ�÷ο츦 �����մϴ�.

---

MLoop�� ����-��-���� MLOps�� �����ϴ� ���� �÷����Դϴ�. ������ �������� �� ����, ����͸����� �ӽŷ��� ���� ��ü ����������Ŭ�� �ڵ�ȭ�մϴ�. RESTful API�� �� ��� ����� �������̽��� ���� ML ��ũ�÷ο츦 �����ϸ�, �ó����� ����� ������Ʈ ������ ������ ü�������� ������ �� �ֽ��ϴ�.

MLoop�� �������� �� ���� ������������ �����մϴ�. �پ��� Ʈ����(�ð� ���, ������ �帮��Ʈ ����, ���� �Ӱ谪 ��)�� ���� �ڵ����� ���� ���н��ϰ�, ���ο� ���� ���Ͽ� �ڵ����� �����ϴ� MLOps ȯ���� �����մϴ�.

## �ֿ� ���

- �ó����� ��� ML ������Ʈ ����
- ������ ���� ���ε� �� ����
- ML �� �н� �� ��
- �н��� ���� ����� ���� API ����
- �۾� ���� ����͸� �� �α� ����
- ���� �� �ڵ� ����
- YAML ����� ��ũ�÷ο� ����
- �ڵ�ȭ�� �� ���н� Ʈ����
  - �ð� ��� �����ٸ�
  - ������ �帮��Ʈ ����
  - ���� ��Ʈ�� ��� Ʈ����
- �� ���� ���� �� �ѹ�
- A/B �׽�Ʈ ����
- �ǽð� �� ����͸�
- ���� ��� �ǵ�� ����
- �ڵ�ȭ�� �� ���� ����������
- CI/CD ����
- �л� �н� ����

## �ý��� �䱸����

- .NET 9.0 �̻�
- Docker (�����̳� ���� ��)
- Azure Storage Account (���û��� - �����ϸ� ť ��� ��)

## ML �ó�����

- Classification(�з�)  
- Regression(ȸ��)  
- Recommendation(��õ)  
- Image Classification(�̹��� �з�)  
- Text Classification(�ؽ�Ʈ �з�)  
- Forecasting(����)  
- Object Detection(��ü ����)
- Anomaly Detection(�̻� Ž��)

## ��ġ ���

### Docker�� �̿��� ��ġ

1. ��Ŀ �̹��� ����
```bash
docker build -t mloop-api .
```

2. �����̳� ����
```bash
docker run -d \
  -p 80:80 \
  -v /path/to/data:/var/data \
  -e Storage__BasePath=/var/data/mloop \
  -e ConnectionStrings__QueueConnection="<your-queue-connection-string>" \
  mloop-api
```

### ���� ����

1. ������Ʈ ����
```bash
dotnet build
```

2. ���� ����
```bash
dotnet run --project MLoop.Api
```

## ȯ�� ����

### �ֿ� ���� �׸�

- `Storage:BasePath`: ������ ���� ���
- `ConnectionStrings:QueueConnection`: Azure Storage Queue ���� ���ڿ�
- `Logging:LogLevel:Default`: �⺻ �α� ����

### appsettings.json ����
```json
{
  "Storage": {
    "BasePath": "/var/data/mloop"
  },
  "ConnectionStrings": {
    "QueueConnection": "your-connection-string"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## API ��������Ʈ

### �ó����� ����

- `GET /api/scenarios`: �ó����� ��� ��ȸ
- `POST /api/scenarios`: �� �ó����� ����
- `GET /api/scenarios/{scenarioId}`: �ó����� �� ��ȸ
- `PUT /api/scenarios/{scenarioId}`: �ó����� ���� ����
- `DELETE /api/scenarios/{scenarioId}`: �ó����� ����

### ������ ����

- `GET /api/scenarios/{scenarioId}/data`: ������ ���� ��� ��ȸ
- `POST /api/scenarios/{scenarioId}/data`: ������ ���� ���ε�
- `DELETE /api/scenarios/{scenarioId}/data/{filePath}`: ������ ���� ����

### �� ����

- `GET /api/scenarios/{scenarioId}/models`: �� ��� ��ȸ
- `GET /api/scenarios/{scenarioId}/models/{modelId}`: �� �� ��ȸ
- `GET /api/scenarios/{scenarioId}/models/{modelId}/metrics`: �� ��Ʈ�� ��ȸ
- `GET /api/scenarios/{scenarioId}/models/best-model`: ���� �� ��ȸ
- `POST /api/scenarios/{scenarioId}/models/cleanup`: ���ʿ��� �� ����

### ����

- `POST /api/scenarios/{scenarioId}/predict`: ���� ����
- `GET /api/scenarios/{scenarioId}/predictions`: ���� �̷� ��ȸ
- `GET /api/scenarios/{scenarioId}/predictions/{predictionId}`: ���� ��� ��ȸ
- `POST /api/scenarios/{scenarioId}/predictions/cleanup`: �Ϸ�� ���� ����

### ��ũ�÷ο�

- `GET /api/scenarios/{scenarioId}/workflows/train`: �н� ��ũ�÷ο� ��ȸ
- `POST /api/scenarios/{scenarioId}/workflows/train`: �н� ��ũ�÷ο� ������Ʈ
- `GET /api/scenarios/{scenarioId}/workflows/predict`: ���� ��ũ�÷ο� ��ȸ
- `POST /api/scenarios/{scenarioId}/workflows/predict`: ���� ��ũ�÷ο� ������Ʈ

## ������ ����

### �ó����� ���丮 ����
```
scenarios/
  ������ {scenarioId}/
  ��   ������ scenario.json    # �ó����� ��Ÿ������
  ��   ������ data/           # ������ ����
  ��   ������ models/         # �н��� ��
  ��   ������ jobs/           # �۾� ���� �� �α�
  ��   ������ predictions/    # ���� ���
  ��   ������ workflows/      # ��ũ�÷ο� ����
  ������ worker.lock         # �۾� ó�� ��� ����
```

## ���� ���̵�

### ���ο� ML Ÿ�� �߰�

1. `ScenarioService.cs`�� `IsValidMLType` �޼��忡 ���ο� Ÿ�� �߰�
2. �ش� Ÿ�Կ� �´� ��ũ�÷ο� ���ø� ����
3. �ʿ��� ��� ���ο� ��Ʈ�� ���� ���� �߰�

### ��ũ�÷ο� ���� ����

```yaml
steps:
  - name: preprocess
    type: data_preprocessing
    config:
      inputPath: "data/raw.csv"
      outputPath: "data/processed.csv"
      
  - name: train
    type: model_training
    needs: [preprocess]
    config:
      algorithm: "lightgbm"
      parameters:
        learningRate: 0.1
        numLeaves: 31
```

## ���̼���

�� ������Ʈ�� MIT ���̼����� �����˴ϴ�.

## �⿩�ϱ�

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request