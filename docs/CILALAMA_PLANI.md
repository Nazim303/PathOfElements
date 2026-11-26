# Path of Elements — Cilalama Planı

> Proje kod hataları yüzünden değil, çizimler tamamlanmadığı için yarım kaldı. Bu yüzden cilalamanın ağırlık merkezi **eksik görseller**.
> Kural: VFX prefab'larına, partikül materyallerine ve efekt referanslarına sormadan dokunulmaz.

## 0. Token tasarrufu (her adımdan önce geçerli)

Bu maddeler bir kere yapılır, sonraki her oturumda maliyeti düşürür.

| # | Madde | Neden |
|---|---|---|
| T1 | **Sahne dosyaları asla bütün olarak okunmaz.** `SampleScene.unity` ~470 KB (≈150k token). Sadece `grep` ile ilgili alan aranır. Bu kural CLAUDE.md'ye yazılır. | Tek bir yanlış okuma bütün oturumun bütçesini yer. |
| T2 | `.claude/settings.json` içinde `Library/`, `Temp/`, `Logs/`, `obj/`, `*.unity`, `*.prefab` için `Read` izni kapatılır (deny). | Arama ve okumalar yanlışlıkla dev dosyalara girmesin. |
| T3 | Tarama script'leri repoya eklenir: `Tools/guidscan.py` (kopuk referanslar), `Tools/artscan.py` (kart/düşman/eşya görselleri). | Bu incelemede birkaç bin token'lık keşif yapıldı; sonraki seferler tek komut. |
| T4 | `docs/OYUN_VERISI.md`: tüm kartların, düşmanların ve eşyaların istatistik tablosu (T3'teki script üretir). | Denge konuşmaları 47 asset dosyası yerine tek tablo okuyarak yapılır. |
| T5 | `docs/SISTEMLER.md`: her manager için 3–5 satırlık özet ve önemli metotlar. | 956 satırlık `CombatManager`'ı baştan okumadan doğru yere gidilir. |
| T6 | `CombatManager.cs` (956 satır) mantıklı parçalara bölünür: efekt oynatma, hasar hesabı, füzyon/ultimate. *(İsteğe bağlı, davranış değişmeden)* | Her savaş değişikliğinde 1000 satır okunmaz. |
| T7 | Oturum disiplini: her faz ayrı oturum, faz bitince CLAUDE.md güncellenir. Mekanik işler (yeniden adlandırma, tablo üretme) daha ucuz modelle yapılır. | Uzun bağlam = pahalı bağlam. |
| T8 | Sahne veya prefab değişiklikleri YAML'ı elle düzenleyerek değil, Unity Editor'de (veya Editor script'iyle) yapılır. | YAML düzenlemesi hem riskli hem çok token harcıyor. |

## 1. Eksik görseller (asıl iş)

### Görseli hiç olmayanlar
| Asset | Eksik |
|---|---|
| Ultimate parçaları: `Kafa`, `Gövde`, `Kollar`, `Bacaklar` | kart görseli (4 adet) |
| `Fire Supportive` | kart görseli |
| `Support Data/Shield` | kart görseli |
| `Support Data/Mana` | kart görseli + kart çerçevesi |
| `BossData/Boss_Elementalist` | düşman görseli + çerçeve (**boss — en görünür eksik**) |
| `ItemData/MaxHand` | eşya ikonu |

### Yer tutucu kullananlar (yerine gerçek çizim gelmeli)
| Asset | Şu an kullandığı görsel |
|---|---|
| `Water Jet` | bir ekran görüntüsü (`Ekran görüntüsü 2026-05-11 121109.png`) |
| `Support Data/ChangeTerrain` | Ice Spear görseli |
| `Support Data/Changeterrainwater` | Heal Potion görseli |
| `TrainingDummy_Fire` | Gemini ile üretilmiş görsel |
| `Assets/Sprites` altında 22 tane `Gemini_Generated_Image_*` | hangileri oyunda kullanılıyor, kontrol edilecek |

### Kontrol edilecek tutarsızlıklar
- `Ice Witch` Earth çerçevesi kullanıyor. Elementi ne olmalı?
- Düşman isimleri ile görsel dosya adları eşleşmiyor (ör. `Angry Bat` → "gözlü melek"). İsimler mi değişecek, görseller mi?

### Karar: görseller nereden gelecek?
1. Çizer işi tamamlar (en tutarlı sonuç).
2. Yeni çizer / freelance.
3. Mevcut stile uygun yer tutucular (ör. element renginde çerçeve + ikon). Oyun bu hâliyle **bitmiş gibi oynanabilir** hâle gelir, çizimler sonra değiştirilir.

> Öneri: önce 3, sonra 1/2. Böylece proje yeniden çizere bağımlı olup durmaz.

## 2. Oyun hissi
- Düşman ölüm efekti (`enemyDeathEffectPrefab`) şu an boş. Sulhnafein'le yeni efekt kararı verilecek.
- Ses kapsamı: her kart tipi, satın alma ve harita geçişi için ses var mı, listelenecek.
- DOTween ile UI geçişleri: menü, dükkan ve harita panellerinde tutarlılık.

## 3. Temizlik
- `SampleScene` içindeki **Missing Script** (commit edilmemiş script, `c0d0698`). Bulunursa eklenir, bulunmazsa component silinir.
- `Renderer2D.asset` içindeki URP referanslarını Unity'de aç–kaydet ile yenile.
- Repo çöpleri: `Assets/` kökündeki 3 ekran görüntüsü, `Yeni klasör`, kullanılmayan `Gemini_Generated_*` dosyaları.
- Eski sahneler (`TutorialScene`, `VFXScene`): build'de değiller. Silinecek mi, arşivlenecek mi?
- Tek VCS seçimi (git **veya** Plastic), büyük görseller/video için Git LFS, UnityYAMLMerge kurulumu.

## 4. Denge ve içerik
- `docs/OYUN_VERISI.md` tablosu üzerinden kart maliyetleri ve hasar eğrisi.
- Boss_Elementalist ve Boss_Protector zorluğu.

## 5. Build ve test
- Windows build'i, baştan sona bir koşu (menü → tutorial → harita → boss).
- Bulunan hatalar bu dosyanın sonuna eklenir.

## Önerilen sıra
`T1–T3` → `1 (yer tutucular)` → `3 (temizlik)` → `T4–T5` → `2` → `4` → `5`. `T6` sadece savaş koduna yoğun dokunulacaksa yapılır.
