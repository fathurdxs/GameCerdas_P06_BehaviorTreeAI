# Praktikum 6 — NPC Behavior Tree & Utility-Based Decision AI

Unity 6 (6000.3.23f1) · URP 17.3.0 · AI Navigation 2.0.14

## Cara membuka

1. Unity Hub → **Add → Add project from disk** → pilih folder `GameCerdas_P06_BehaviorTreeAI`.
2. Menu **Praktikum 06 → Build Scene P06_BehaviorTree** membuat (atau membuat ulang)
   `Assets/Scenes/P06_BehaviorTree.unity`, termasuk layer `Obstacle`, material, dan Bake NavMesh.

   Perhatian: menu ini **menimpa** scene tersebut, jadi jangan dijalankan lagi setelah
   kamu mengedit scene secara manual.
3. Setelah mengubah dinding/obstacle statis: **Praktikum 06 → Bake Ulang NavMesh (scene aktif)**,
   atau tombol Bake di komponen NavMeshSurface pada GameObject `Navigation`.
4. Eksperimen 1–5 (modul bagian 57–61) dijalankan dari komponen `EnemyBTController` saat Play —
   tiga checkbox di header **Eksperimen** dan tiga preset di context menu komponen.
   Lihat bagian "Eksperimen 1–5" di bawah.
5. Opsional (Tugas Pengembangan 80D): **Praktikum 06 → Tambah Enemy Utility (Bonus D)**
   menambahkan Enemy kedua berwarna ungu yang memakai Utility AI, untuk dibandingkan
   berdampingan dengan Enemy merah yang memakai Behavior Tree.

Kontrol saat Play: **WASD / arrow key** untuk menggerakkan Player.

## Struktur

```
Assets/Scripts/BehaviorTree/   BTNode, SelectorNode, SequenceNode, ConditionNode,
                               ActionNode, CooldownDecorator
Assets/Scripts/AI/             EnemyBTController        (bagian wajib)
                               UtilityEnemyController   (bagian II / bonus 80D)
Assets/Scripts/Player/         SimplePlayerController, PlayerHealth
Assets/Scripts/Editor/         Praktikum06SceneBuilder  (pembuat scene)
Assets/Scenes/                 P06_BehaviorTree.unity
                               P06_BehaviorTree_NavMeshData.asset (hasil Bake)
Assets/Materials/              P06_Ground, P06_Wall, P06_Player, P06_Enemy,
                               P06_Waypoint, P06_SafePoint
```

Isi scene:

```
P06_BehaviorTree
├── Environment
│   ├── Ground          Plane  (0, 0, 0)   scale (3, 1, 3)
│   ├── Wall01          Cube   (0, 1, 3)   scale (6, 2, 1)   layer Obstacle
│   ├── Wall02          Cube   (-4, 1, -2) scale (1, 2, 5)   layer Obstacle
│   └── Wall03          Cube   (4, 1, -4)  scale (1, 2, 5)   layer Obstacle
├── Player              Capsule (0, 1, 0)  layer Player
├── Enemy               Capsule (6, 1, 5)  NavMeshAgent + EnemyBTController
├── PatrolPoints
│   ├── Waypoint01      (-7, 0, -7)
│   ├── Waypoint02      (-7, 0, 7)
│   ├── Waypoint03      (7, 0, 7)
│   └── Waypoint04      (7, 0, -7)
├── SafePoint           (-10, 0, -10)
├── Navigation          NavMeshSurface (sudah di-Bake)
├── Main Camera
└── Directional Light
```

Behavior Tree yang terbentuk di `BuildBehaviorTree()`:

```
Root Selector
├── Sequence — Flee
│   ├── Condition  IsHealthLow
│   └── Action     Flee
├── Sequence — Attack
│   ├── Condition  CanSeePlayer
│   ├── Condition  IsPlayerInAttackRange
│   └── Cooldown (1.5s)
│       └── Action AttackPlayer
├── Sequence — Chase
│   ├── Condition  CanSeePlayer
│   └── Action     ChasePlayer
└── Action — Patrol
```

## Perubahan kecil dari kode modul

Enam file framework Behavior Tree (`BTNode`, `SelectorNode`, `SequenceNode`, `ConditionNode`,
`ActionNode`, `CooldownDecorator`) ditulis **sama dengan modul**, tanpa perubahan.

`EnemyBTController` juga sama dengan modul, dengan satu tambahan: tiga toggle eksperimen
(bagian 57–59) dan tiga preset parameter (bagian 60–61), supaya Eksperimen 1–5 bisa dijalankan
tanpa meng-comment kode. **Ketiga toggle OFF secara default**, dan pada kondisi itu
`BuildBehaviorTree()` menyusun pohon yang persis sama dengan modul bagian 31 — urutan Selector,
kedua Condition di branch Attack, dan `CooldownDecorator` semuanya pada posisi aslinya.

Selain itu ada tiga hal di luar logika AI yang berbeda:

- **`NavMeshAgent.baseOffset = 1`.** Modul menaruh Enemy di `y = 1`, tetapi pivot capsule ada di
  tengah badan sementara NavMesh berada di `y = 0`. Tanpa offset ini agent melayang satu unit
  di atas NavMesh dan berisiko gagal ditempatkan.
- **`NavMeshModifier` (Ignore From Build) pada Player dan Enemy.** Keduanya bergerak, jadi
  collider-nya tidak boleh ikut di-Bake — kalau ikut, akan tertinggal lubang di NavMesh.
- **`SimplePlayerController` punya dua cabang input.** Template Universal 3D Unity 6 memasang
  Active Input Handling = *Input System Package (New)*, dan pada mode itu `Input.GetKey` melempar
  exception. File ini memakai preprocessor directive:

  ```csharp
  #if ENABLE_LEGACY_INPUT_MANAGER
      // kode Input.GetKey persis seperti modul
  #elif ENABLE_INPUT_SYSTEM
      // versi Keyboard.current
  #endif
  ```

  Jadi kode modul tetap ada di file dan akan dipakai kalau setting diubah ke *Both* / *Old*.
  Modul sendiri mengizinkan ini di catatan bagian 14.

Scene dibangun lewat Editor script, bukan disusun manual, karena `NavMeshSurface` dan hasil Bake
NavMesh tidak bisa dihasilkan dari luar Unity. Objek, posisi, dan nilai Inspector yang dihasilkan
sama dengan langkah manual modul bagian 9–41.

## Cara uji (modul bagian 42–48)

Select `Enemy` di Hierarchy sebelum Play dan biarkan tetap terpilih — gizmo Vision Range dan
Attack Range hanya muncul untuk objek yang ter-select. Amati field **Current Action** di Inspector.

| # | Pengujian | Langkah | Hasil yang diharapkan |
|---|---|---|---|
| 1 | Patrol | Play, jauhkan Player | `Current Action = PATROL`, Enemy berputar Waypoint01 → 02 → 03 → 04 → 01 |
| 2 | Chase | Dekati Player sampai masuk Vision Range, tapi jarak > 2 | `CHASE`, Enemy bergerak ke Player |
| 3 | Attack | Dekatkan sampai jarak ≤ 2 | `ATTACK`, Console: `Enemy attacks Player! Damage = 10` |
| 4 | Cooldown | Biarkan Player tetap dekat | Serangan berulang tiap 1,5 detik, bukan tiap frame |
| 5 | Obstacle | Berdiri di balik Wall, jarak tetap dekat | Enemy tidak melihat Player, kembali `PATROL` |
| 6 | Field of View | Berdiri tepat di belakang Enemy | Enemy tidak bereaksi; pindah ke depan → `CHASE` / `ATTACK` |
| 7 | Flee | Klik kanan komponen `EnemyBTController` → **Test Damage 25**, empat kali | Health 100 → 75 → 50 → 25; di 25 (< 30) `Current Action = FLEE`, Enemy menuju SafePoint |

Untuk Pengujian 7, `Test Damage 25` dan `Reset Health` ada di context menu komponen
(klik kanan pada header komponen, atau ikon tiga titik di kanan atas komponen).

## Checklist pengujian (modul bagian 82)

Sudah diverifikasi dari file project:

- [x] Project dapat dijalankan tanpa compile error
- [x] AI Navigation telah terpasang (2.0.14)
- [x] NavMesh telah dibuat (`P06_BehaviorTree_NavMeshData.asset`)
- [x] Enemy memiliki NavMeshAgent
- [x] Enemy memiliki empat waypoint
- [x] Selector bekerja berdasarkan prioritas (urutan child di `BuildBehaviorTree`)
- [x] Sequence berhenti jika condition gagal (`SequenceNode.Tick`)
- [x] Action yang membutuhkan waktu mengembalikan `Running` (Patrol, Chase, Flee)
- [x] Attack memiliki cooldown (`CooldownDecorator`, 1.5s)
- [x] Current Action dapat diamati (field `currentAction` di Inspector)
- [x] Vision Range dan Attack Range tergambar oleh Gizmos

Harus kamu centang sendiri setelah menjalankan Pengujian 1–7 di atas:

- [ ] Player dapat digerakkan
- [ ] Enemy Patrol ketika Player tidak terlihat
- [ ] Enemy Chase ketika Player terlihat
- [ ] Enemy Attack ketika Player dekat
- [ ] Enemy tidak melihat Player melalui obstacle
- [ ] Enemy Flee ketika health rendah
- [ ] Tidak terdapat NullReferenceException

---

# Tugas 6 — Analisis: FSM Praktikum 05 vs Behavior Tree Praktikum 06

> Catatan: bagian FSM di bawah mengacu pada rancangan FSM yang dijelaskan modul Praktikum 05
> dan modul Praktikum 06 bagian 5 dan 77. Kalau implementasi FSM kamu di Praktikum 05 berbeda
> (misalnya jumlah state atau cara transisinya), sesuaikan angka dan nama state di bagian ini.

Perilaku NPC pada kedua praktikum **identik**: Patrol, Chase, Attack, Flee, dengan perception
yang sama (jarak, field of view, raycast obstacle) dan navigasi yang sama (`NavMeshAgent`).
Yang berubah adalah arsitektur yang memilih perilaku itu.

### 1. Unit penyusunnya berbeda

Pada FSM, unit dasarnya adalah **state** dan **transition**. Kode bertanya "state aktif sekarang
apa?", lalu memeriksa daftar transisi yang keluar dari state itu.

Pada Behavior Tree, unit dasarnya adalah **node** yang mengembalikan `Success`, `Failure`, atau
`Running`. Kode bertanya "perilaku berprioritas tertinggi yang kondisinya terpenuhi sekarang apa?",
dan jawabannya dihitung ulang dari akar pohon setiap frame lewat satu panggilan `rootNode.Tick()`
di `Update()`.

### 2. Jumlah relasi yang harus ditulis

Ini perbedaan yang paling terasa saat menulis kodenya. FSM dengan empat state butuh transisi
eksplisit antar pasangan state. Untuk empat state, kemungkinan arah transisi ada 4 × 3 = 12, dan
pada praktiknya FSM Praktikum 05 menulis sekitar enam sampai delapan di antaranya
(Patrol→Chase, Chase→Attack, Attack→Chase, Chase→Patrol, Patrol→Flee, Chase→Flee, Attack→Flee).
Setiap state baru menambah kemungkinan transisi secara kuadratik.

Behavior Tree di praktikum ini tidak menulis satu pun transisi. `BuildBehaviorTree()` hanya
mendaftarkan empat branch ke dalam satu Selector, berurutan:

```csharp
rootNode = new SelectorNode(new List<BTNode>
{
    fleeSequence,
    attackSequence,
    chaseSequence,
    patrolAction
});
```

Perpindahan perilaku muncul sebagai efek samping dari evaluasi ulang, bukan sebagai aturan yang
ditulis. Menambah perilaku baru (misalnya Search Last Position) berarti menyisipkan satu branch
di posisi prioritas yang tepat — bukan menulis transisi dari dan ke setiap state lama.

### 3. Prioritas: implisit vs eksplisit

Pada FSM, prioritas tersebar di dalam kondisi transisi. Supaya Flee selalu mengalahkan Attack,
transisi `Attack → Flee` harus ada **dan** kondisi di dalam state Attack harus diperiksa dengan
urutan yang benar. Prioritasnya tidak terlihat di satu tempat.

Pada Behavior Tree, prioritas adalah **urutan child Selector** dan terbaca dalam satu layar.
Memindahkan `fleeSequence` ke posisi kedua langsung mengubah karakter NPC, tanpa menyentuh
kondisi apa pun. Inilah yang diuji oleh Eksperimen 1 modul bagian 57.

### 4. Status `Running`

FSM tidak punya konsep ini. Sebuah state "sedang berjalan" selama belum ada transisi yang
terpicu — informasinya implisit.

Behavior Tree membuatnya eksplisit. `ChasePlayer()` mengembalikan `Running` karena mengejar butuh
banyak frame, dan `SelectorNode` berhenti mengevaluasi child berikutnya begitu menerima `Running`.
Status ini juga yang membuat `CooldownDecorator` bisa bekerja: selama cooldown dia mengembalikan
`Running`, sehingga branch Attack tetap menjadi perilaku aktif dan Chase tidak ikut dijalankan
di tengah jeda serangan.

### 5. Modularitas komponen

Di FSM, logika "cek health lalu kabur" biasanya tertanam di dalam class state atau di dalam
`switch` besar. Sulit dipakai ulang.

Di Behavior Tree, `ConditionNode` dan `ActionNode` menerima delegate (`Func<bool>` dan
`Func<NodeState>`). `new ConditionNode(CanSeePlayer)` dipakai dua kali — sekali di branch Attack,
sekali di branch Chase — dari method yang sama tanpa duplikasi kode. `CooldownDecorator` juga
tidak tahu apa pun tentang Attack; dia bisa dibungkuskan ke node mana saja.

### 6. Cara debugging

FSM: cukup melihat satu variabel, `currentState`. Sederhana, dan ini keunggulan FSM.

Behavior Tree: yang perlu diketahui bukan hanya hasil akhir, tetapi branch mana yang dievaluasi
dan di node mana evaluasi berhenti. Di praktikum ini jejaknya diringkas ke field `currentAction`,
yang di-set di awal setiap Action. Untuk pohon yang lebih besar, ini tidak cukup dan biasanya
dibutuhkan visualizer pohon.

### Kesimpulan

FSM lebih mudah dipahami dan lebih mudah di-debug untuk NPC dengan sedikit state. Behavior Tree
lebih mahal di awal — ada enam file framework yang harus dibuat sebelum NPC bergerak sama sekali —
tetapi biaya itu konstan, sementara biaya penambahan perilaku pada FSM naik kuadratik. Untuk NPC
empat perilaku seperti praktikum ini, keduanya sebenarnya setara; keunggulan Behavior Tree baru
terasa pada perilaku kelima dan seterusnya.

---

# Eksperimen Wajib — Utility Score

Behavior Tree memilih berdasarkan **prioritas**; Utility AI memilih berdasarkan **skor**.
Bagian ini menghitung skor keempat aksi dengan parameter yang benar-benar dipakai di scene,
lalu membandingkan hasilnya dengan keputusan Behavior Tree.

### Parameter yang dipakai

```
maxHealth    = 100        visionRange  = 10
attackRange  = 2          patrolScore  = 0.1 (konstan)
threatWhenUnseen = 0.5
```

### Rumus

```
healthPercent   = currentHealth / maxHealth
lowHealthScore  = 1 - healthPercent
visibilityScore = canSeePlayer ? 1 : 0
threatScore     = canSeePlayer ? 1 : 0.5

attackScore = Clamp01(1 - distance / attackRange) × visibilityScore
chaseScore  = visibilityScore × Clamp01(distance / visionRange)
fleeScore   = lowHealthScore × threatScore
patrolScore = 0.1
```

`visibilityScore` berfungsi sebagai faktor pengali wajib (modul bagian 69): kalau Player tidak
terlihat, Attack dan Chase otomatis nol berapa pun jaraknya.

### Perhitungan untuk empat kasus modul bagian 78

| Kasus | Health | Terlihat | Jarak | Attack | Chase | Flee | Patrol | Utility | Behavior Tree |
|---|---:|---|---:|---:|---:|---:|---:|---|---|
| 1 | 100 | tidak | — | 0.00 | 0.00 | 0.00 | 0.10 | **PATROL** | PATROL |
| 2 | 100 | ya | 6 | 0.00 | 0.60 | 0.00 | 0.10 | **CHASE** | CHASE |
| 3 | 100 | ya | 1 | 0.50 | 0.10 | 0.00 | 0.10 | **ATTACK** | ATTACK |
| 4 | 20 | ya | 1 | 0.50 | 0.10 | 0.80 | 0.10 | **FLEE** | FLEE |

Keempatnya cocok. Yang menarik justru kasus di antaranya.

### Kasus di mana kedua pendekatan berbeda

| Kasus | Health | Terlihat | Jarak | Attack | Chase | Flee | Patrol | Utility | Behavior Tree |
|---|---:|---|---:|---:|---:|---:|---:|---|---|
| 5 | 40 | ya | 1 | 0.50 | 0.10 | 0.60 | 0.10 | **FLEE** | ATTACK |
| 6 | 35 | tidak | 12 | 0.00 | 0.00 | 0.33 | 0.10 | **FLEE** | PATROL |
| 7 | 60 | ya | 1 | 0.50 | 0.10 | 0.40 | 0.10 | **ATTACK** | ATTACK |

Kasus 5 adalah inti perbedaannya. Behavior Tree memakai ambang keras `health <= 30`, jadi pada
health 40 NPC masih menyerang dengan percaya diri, lalu mendadak kabur begitu health menyentuh 30.
Utility AI tidak punya ambang: `fleeScore` naik terus-menerus seiring health turun, dan menyalip
`attackScore` begitu nilainya melewati 0,50.

Titik baliknya bisa dihitung. Pada jarak 1 unit, `attackScore` tetap 0,50, sedangkan
`fleeScore = 1 - healthPercent`. Keduanya sama ketika `healthPercent = 0,5`:

```
Utility AI       → mulai kabur di health < 50
Behavior Tree    → mulai kabur di health <= 30
```

Jadi NPC Utility berperilaku lebih "takut secara bertahap", dan ambangnya ikut berubah sesuai
jarak — semakin dekat Player, semakin tinggi `attackScore`, semakin rendah health yang dibutuhkan
sebelum NPC memilih kabur. Pada jarak nol `attackScore` mencapai 1,00 dan NPC praktis tidak akan
pernah kabur. Perilaku seperti itu mustahil dinyatakan dengan satu ambang di Behavior Tree;
butuh beberapa Condition Node bersarang.

Kasus 6 menunjukkan efek `threatWhenUnseen`. Karena NPC tetap merasa sedikit terancam walau tidak
melihat Player, dengan health 35 dia memilih mundur ke SafePoint, sementara Behavior Tree tetap
patroli karena 35 masih di atas ambang 30.

### Masalah Utility AI dan penanganannya

Rapid action switching (modul bagian 72) nyata terjadi di sini. Pada jarak sekitar 2,2 unit dengan
health penuh, `chaseScore ≈ 0,22` sementara `attackScore` baru keluar dari nol — selisihnya tipis,
dan sedikit gerakan Player bisa membuat pemenangnya berganti beberapa kali per detik.

`UtilityEnemyController` memakai dua pengaman dari modul:

- **Action Commitment** (bagian 73) — `minimumActionDuration = 1` detik. Aksi yang sudah terpilih
  ditahan minimal satu detik sebelum evaluasi boleh mengganti.
- **Hysteresis** (bagian 74) — `switchMargin = 0.15`. Kandidat baru harus menang minimal 0,15 di
  atas skor aksi yang sedang berjalan, bukan cukup 0,01.

Untuk melihat masalahnya langsung: set `minimumActionDuration = 0` dan `switchMargin = 0`, lalu
berdiri di sekitar jarak 2 unit. Field `Current Action` akan berkedip antara CHASE dan ATTACK.
Kembalikan ke nilai semula, dan perilakunya tenang.

### Tabel perbandingan akhir

| Aspek | Behavior Tree | Utility AI |
|---|---|---|
| Dasar keputusan | prioritas (urutan child Selector) | skor tertinggi |
| Representasi kondisi | boolean (`health <= 30`) | kontinu (`1 - healthPercent`) |
| Perubahan perilaku | mendadak di ambang | bertahap |
| Jumlah evaluasi per frame | berhenti di branch pertama yang berhasil | semua aksi selalu dihitung |
| Risiko utama | branch berprioritas tinggi memblokir yang lain | rapid action switching |
| Penanganan risiko | atur urutan child dengan benar | action commitment, hysteresis |
| Debugging | node/action yang aktif | nilai skor tiap aksi |
| Cocok untuk | perilaku dengan aturan prioritas jelas | perilaku yang perlu pertimbangan bergradasi |

### Cara mendemokan

Jalankan **Praktikum 06 → Tambah Enemy Utility (Bonus D)**. Scene akan punya dua Enemy dengan
perception dan navigasi identik:

- **Enemy** (merah) — Behavior Tree
- **EnemyUtility** (ungu) — Utility AI, keempat skornya tampil live di Inspector

Beri damage 25 dua kali ke keduanya (health → 50). Lalu dekati keduanya sampai jarak 1 unit.
Enemy merah akan menyerang; Enemy ungu akan mulai mundur. Context menu `Log Semua Skor` di
`UtilityEnemyController` mencetak keempat skor dan aksi terpilih ke Console untuk lampiran laporan.

---

# Eksperimen 1–5 (modul bagian 57–61)

Semuanya dijalankan dari komponen `EnemyBTController` saat Play, tanpa mengedit kode.
Eksperimen 1–3 lewat tiga checkbox di header **Eksperimen**; Eksperimen 4–5 lewat preset di
context menu komponen (klik kanan header komponen, atau ikon tiga titik di kanan atas).

**Penting:** setelah mengubah checkbox atau `Attack Cooldown` saat Play, jalankan context menu
**Rebuild Behavior Tree**. `CooldownDecorator` menyimpan nilai cooldown pada saat node dibuat,
jadi pohonnya harus disusun ulang agar perubahan ikut terpakai. Setiap kali pohon dibangun,
Console mencetak konfigurasi aktifnya — pakai log itu sebagai bukti di laporan.

Kembalikan ke kondisi modul kapan saja dengan preset **Enemy Normal (modul)**.

### Eksperimen 1 — Mengubah prioritas Selector (bagian 57)

Centang `Eksperimen 1 Attack Sebelum Flee` → Rebuild Behavior Tree. Urutan Selector menjadi
`Attack > Flee > Chase > Patrol`.

Kondisi uji: **Test Damage 25** tiga kali (health 25, di bawah threshold 30), lalu berdiri
1 unit di depan Enemy.

Hasil yang diharapkan: `Current Action = ATTACK`, bukan FLEE. Selector memeriksa `attackSequence`
lebih dulu; `CanSeePlayer` Success, `IsPlayerInAttackRange` Success, Cooldown → Attack Success,
jadi Selector berhenti di situ dan `fleeSequence` tidak pernah dievaluasi — padahal
`IsHealthLow()` tetap mengembalikan `true`.

Kesimpulan: posisi child di Selector **adalah** prioritas. Kondisi yang sama persis menghasilkan
perilaku berbeda hanya karena urutannya ditukar.

### Eksperimen 2 — Menghilangkan Condition Attack Range (bagian 58)

Centang `Eksperimen 2 Tanpa Attack Range` → Rebuild. Branch Attack menjadi
`CanSeePlayer → Attack` saja.

Kondisi uji: berdiri sekitar 8 unit di depan Enemy, masih di dalam Vision Range 10 dan di dalam
FOV, tanpa penghalang.

Hasil yang diharapkan: Enemy **menyerang dari jarak 8 unit tanpa mendekat sama sekali**. Console
mencetak `Enemy attacks Player!` dan Player Health turun, sementara Enemy berdiri diam karena
`AttackPlayer()` memanggil `agent.isStopped = true`. Branch Chase tidak pernah dievaluasi karena
Attack sudah berhasil lebih dulu.

Kesimpulan: Action Node tidak tahu apa pun soal kelayakan dirinya sendiri. Yang menjaga agar
Attack hanya terjadi pada jarak yang benar adalah Condition Node, bukan Action-nya.

### Eksperimen 3 — Menghapus Cooldown (bagian 59)

Centang `Eksperimen 3 Tanpa Cooldown` → Rebuild. Branch Attack memanggil `AttackPlayer()` langsung
tanpa Decorator.

Kondisi uji: berdiri 1 unit di depan Enemy.

Hasil yang diharapkan: Console dibanjiri `Enemy attacks Player!` — satu per frame. Pada 60 FPS itu
60 serangan per detik, masing-masing 10 damage, jadi 600 damage per detik. Player Health 100 habis
dalam waktu kurang dari sepertiga detik dan Console langsung mencetak `Player Dead`.

Kesimpulan: ini yang dicegah Decorator. `CooldownDecorator` tidak mengubah logika Attack sedikit
pun — dia hanya mengatur *kapan* child-nya boleh di-`Tick()`, dan mengembalikan `Running` selama
menunggu agar branch Attack tetap menjadi perilaku aktif.

### Eksperimen 4 — Enemy Aggressive (bagian 60)

Context menu → **Preset: Enemy Aggressive (bagian 60)**.

```
Vision Range = 15      Attack Range    = 2.5
Vision Angle = 150     Attack Cooldown = 0.8
Chase Speed  = 5       Low Health Threshold = 15
```

Hasil yang diharapkan: jauh lebih sulit dihindari. FOV naik dari 90° ke 150°, artinya zona buta
di belakang Enemy menyusut dari 270° menjadi 210° — Player harus benar-benar di belakang punggung
Enemy untuk tidak terlihat, dan trik Pengujian 6 jadi jauh lebih sempit. Vision Range 15 pada arena
30×30 berarti Enemy mengawasi radius setengah arena.

Chase Speed 5 menyamai `moveSpeed` Player yang juga 5, jadi Player tidak bisa kabur lurus — satu-
satunya cara lepas adalah memutar di balik Wall untuk memutus garis pandang. Attack Cooldown 0.8
membuat serangan hampir dua kali lebih sering, dan Low Health Threshold 15 berarti Enemy bertahan
bertempur sampai hampir mati — butuh **Test Damage 25** empat kali sebelum dia memilih kabur.

### Eksperimen 5 — Enemy Coward (bagian 61)

Context menu → **Preset: Enemy Coward (bagian 61)**.

```
Vision Range = 10      Attack Cooldown = 2
Vision Angle = 90      Low Health Threshold = 70
Chase Speed  = 3       Flee Speed = 6
```

Hasil yang diharapkan: **Test Damage 25** dua kali sudah cukup. Health 100 → 75 (masih di atas 70,
tetap bertempur) → 50 (di bawah 70), dan `Current Action` langsung berubah `FLEE`. Karena Flee
Speed 6 lebih besar dari Chase Speed Player, Enemy tidak bisa dikejar. Dan karena health tidak
pernah pulih sementara Flee ada di posisi pertama Selector, Enemy akan menetap di SafePoint
sampai kamu menjalankan **Reset Health**.

### Kesimpulan Eksperimen 4–5

Algoritma kedua preset **identik** — pohon Behavior Tree-nya sama persis, kode yang dijalankan
sama persis. Yang berbeda hanya delapan angka di Inspector. Tetapi karakter NPC-nya terbaca
sangat berbeda: satu penyerang gigih, satu penakut yang kabur di luka pertama.

Ini poin yang paling layak ditulis di kesimpulan laporan: struktur decision making menentukan
*perilaku apa yang mungkin*, sedangkan parameter tuning menentukan *kepribadian* NPC. Keduanya
bukan hal yang sama, dan yang kedua sering lebih menentukan rasa bermain daripada yang pertama.

---

# Jawaban Pertanyaan Analisis (modul bagian 81)

> Draf — sesuaikan dengan kata-katamu sendiri sebelum dikumpulkan.

**1. Apa fungsi `BTNode`?**
Base class abstrak untuk semua node Behavior Tree. Dia mendefinisikan satu kontrak,
`public abstract NodeState Tick()`, sehingga Selector bisa memanggil `child.Tick()` tanpa perlu
tahu child-nya Sequence, Condition, Action, atau Decorator. Inilah yang membuat pohon bisa
disusun bebas.

**2. Apa perbedaan `Success`, `Failure`, dan `Running`?**
`Success` berarti node selesai dan berhasil (kondisi terpenuhi, atau action tuntas).
`Failure` berarti kondisi tidak terpenuhi atau action gagal. `Running` berarti action belum
selesai dan masih butuh frame berikutnya. `Running` yang membuat Behavior Tree bisa menangani
aksi berdurasi, bukan hanya keputusan sesaat.

**3. Apa fungsi Selector?**
Mencoba child satu per satu sesuai urutan, lalu berhenti pada child pertama yang mengembalikan
`Success` atau `Running`. Kalau semua child `Failure`, Selector sendiri `Failure`. Analoginya
operator OR, tetapi dengan tambahan status `Running`.

**4. Mengapa urutan child pada Selector penting?**
Karena Selector berhenti di child pertama yang tidak `Failure`, urutan child adalah prioritas.
Child yang di bawah tidak akan pernah dievaluasi kalau child di atasnya sudah berhasil. Di
praktikum ini `Flee` di posisi pertama menjamin health kritis selalu mengalahkan keinginan
menyerang.

**5. Apa fungsi Sequence?**
Menjalankan child berurutan dan memastikan semuanya berhasil. Begitu ada satu child `Failure`,
Sequence langsung `Failure` dan child berikutnya tidak dijalankan. Analoginya operator AND.
Di branch Attack, ini yang mencegah `AttackPlayer()` berjalan ketika Player di luar jarak serang.

**6. Apa perbedaan Condition Node dan Action Node?**
Condition Node hanya memeriksa keadaan dan tidak mengubah apa pun; dia menerima `Func<bool>` dan
hanya bisa mengembalikan `Success` atau `Failure`. Action Node melakukan sesuatu — menggerakkan
agent, memberi damage — menerima `Func<NodeState>`, dan bisa mengembalikan ketiga status
termasuk `Running`.

**7. Mengapa Chase mengembalikan `Running`?**
Karena mengejar tidak selesai dalam satu frame. `ChasePlayer()` hanya memanggil
`agent.SetDestination(player.position)` lalu menyerahkan pergerakannya ke NavMeshAgent selama
banyak frame. Kalau dia mengembalikan `Success`, Selector akan menganggap pekerjaannya tuntas
di frame itu; kalau `Failure`, Selector akan lanjut mengevaluasi Patrol dan NPC akan mengejar
sambil patroli sekaligus.

**8. Mengapa Attack menggunakan Cooldown?**
Karena `Update()` berjalan setiap frame. Tanpa cooldown, pada 60 FPS `AttackPlayer()` dipanggil
60 kali per detik dan Player menerima 600 damage per detik — bukan gameplay. `CooldownDecorator`
dengan `attackCooldown = 1.5` membatasinya menjadi satu serangan per 1,5 detik.

**9. Apa fungsi Decorator?**
Membungkus **satu** child dan memodifikasi cara child itu dieksekusi, tanpa mengubah kode child
tersebut. `CooldownDecorator` tidak tahu apa pun tentang Attack — dia hanya menahan `Tick()`
child-nya selama cooldown aktif dan mengembalikan `Running`. Decorator umum lainnya: Inverter,
Repeat, Timeout.

**10. Apa fungsi Blackboard?**
Tempat penyimpanan data bersama yang dibaca dan ditulis oleh sistem AI — posisi Player, health,
status terlihat atau tidak, posisi terakhir Player terlihat. Dengan Blackboard, node Behavior Tree
tidak perlu memanggil perception sendiri; mereka cukup membaca data yang sudah tersedia.
Di praktikum ini data tersebut masih disimpan sebagai field di `EnemyBTController` agar
strukturnya belum terlalu kompleks.

**11. Mengapa Perception sebaiknya dipisahkan dari Decision Making?**
Tiga alasan. Pertama, efisiensi: `CanSeePlayer()` dipanggil dua kali per frame di implementasi
ini — sekali di branch Attack, sekali di branch Chase — jadi raycast-nya dijalankan dua kali
padahal hasilnya sama. Kalau perception dihitung sekali lalu disimpan di Blackboard, cukup satu
raycast. Kedua, keduanya bisa diuji dan diganti terpisah. Ketiga, beberapa sistem lain
(animasi, audio, UI) sering butuh data perception yang sama.

**12. Mengapa Flee ditempatkan sebelum Attack?**
Supaya health kritis menjadi kondisi darurat yang mengalahkan segalanya. Dengan health 20, Player
terlihat, dan jarak 1 meter, NPC sebenarnya memenuhi semua syarat Attack. Tetapi Selector
memeriksa `fleeSequence` lebih dulu; `IsHealthLow()` mengembalikan `Success`, `Flee()`
mengembalikan `Running`, dan Selector berhenti di situ — branch Attack dan Chase tidak pernah
dievaluasi.

**13. Apa yang terjadi jika Patrol ditempatkan pertama pada Selector?**
`Patrol()` selalu mengembalikan `Running` selama `patrolPoints` terisi. Selector akan selalu
berhenti di child pertama, sehingga Flee, Attack, dan Chase tidak pernah dievaluasi sama sekali.
NPC akan patroli terus-menerus dan mengabaikan Player maupun health-nya sendiri. Ini kesalahan
nomor satu di modul bagian 84.

**14. Apa perbedaan decision making FSM dan Behavior Tree?**
FSM menyimpan state aktif dan berpindah lewat transisi eksplisit yang harus ditulis satu per satu;
pertanyaannya "sekarang saya di state apa, dan ke mana saya boleh pindah?". Behavior Tree tidak
menyimpan state aktif — dia mengevaluasi ulang seluruh pohon setiap frame dan memilih branch
berprioritas tertinggi yang kondisinya terpenuhi; pertanyaannya "perilaku terpenting yang bisa
saya lakukan sekarang apa?". Uraian lengkapnya ada di bagian Tugas 6 laporan ini.

**15. Apa perbedaan Behavior Tree dan Utility AI?**
Behavior Tree memilih berdasarkan prioritas tetap dan berhenti di pilihan pertama yang valid.
Utility AI menghitung skor semua aksi lalu memilih yang tertinggi, sehingga perubahan perilakunya
bertahap bukan mendadak. Perbandingan berikut angkanya ada di bagian Eksperimen Utility Score.

**16. Mengapa Utility AI memerlukan normalisasi skor?**
Supaya skor antar aksi bisa dibandingkan. Kalau `attackScore` dihitung dari jarak dalam unit
(bisa 0–10) sementara `fleeScore` dari persentase health (0–1), maka Attack akan hampir selalu
menang bukan karena lebih penting, tetapi karena satuannya lebih besar. Dengan menormalkan
semuanya ke rentang 0–1 lewat `Mathf.Clamp01()`, angka 0,8 berarti hal yang sama untuk setiap
aksi. Normalisasi juga membuat skor bisa dikalikan sebagai faktor, seperti `visibilityScore`.

**17. Apa kegunaan default action pada Utility AI?**
Menjamin selalu ada minimal satu aksi yang terpilih. Kalau Player tidak terlihat dan health
penuh, Attack, Chase, dan Flee semuanya nol; tanpa default action, NPC tidak melakukan apa pun
dan hanya berdiri diam. `patrolScore = 0.1` membuat Patrol menang dalam kondisi itu, tetapi
nilainya cukup rendah untuk kalah begitu ada aksi lain yang relevan.

**18. Mengapa Utility AI dapat mengalami rapid action switching?**
Karena skor dihitung ulang setiap frame dan tidak ada yang mengikat keputusan sebelumnya.
Kalau dua aksi punya skor berdekatan — misalnya Attack 0,51 dan Chase 0,50 — pergerakan kecil
Player bisa menukar pemenangnya bolak-balik beberapa kali per detik. NPC-nya terlihat bergetar
dan tidak pernah menyelesaikan satu aksi pun.

**19. Apa fungsi Action Commitment?**
Mengikat NPC pada aksi yang sudah dipilih selama durasi minimum tertentu sebelum evaluasi
diizinkan menggantinya. Di `UtilityEnemyController`, `minimumActionDuration = 1` detik: setelah
memilih Attack, NPC bertahan menyerang minimal satu detik walaupun skor Chase sempat lebih
tinggi di tengah jalan. Hasilnya aksi terlihat tuntas dan tidak bergetar.

**20. Kapan Utility AI lebih cocok dibanding Behavior Tree?**
Ketika keputusan NPC bergantung pada banyak faktor yang perlu ditimbang bersama dan bergradasi,
bukan diurutkan prioritas. Contohnya memilih senjata (jarak, amunisi, tipe musuh, cover),
memilih target di antara beberapa musuh, atau NPC simulasi kebutuhan seperti The Sims.
Behavior Tree tetap lebih cocok ketika aturannya memang hierarkis dan tegas — "kalau health
kritis, kabur, titik" — karena di situ prioritas eksplisit lebih mudah dibaca, di-debug, dan
dijamin benar daripada berharap bobot skor menghasilkan urutan yang diinginkan.

---

## Screenshot yang perlu diambil

1. Hierarchy scene `P06_BehaviorTree` lengkap.
2. Inspector `Enemy` — seluruh field `EnemyBTController` terisi (player, patrolPoints, safePoint,
   obstacleMask).
3. Scene View dengan `Enemy` ter-select — gizmo Vision Range, Attack Range, dan dua garis FOV.
4. Satu screenshot per pengujian 1–4 dengan field `Current Action` terbaca:
   PATROL, CHASE, ATTACK, FLEE.
5. Pengujian 5 — Player di balik Wall, Enemy tetap PATROL.
6. Console saat Attack berulang, memperlihatkan jeda cooldown dan Player Health menurun.
7. `Navigation` ter-select dengan NavMesh biru terlihat di Scene View.
8. Inspector `EnemyUtility` saat Play — keempat skor terbaca (untuk bagian Utility Score).
