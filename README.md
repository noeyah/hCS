# hCS

C# 실시간 게임 서버 - 네트워크 계층부터 AOI, 부하 측정까지 구현.

> 🔗 다른 프로젝트<br>
> **[hCPP](https://github.com/noeyah/hCPP)** - C++ IOCP 서버 라이브러리 + C# 네트워크 라이브러리


<p align="center">
  <img src="https://github.com/user-attachments/assets/d826c3fb-aabe-41ae-8717-4f237dcd32f3" width="416" alt="hCS 데모 — 윈폼 클라이언트가 자기 시야(3×3 셀)만 렌더">
</p>
<p align="center"><sub>윈폼 클라이언트 – 자기 시야(3×3 셀)만 렌더</sub></p>

<br>

## 🔍 요약

| 구분 | 내용 |
|---|---|
| 핵심 | System.IO.Pipelines 기반 네트워크 + IO 스레드풀 / Zone 단일 스레드 분리 + 셀 기반 AOI |
| 성과 | 3,000 커넥션 이동 중 20Hz 유지, 틱당 할당 10.5MB → 0.7MB, Gen2 GC 12회 → 0회 |
| 언어 | C# (.NET 10) |
| 라이브러리 | [MemoryPack](https://github.com/Cysharp/MemoryPack) (패킷 직렬화), WinForms (테스트 클라이언트) |
| 환경 | Windows, Visual Studio 2026 |
| 기간 | 2026년 7월 |
| AI 사용 | 설계, 구현 전반에 활용 ([프로젝트별 범위](#프로젝트-구성-및-구현-범위)) |

<br>

## 프로젝트 구성 및 구현 범위

| 프로젝트 | 역할 | 구현 범위 |
|---|---|---|
| **Core** | Socket과 System.IO.Pipelines 기반 네트워크 라이브러리 | AI 협업 설계, 직접 작성 + AI 작성 |
| **Shared** | 클라이언트와 서버의 공통 데이터 및 MemoryPack 프로토콜 | 공통 코드 |
| **TestServer** | Core를 사용하는 게임 서버, Zone 및 AOI 구현 | AI 협업 설계, 직접 작성 + AI 작성 |
| **TestClient** | AOI 동작을 시각적으로 확인하는 WinForms 클라이언트 | AI 작성 |
| **TestDummy** | 다중 접속과 이동 부하를 생성하는 헤드리스 봇 | AI 작성 |

## Core 설계

1. **비동기 수신**
   - Socket으로 받은 데이터를 `PipeWriter`에 기록하고 `PipeReader`에서 패킷 단위로 소비
2. **패킷 프레이밍**
   - `[2바이트 길이][payload]` 형식으로 TCP 스트림의 분할 및 병합 수신 처리
3. **송신 순서 보장**
   - 세션별 `Channel`과 단일 송신 루프로 패킷 순서 보장

## TestServer 설계

1. **IO / 로직 스레드 분리**
   - 네트워크 IO는 스레드풀, 게임 상태 변경은 전용 Zone 스레드에서 처리
   - 상태 변경 요청은 `ConcurrentQueue`로 Zone에 전달 → Zone 상태는 단일 스레드만 접근해 락 없음
2. **셀 기반 AOI**
   - 현재 셀 + 인접 8칸(3×3)만 관심 영역으로 관리
   - 플레이어별 이전 시야와 새 3×3을 비교해 Enter/Leave 대상만 전송
3. **이동 브로드캐스트 묶음**
   - 같은 셀의 이동 정보를 하나의 패킷으로 묶어 인접 셀에 전송
4. **패킷 공유**
   - 직렬화와 프레이밍을 한 번만 수행한 패킷을 여러 세션이 공유해 브로드캐스트 할당 감소

### 스레드 모델

```mermaid
sequenceDiagram
    participant C as 클라이언트
    box IO 스레드풀
        participant S as [Core] Session
        participant H as [Game] GameSession<br/>PacketHandler
    end
    participant Q as Zone._commands<br/>(ConcurrentQueue)
    box Zone 단일 스레드
        participant Z as Zone
    end
    participant SC as Session._sendChannel

    C->>S: Recv
    S->>H: OnMessage
    alt 가벼운 패킷 (LoginReq 등)
        H->>H: IO 스레드에서 바로 처리
    else Zone 상태 변경 (EnterZone, Move 등)
        H->>Q: Zone.Push (Write)
        Note over H: IO 스레드는 여기서 반환
    end
    loop 매 틱
        Z->>Q: Dequeue
        Z->>Z: 커맨드 처리 → Update
        Note over Z: Zone 상태는 이 스레드만 접근 → 락 없음
        Z->>SC: Broadcast → Write
    end
    SC->>C: SendAsync (Read)
```

## 측정 결과
> Release / 루프백 / 3,000 커넥션 / 지속적으로 이동 / 목표 20Hz

| 지표 | before | after | 방법 |
|---|---|---|---|
| 틱당 할당 | 10.5MB | 0.70MB | 송신 버퍼를 수신자끼리 공유 (15배↓) |
| Gen2 GC | 12회 | 0회 | AOI 도입으로 브로드캐스트 축소 |

- 3,000 커넥션 이동 처리 중에도 목표 틱 20Hz 유지

## 버그 해결: AOI 유령 버그

셀 경계를 넘을 때 일부 더미가 화면에 남는 버그.
- **원인:** 같은 틱에 두 명이 셀을 이동하면 Leave를 '이동 후 셀' 기준으로 계산해 서로를 못 찾음
- **해결:** 플레이어별 이전 시야(`HashSet`)와 새 3×3을 비교하도록 변경 → 처리 순서와 무관, 3,000 더미로 소멸 확인

## 실행
```bash
# 빌드
dotnet build hCS.slnx -c Release

# 서버
dotnet run --project TestServer -c Release

# 윈폼 클라이언트
dotnet run --project TestClient -c Release

# 부하 봇
dotnet run --project TestDummy -c Release
```
> 호스트/포트/커넥션 수 등 설정값은 각 프로젝트의 `appsettings.json`에서 조정 가능.
