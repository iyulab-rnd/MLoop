# MLoop

MLoop 는 머신러닝 모델의 학습, 평가, 예측을 관리하는 서비스입니다. 시나리오 기반으로 데이터와 모델을 관리하며, 효율적인 ML 워크플로우를 지원합니다.

---

MLoop는 엔드-투-엔드 MLOps를 제공하는 통합 플랫폼입니다. 데이터 수집부터 모델 배포, 모니터링까지 머신러닝 모델의 전체 라이프사이클을 자동화합니다. RESTful API와 웹 기반 사용자 인터페이스를 통해 ML 워크플로우를 관리하며, 시나리오 기반의 프로젝트 구조로 실험을 체계적으로 구성할 수 있습니다.

MLoop는 지속적인 모델 개선 파이프라인을 구축합니다. 다양한 트리거(시간 기반, 데이터 드리프트 감지, 성능 임계값 등)에 의해 자동으로 모델을 재학습하고, 새로운 모델을 평가하여 자동으로 배포하는 MLOps 환경을 제공합니다.

## 주요 기능

- 시나리오 기반 ML 프로젝트 관리
- 데이터 파일 업로드 및 관리
- ML 모델 학습 및 평가
- 학습된 모델을 사용한 예측 API 제공
- 작업 상태 모니터링 및 로그 관리
- 최적 모델 자동 선택
- YAML 기반의 워크플로우 정의
- 자동화된 모델 재학습 트리거
  - 시간 기반 스케줄링
  - 데이터 드리프트 감지
  - 성능 메트릭 기반 트리거
- 모델 버전 관리 및 롤백
- A/B 테스트 지원
- 실시간 모델 모니터링
- 예측 결과 피드백 수집
- 자동화된 모델 배포 파이프라인
- CI/CD 통합
- 분산 학습 지원

## 시스템 요구사항

- .NET 9.0 이상
- Docker (컨테이너 배포 시)
- Azure Storage Account (선택사항 - 스케일링 큐 사용 시)

## ML 시나리오

- Classification(분류)  
- Regression(회귀)  
- Recommendation(추천)  
- Image Classification(이미지 분류)  
- Text Classification(텍스트 분류)  
- Forecasting(예측)  
- Object Detection(물체 감지)
- Anomaly Detection(이상 탐지)

## 설치 방법

### Docker를 이용한 설치

1. 도커 이미지 빌드
```bash
docker build -t mloop-api .
```

2. 컨테이너 실행
```bash
docker run -d \
  -p 80:80 \
  -v /path/to/data:/var/data \
  -e Storage__BasePath=/var/data/mloop \
  -e ConnectionStrings__QueueConnection="<your-queue-connection-string>" \
  mloop-api
```

### 직접 실행

1. 프로젝트 빌드
```bash
dotnet build
```

2. 서비스 실행
```bash
dotnet run --project MLoop.Api
```

## 환경 설정

### 주요 설정 항목

- `Storage:BasePath`: 데이터 저장 경로
- `ConnectionStrings:QueueConnection`: Azure Storage Queue 연결 문자열
- `Logging:LogLevel:Default`: 기본 로그 레벨

### appsettings.json 예시
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

## API 엔드포인트

### 시나리오 관리

- `GET /api/scenarios`: 시나리오 목록 조회
- `POST /api/scenarios`: 새 시나리오 생성
- `GET /api/scenarios/{scenarioId}`: 시나리오 상세 조회
- `PUT /api/scenarios/{scenarioId}`: 시나리오 정보 수정
- `DELETE /api/scenarios/{scenarioId}`: 시나리오 삭제

### 데이터 관리

- `GET /api/scenarios/{scenarioId}/data`: 데이터 파일 목록 조회
- `POST /api/scenarios/{scenarioId}/data`: 데이터 파일 업로드
- `DELETE /api/scenarios/{scenarioId}/data/{filePath}`: 데이터 파일 삭제

### 모델 관리

- `GET /api/scenarios/{scenarioId}/models`: 모델 목록 조회
- `GET /api/scenarios/{scenarioId}/models/{modelId}`: 모델 상세 조회
- `GET /api/scenarios/{scenarioId}/models/{modelId}/metrics`: 모델 메트릭 조회
- `GET /api/scenarios/{scenarioId}/models/best-model`: 최적 모델 조회
- `POST /api/scenarios/{scenarioId}/models/cleanup`: 불필요한 모델 정리

### 예측

- `POST /api/scenarios/{scenarioId}/predict`: 예측 수행
- `GET /api/scenarios/{scenarioId}/predictions`: 예측 이력 조회
- `GET /api/scenarios/{scenarioId}/predictions/{predictionId}`: 예측 결과 조회
- `POST /api/scenarios/{scenarioId}/predictions/cleanup`: 완료된 예측 정리

### 워크플로우

- `GET /api/scenarios/{scenarioId}/workflows/train`: 학습 워크플로우 조회
- `POST /api/scenarios/{scenarioId}/workflows/train`: 학습 워크플로우 업데이트
- `GET /api/scenarios/{scenarioId}/workflows/predict`: 예측 워크플로우 조회
- `POST /api/scenarios/{scenarioId}/workflows/predict`: 예측 워크플로우 업데이트

## 데이터 구조

### 시나리오 디렉토리 구조
```
scenarios/
  ├── {scenarioId}/
  │   ├── scenario.json    # 시나리오 메타데이터
  │   ├── data/           # 데이터 파일
  │   ├── models/         # 학습된 모델
  │   ├── jobs/           # 작업 상태 및 로그
  │   ├── predictions/    # 예측 결과
  │   └── workflows/      # 워크플로우 정의
  └── worker.lock         # 작업 처리 잠금 파일
```

## 개발 가이드

### 새로운 ML 타입 추가

1. `ScenarioService.cs`의 `IsValidMLType` 메서드에 새로운 타입 추가
2. 해당 타입에 맞는 워크플로우 템플릿 구현
3. 필요한 경우 새로운 메트릭 수집 로직 추가

### 워크플로우 정의 예시

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

## 라이선스

이 프로젝트는 MIT 라이선스로 배포됩니다.

## 기여하기

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request