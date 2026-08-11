# hCS

C# 실시간 게임 서버 - 네트워크 계층부터 AOI, 부하 측정까지 구현.


<p align="center">
  <img src="https://github.com/user-attachments/assets/d826c3fb-aabe-41ae-8717-4f237dcd32f3" width="416" alt="hCS 데모 — 윈폼 클라이언트가 자기 시야(3×3 셀)만 렌더">
</p>

## 기술 스택
- C#(.NET10)
- MemoryPack
- WinForms

## 특징
- Socket + System.IO.Pipelines 커스텀 네트워크 계층
- IO ↔ Zone 스레드 분리 (Zone 단일 스레드 + 커맨드큐 → Zone 로직 락 프리)
- 헤드리스 봇 부하 테스트 + 윈폼 클라이언트

## 측정 결과
> 루프백 / 3,000 커넥션 / 20Hz 기준

| 지표 | before | after | 방법 |
|---|---|---|---|
| 틱당 할당 | 10.5MB | 0.70MB | 송신 버퍼를 수신자끼리 공유 (15배↓) |
| Gen2 GC | 12회 | 0회 | AOI 도입으로 브로드캐스트 축소 |
- 3,000 커넥션 이동 처리 중에도 틱 20~21Hz 유지. 애플리케이션 유실 0


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
