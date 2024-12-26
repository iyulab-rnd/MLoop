# MLoop Azure Container Apps/Jobs Architecture

## Overview

MLoop�� Azure Container Apps�� Container Jobs�� Ȱ���Ͽ� ML ���� �н��� ������ ó���ϴ� �ý����Դϴ�.

- **MLoop.Api**: Azure Container App���� ����
- **MLoop.Worker**: Azure Container Job���� ����
- **Azure Queue Storage**: Worker�� Auto-scaling Ʈ���ŷ� Ȱ��

## Architecture Components

### 1. MLoop.Api (Container App)

- HTTP API�� �����Ͽ� �ó����� ����, �۾� ����, ���� ��ȸ ���� ó��
- ���Ͻý���(Azure Files)�� ���� �۾� ���¿� ��Ÿ������ ����
- ���ο� �۾��� ������ �� Azure Queue�� scaling Ʈ���� �޽��� ����
- ���������� ����Ǵ� ���񽺷� �

```mermaid
flowchart LR
    Client --> Api[MLoop.Api]
    Api --> Files[(Azure Files)]
    Api --> Queue[(Azure Queue)]
```

### 2. MLoop.Worker (Container Job)

- Event Ʈ���� Ÿ���� Container Job���� ����
- Azure Queue�� �޽����� scaling Ʈ���ŷ� Ȱ��
- ���� �۾��� ���Ͻý��� ������� ó��
- ���信 ���� 0~N���� �ν��Ͻ��� �ڵ� Ȯ��/���

```mermaid
flowchart LR
    Queue[(Azure Queue)] --> Jobs[Container Jobs]
    Jobs --> Files[(Azure Files)]
    subgraph Jobs
        Worker1[Worker]
        Worker2[Worker]
        Worker3[Worker]
    end
```

### 3. Azure Queue Storage�� ����

Azure Queue�� **�������� �۾� �й谡 �ƴ� Worker�� Auto-scaling Ʈ���ŷθ� Ȱ��**�˴ϴ�.

- Queue �޽��� �����ֱ�:
  1. API�� ���ο� Waiting ���� �۾� ���� �� �޽��� ����
  2. TTL(Time To Live)�� 1������ �����Ͽ� ������ �޽����� ���� ���ʿ��� scaling ����
  3. Worker�� �޽����� �����ϸ� ��� ����

```mermaid
sequenceDiagram
    participant API
    participant Queue
    participant Worker
    participant Files
    
    API->>Files: �� �۾� ���� (Waiting ����)
    API->>Queue: Scaling Ʈ���� �޽��� ���� (TTL: 1��)
    Queue->>Worker: �۾� ���� �˸�
    Worker->>Queue: �޽��� ����
    Worker->>Files: ���Ͻý��ۿ��� ���� �۾� �˻�
    Worker->>Files: �۾� ó�� �� ���� ������Ʈ
```

### 4. �۾� ó�� �帧

1. **�۾� ����**:
   - API�� ���� �� �۾� ����
   - ���Ͻý��ۿ� �۾� ��Ÿ������ ����
   - Queue�� scaling Ʈ���� �޽��� ����

2. **Worker Scaling**:
   - Queue�� �޽����� ������ Azure Container Jobs�� Worker �ν��Ͻ� ����
   - �ν��Ͻ��� Queue Length�� ���� �ڵ����� Ȯ��/���

3. **�۾� ó��**:
   - Worker�� ť �޽��� ��ü�� �����ϰ� ���Ͻý��ۿ��� �۾� �˻�
   - `FindAndClaimNextJobAsync()`�� ���� Waiting ������ �۾��� ����
   - �۾� ó�� �� ���Ͻý��ۿ� ��� ����

## Deployment Configuration

### 1. Container App (MLoop.Api)

```yaml
name: mloop-api
type: Microsoft.App/containerApps
properties:
  configuration:
    ingress:
      external: true
      targetPort: 80
  template:
    containers:
    - name: api
      image: ${REGISTRY}/mloop-api:${TAG}
```

### 2. Container Job (MLoop.Worker)

```yaml
name: mloop-worker
type: Microsoft.App/jobs
properties:
  configuration:
    triggerType: Event
    replicaTimeout: 3600
    scale:
      minExecutions: 0
      maxExecutions: 10
      rules:
        - name: queue
          type: azure-queue
          metadata:
            queueName: mloop-scaling-queue
            queueLength: "1"
```

## ����

1. **ȿ������ ���ҽ� ����**
   - �۾��� ���� ���� Worker �ν��Ͻ��� 0���� ����
   - �۾����� ���� �ڵ����� Ȯ��/���

2. **������**
   - Queue�� scaling �뵵�θ� ���Ǿ� Queue ��� �ÿ��� �ٽ� ��� ���� �ּ�ȭ
   - ���Ͻý��� ����� �������� �۾� ���� ����

3. **��� ����ȭ**
   - ������ auto-scaling���� ���ʿ��� ���ҽ� ��� ����
   - Container Job�� event-driven Ư�� Ȱ��

4. **Ȯ�强**
   - �۾� �������� �ٸ� Worker Ǯ ���� ����
   - ó������ ���� ������ scaling ����

## �������

1. **Azure Files ����**
   - �ټ��� Worker�� ���ÿ� ���Ͻý��ۿ� ������ ���� ���� ���
   - Premium ���� ���� ��� ����

2. **�۾� �ߺ� ����**
   - Worker �� �۾� ���� �� Race Condition ����
   - ���Ͻý��� ��� Lock ��Ŀ���� Ȱ��

3. **����͸�**
   - Container Apps/Jobs ��Ʈ�� ����
   - Application Insights ����
   - �۾� ó�� ���� �� ���� ����͸�

4. **���**
   - Container Instance ��뷮
   - Azure Files ���丮�� ���
   - Queue Storage Ʈ����� ���