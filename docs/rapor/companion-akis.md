# Companion Karar Akışı

Robotun karar akışı (PDF 5.2). Durum adları koddaki sınıflarla aynıdır: `IdleState`, `MoveToTargetState`, `PetInvestigateState`; labirent keşfi `MazeRaycastNavigator` bileşeniyle yapılır.

```mermaid
flowchart TD
    S([Oyun başlar]) --> F[Oyuncuyu takip et<br/>IdleState / MoveToTargetState]
    F --> Q1{Oyuncu E ile<br/>komut verdi mi?}
    Q1 -->|hayır| F
    Q1 -->|evet: engel / şalter| I[PetInvestigateState]
    Q1 -->|evet: labirent| ME[Labirent girişine git]
    ME --> Q2{Labirent alanına<br/>girildi mi?}
    Q2 -->|hayır| ME
    Q2 -->|evet| M1[NavMesh düğüm keşfi durur<br/>MazeRaycastNavigator aktif]
    M1 --> M2[Raycast ile ızgara tabanlı keşif]
    M2 --> Q3{Şaltere 1,5 m<br/>yaklaşıldı mı?}
    Q3 -->|hayır| M2
    Q3 -->|evet| M3[Labirent algoritması kapanır<br/>kontrol PetInvestigateState'e geçer]
    M3 --> I
    I --> I1[NavMesh düğüm keşfi:<br/>bilinmeyen noktaları tara]
    I1 --> Q4{Engelle ilişkili<br/>şalter bulundu mu?}
    Q4 -->|hayır| I1
    Q4 -->|evet| I2[Şaltere git, etkinleştir<br/>engel açılır]
    I2 --> F
```
