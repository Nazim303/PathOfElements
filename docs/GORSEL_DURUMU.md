# Görsel Durumu (2026-11-26)

`python Tools/artscan.py` çıktısı ve görsellerin önizlemesi üzerinden elle sınıflandırıldı.

**Durumlar:** ✅ Bitmiş · 🎨 Renksiz (boyanmış ama gri tonlu) · ✏️ Eskiz (çizgi çalışması) · ❌ Yok · ⚠️ Yanlış görsel

## Özet
> Hexodia (Ultimate) kartları ve zemin kartları oyundan çıkarıldı (2026-11-26). Kart sayısı 30 → 24.

| | Kart (30) | Düşman (14) | Eşya (3) | Toplam |
|---|---|---|---|---|
| ✅ Bitmiş | 11 | 6 | 0 | **17** |
| 🎨 Renksiz | 0 | 3 | 0 | 3 |
| ✏️ Eskiz | 12 | 3 | 2 | **17** |
| ❌ Yok | 7 | 1 | 1 | **9** |
| ⚠️ Yanlış / geçici | 0 | 1 | 0 | 1 |

Oyunda görünen görsellerin **yarıdan fazlası bitmemiş**.

## Güncelleme: eskizler renklendirildi
✏️ olarak işaretli 17 eskiz `Tools/colorize_sketches.py` ile çerçeve elementinin rengine boyandı. Dosyalar yerinde değişti, .meta/GUID aynı kaldı. Orijinal eskizler git geçmişinde (`579bc9f`) duruyor. Çizer bitmiş görseli verdiğinde dosyanın üzerine yazmak yeterli.

Bu bir **ara çözüm**. Tablolardaki ✏️ işareti "bitmiş çizim hâlâ gerekiyor" anlamında kaldı.

## Kartlar
| Kart | Durum | Not |
|---|---|---|
| Droplet | ✅ | |
| Fire Card | ✅ | |
| Heal Card | ✅ | |
| Ice Card | ✅ | |
| Rock Throw | ✅ | |
| Tornado | ✅ | |
| McDuck | ✅ | |
| Recycle | ✅ | |
| Water Jet | ✅ ⚠️ | Görsel iyi ama dosya bir ekran görüntüsü (`Ekran görüntüsü 2026-05-11…`). Orijinal dosyayla değiştirilmeli. |
| Breeze | ✏️ | |
| Healing Water | ✏️ | |
| Magma Punch | ✏️ | Kısmen renkli |
| Pebble | ✏️ | |
| Quake | ✏️ | |
| Spark | ✏️ | |
| Wind Slash | ✏️ | |
| Gambling | ✏️ | |
| Greed | ✏️ | Kısmen renkli |
| Mother Nature (Companion) | ✏️ | |
| Strong Magic (Companion) | ✏️ | |
| Will (Companion) | ✏️ | |
| Fire Supportive | ❌ | sadece element çerçevesi, çizim yok |
| Water / Earth / Air Supportive | ❌ | yeni (2026-11-26), Fire Supportive gibi sadece çerçeve |
| Shield | ❌ | |
| Mana | ❌ | Kart çerçevesi de yok |

## Düşmanlar
> **Düzeltme:** Oyunda görünen isimler (`enemyName`) görsellerle zaten uyumlu: Watcher, Fire Arrows, Drowned, Marauders, Gao, Bloom Sprout vb. Uyumsuz olanlar sadece asset **dosya adları** (ör. `Coal Imp.asset`). İstenirse Unity içinde yeniden adlandırılabilir; oyuncuya görünmez.

| Düşman | Durum | Görselde ne var |
|---|---|---|
| Angry Bat | ✅ | gözlü yaratık ("gözlü melek") |
| Green Slime | ✅ | su/sis yaratığı ("boğulanlar") |
| Harpy | ✅ | |
| Mini Golem | ✅ | mantar yaratık |
| Toxic Mushroom | ✅ | karanlık orman ağacı |
| Water Spirit | ✅ | |
| TrainingDummy_Fire | ⚠️ | Gemini ile üretilmiş, stil farklı |
| Magma Worm | 🎨 | çöl şeytanı, gri tonlu |
| Wolf | 🎨 | orman manzarası, gri tonlu |
| Boss_Protector | 🎨 | gri tonlu |
| Coal Imp | ✏️ | okçular |
| Ice Witch | ✏️ | golem/robot çizgisi. Çerçevesi de Earth, element kontrol edilmeli |
| White Wolf | ✏️ | asker grubu |
| **Boss_Elementalist** | ❌ | Final boss. Oyunun en görünür eksiği |

## Eşyalar
| Eşya | Durum |
|---|---|
| Extra Demage | ✏️ (bandaj) |
| Max health | ✏️ (kamp ateşi, kısmen renkli) |
| MaxHand | ❌ |

## Önceliklendirme önerisi
1. **Boss_Elementalist**: oyunun sonu, mutlaka bitmiş olmalı.
2. **Başlangıç destesindeki kartlar**: oyuncunun ilk dakikada gördükleri.
3. ❌ olan 9 görsel: şu an boş kare görünüyor.
4. ⚠️ olanlar: yanlış görseller oyuncunun kafasını karıştırır.
5. ✏️ eskizler: tutarlı bir stil seçilirse "bilerek eskiz" görünümüne dönüştürülebilir (aşağıya bakın).

## Stil seçeneği: eskizi bilinçli stile çevirmek
Eskizlerin hepsi aynı gri kağıt ve kurşun kalem stilinde. Bitmiş görsellere ulaşılamıyorsa iki seçenek var:
- **Tüm kartları eskiz stiline çekmek:** bitmiş görsellere eskiz filtresi uygulanır, "çizim defteri" teması oluşur. Oyunda zaten *Journal* (günlük) sistemi var, bu temaya uyuyor.
- **Eskizleri renklendirmek:** eskizin üstüne element renginde yarı saydam bir boya katmanı ve parıltı eklenir. Çizgi korunur, kart diğerlerinin yanında sırıtmaz.
