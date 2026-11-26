# Dengeleme Planı (2026-11-26)

Bu belge oyunun sayılarını baştan ele alıyor: kartlar, düşmanlar, bosslar, eşyalar, enerji/çekme ekonomisi ve harita oranları.
Kararların hepsi `Tools/balance_sim.py` simülasyonuyla ölçüldü. Simülasyon her ayar için binlerce koşu oynatır.

Yön `CLAUDE.md` ile aynı: **yeni mekanik yok, yeni çizim yok.** Sadece sayılar ve birkaç küçük kural değişti.
Yeni olanlar: Gambler's Hand eşyasının yeni etkisi ("her tur +1 kart") ve diğer üç element için güçlendirici kartlar. Güçlendiriciler çizim gerektirmiyor, Fire Supportive gibi sadece element çerçevesini kullanıyor.

---

## 1. Teşhis: mevcut durumda ne bozuk?

Simülasyon (açgözlü yapay oyuncu, 1500 koşu, deste başına):

| Deste | Kazanma | Normal savaş can kaybı | Elite can kaybı | Boss can kaybı |
|---|---|---|---|---|
| Tipik (her elementten karışık) | **%23** | 11.6 | 44.5 | 50.6 |
| Güçlü (Magma Punch, Quake, Ice Spear…) | **%72** | 6.2 | 46.9 | 26.4 |
| Ateş ağırlıklı | %48 | 8.7 | 46.1 | 38.0 |

Bulunan sorunlar, önem sırasıyla:

1. **Deste seçimi oyunu belirliyor (%23 ile %72 arası).** Güç-3 kartlar (Magma Punch 15, Quake 16) Fire Ball (12) ile *aynı* 2 enerjiye mal oluyor. Seçim ekranında bunları alan oyuncu kazanıyor, almayan kaybediyor. Tornado ise 3 enerjiye 16 vuruyor; oyunun en kötü kartı.
2. **Elite savaşı bir normal savaşın 4–5 katı can götürüyor (~45/100).** 2–3 düşmanın hepsi ×1.5 can ve ×1.3 hasar alıyor. Bir de **1. katmanda bile** elite çıkabiliyor.
3. **Ara boss final bosstan güçlü.** Protector 150 can ve 20 hasar, Elementalist ise 100 can ve 15 hasar. Protector bir de iki minyon getiriyor ve 20 can iyileştiriyor.
4. **Enerji savaşlar arasında dolmuyor.** `EnergyManager` sadece oyunun başında doluyor. Uzun bir savaştan sonra sonraki savaşa 2–3 enerjiyle başlanıyor. Belgelerde "savaş dolu başlıyor" yazıyor ama kod öyle çalışmıyor.
5. **Düşman elementleri dengesiz.** 11 düşmanın 5'i Toprak. Ateş kartları düşmanların %45'ine 2x vuruyor, Hava kartları aynı düşmanlara 0.5x vuruyor. Hava destesi cezalı.
6. **Bedava destek kartları fazla güçlü.** Destekler 0 enerji. Blood Transfer 15 can veriyor, 2 enerjilik Healing Water'dan iyi. Mana Transfer +2 enerji veriyor, yani bir turun tüm enerji geliri.
7. **Dükkan eşyaları.** Gambler's Hand (+2 el boyutu) işe yaramıyor, çünkü elde zaten yer var ve tur başına sadece 2 kart çekiliyor. Bandage (tam can, 30 altın) ucuz.
8. **Yoldaş kartları kötü.** Tsunami 4 enerjiye 22 vuruyor; 2 enerjilik kartlardan verimsiz. Pablo 3 enerjiye 15 AOE ve tek kullanımlık.
9. **Zorluk eğrisi.** Haritanın ilk yarısıyla ikinci yarısı aynı düşman havuzunu kullanıyor. Sadece düşman sayısı artıyor.

## 2. Hedefler

| Ölçü | Hedef |
|---|---|
| Kazanma oranı (yapay oyuncu) | %60–75. İnsan oyuncu biraz daha iyi oynar, ilk koşuda ~%50 |
| Desteler arası fark | ≤ 15 puan |
| Normal savaş | 2–3 tur, maks. canın ~%7'si |
| Elite | 4–6 tur, maks. canın ~%20'si |
| Final boss | 5–7 tur, en çok ölüm burada (%50–70) |
| Ölümün kaynağı | İlk yarıda neredeyse hiç ölüm yok, ikinci yarıda birikimli yıpranma, sonda boss |

## 3. Yeni ekonomi

| Değer | Eski | Yeni | Neden |
|---|---|---|---|
| Maks. can | 100 | **80** | Küçük sayılar daha okunaklı. Düşman hasarları buna göre ayarlandı |
| Maks. enerji | 11 | **6** | "11 ile +2" yerine net bir bütçe. Bir tur dolu oynanabilir |
| Enerji / tur | +2 | +2 | aynı |
| Savaş başında enerji | dolmuyordu | **doluyor** | Teşhis 4. `EnergyManager.RefillToMax()` |
| Açılış eli | 4 kart çek | **5 kart çek** | İlk tur 6 enerjiyi harcayabilsin |
| Tur başına çekme | 2 (sabit kod) | 2 (`DeckManager.drawPerTurn`) | Gambler's Hand artırabilsin diye alana taşındı |
| Maks. el | 7 | **8** | 5 açılış + elde kalanlar |
| Exchange can bedeli | 10 | **8** | Maks. can düştü, oran korunsun |

**Kart verimi kuralı:** 1 enerji ≈ 6 hasar, 2 enerji ≈ 13, 3 enerji ≈ 20. Element avantajı (2x) bu eğrinin üstüne gelir.

## 4. Kartlar

| Kart | Eski (enerji / değer) | Yeni | Not |
|---|---|---|---|
| Spark / Droplet / Pebble / Breeze | 1 / 5,5,7,6 | 1 / **6** (hepsi) | Dört element eşit |
| Fire Ball / Water Jet / Rock Throw / Wind Slash | 2 / 12,11,13,12 | 2 / **13** (hepsi) | Dört element eşit |
| Magma Punch / Quake | 2 / 15,16 | **3 / 20** | Güç-3 artık pahalı ama güçlü. Bedava "daha iyi Fire Ball" değil |
| Ice Spear | 2 / 14 (güç 2) | **3 / 20 (güç 3)** | Suyun güç-3 kartı oldu (her elementte bir tane) |
| Tornado | 3 / 16 | 3 / **20** | Oyunun en kötü kartıydı |
| Fire Supportive | 0 / 3, +%50 | **1 / 5**, +%50 | Bkz. aşağıda: 0 enerjiyle iki güçlendirici destesi %87 kazanıyordu |
| **Water / Earth / Air Supportive** | yoktu | **1 / 5**, +%50 | **Yeni kart** |
| Heal Potion | 1 / 8 can | aynı | |
| Healing Water | 2 / 15 can | 2 / **16** | |
| Mana Transfer | +2 enerji | **+1** | Bedava kart, bir turun gelirini vermemeli |
| Blood Transfer | +15 can | **+8** | Healing Water'dan iyi olmamalı |
| McDuck | +5 altın | **+10** | 5 altın bir kartı harcamaya değmiyordu |
| Greed / Gambling / Shield | 2 kart / 4 kart / 8 kalkan | aynı | |
| **Yoldaşlar** | | | |
| Pablo (AOE, tek kullanım) | 3 / 15 | **2 / 20** | Tek kullanımlık olduğu için güçlü olmalı |
| Tsunami | 4 / 22 | **3 / 24** | Güç-3 kartlardan biraz iyi (ödül kartı) |
| Mother of Nature | 3 / 20 can | **2** / 20 can | |

### Kart ekle / çıkar
- **Çıkarılan kart yok.** Her kartın artık net bir rolü var.
- **Eklenen kartlar: Water Supportive, Earth Supportive, Air Supportive.** Önceden sadece Ateş destesi güçlendirici kullanabiliyordu.
  Fire Supportive'in kendi çizimi yok, element çerçevesini (`Sprites/CardBnaders/Spell/`) kullanıyor. Yeni kartlar da kendi elementlerinin çerçevesini kullanıyor, yani yeni çizim gerekmiyor.
- **Güçlendiricinin bedeli 0'dan 1 enerjiye çıktı.** Ölçüm: 0 enerjiyle iki güçlendiricili bir deste %87 kazanıyordu. Aynı deste, güçlendiriciler yerine 1 enerjilik kartlarla %55 kazanıyor. Güçlendirici bedavaya 20'lik bir karta +10 hasar ekliyordu.
  1 enerjide (tek başına 5 hasar, eşleşince +%50) aynı deste %58 kazanıyor. Yani güçlendirici artık "iyi bir 1 enerjilik kart" kadar değerli, zorunlu seçim değil.

## 5. Düşmanlar

İki havuz var. **Erken havuz** haritanın ilk yarısında çıkar. Geç savaşlar güçlü havuzdan gelir, 3'lü gruplarda biri erken havuzdan.

| Oyundaki isim (asset) | Element | Eski can / hasar / altın | Yeni | Havuz |
|---|---|---|---|---|
| Bloom Sprout (Wolf) | Toprak | 20 / 4 / 2 | 22 / 6 / 4 | erken |
| Magma Worm | Ateş | 15 / 7 / 4 | 20 / 8 / 4 | erken |
| Watcher (Angry Bat) | Hava | 20 / 7 / 5 | 22 / 7 / 4 | erken |
| Drowned (Green Slime) | Su | 25 / 6 / 5 | 26 / 7 / 5 | erken |
| Marauders (White Wolf) | **Toprak → Hava** | 25 / 6 / 3 | 30 / 8 / 5 | erken |
| Fire Arrows (Coal Imp) | Ateş | 22 / 9 / 7 | 28 / 10 / 7 | geç |
| Tree (Toxic Mushroom) | Toprak | 25 / 9 / 8 | 36 / 9 / 7 | geç |
| Shining Worm (Harpy) | Hava | 30 / 8 / 8 | 34 / 9 / 7 | geç |
| Mushroom (Mini Golem) | **Toprak → Su** | 30 / 8 / 9 | 34 / 9 / 7 | geç |
| Boiling Desire (Water Spirit) | Su | 30 / 10 / 9 | 34 / 11 / 8 | geç |
| Gao (Ice Witch) | Toprak | 35 / 9 / 12 | 44 / 12 / 10 | geç |

Element dağılımı 5/2/2/2'den **3 Toprak, 3 Hava, 3 Su, 2 Ateş** oldu. Element değişen iki düşmanın kart çerçevesi (`enemybander`) de yeni elementin çerçevesiyle değiştirildi.
**Görsel kontrol:** Marauders'ın Hava, Mushroom'un Su olması çizimle çelişiyorsa söyle; başka iki düşmanla değiştiririm.

### Elite
- Can ×1.5 → **×1.3**, hasar ×1.3 → **×1.2**, altın ×2 aynı.
- Düşman sayısı 2–3 → **2**, güçlü havuzdan.
- %50 ihtimalle Protector, aynı.
- **3. katmandan önce elite çıkmaz** (`MapGenerator.eliteMinLayer`).

### Bosslar
| Boss | Eski | Yeni |
|---|---|---|
| Elementalist (final) | 100 can / 15 hasar / 100 altın | **150 / 14 / 0** (oyunun sonu, altının işe yaramıyor) |
| Protector (ara boss, elite) | 150 / 20 / 25, 3 turda bir +20 can | **70 / 10 / 30**, 3 turda bir **+10** can |
| Protector minyonu | sahnede atanan | **Bloom Sprout** (22 / 6), Editor aracı atar |

Protector minyonlar yaşarken hasar almıyor. Bu yüzden 70 can + 2×22 minyon, toplamda ~115 can eder.

## 6. Dükkan

| Eşya | Eski | Yeni |
|---|---|---|
| Bandage | Tam can, 30 altın | Tam can, **40** |
| Camp Fire | +15 maks. can, 30 | **+12**, 30 |
| Gambler's Hand | +2 el boyutu, 25 | **Her tur +1 kart çek, 40** (yeni `ItemEffectType.IncreaseDrawPerTurn`) |

Reroll 10 altın, aynı. Altın akışı: normal savaş ~10, elite ~28 altın. Koşu boyunca 1–2 eşya alınabiliyor.

## 7. Harita (patika) oranları

| Düğüm | Eski | Yeni | Neden |
|---|---|---|---|
| Combat | 40 | **45** | Altın ve ilerleme kaynağı |
| Elite | 10 | **12** | Elite artık adil. Kural: 3. katmandan önce yok |
| Shop | 10 | **12** | Kural: 2. katmandan önce yok (cepte altın yok) |
| Diviner | 10 | **8** | Mevcut kural aynı: ilk 2 ve son 2 katmanda yok |
| Companion | 15 | **10** | Yoldaşlar güçlendi, sıklık düştü |
| Mystery | 15 | **13** | |

Harita uzunluğu 10 katman (aynı). En fazla 2 savaşsız düğüm üst üste gelebilir (mevcut kural).

## 8. Yeni değerlerle simülasyon

3000 koşu:

| Deste | Kazanma | Normal | Elite | Boss |
|---|---|---|---|---|
| Tipik (karışık) | **%67** | 6.0 can, 2.7 tur | 18.3, 6.2 tur | 37.1, 5.9 tur |
| Güçlü (güç-3 kartlar) | **%79** | 5.5, 2.2 tur | 15.8, 4.5 tur | 28.0, 4.4 tur |
| Hava + Air Supportive | **%55** | 5.3, 2.9 tur | 16.2, 12.3 tur | 28.1, 6.7 tur |
| Su/Toprak + iki güçlendirici | **%58** | 5.9, 3.3 tur | 20.9, 11.7 tur | 25.3, 8.0 tur |
| Ateş + Fire Supportive | **%55** | 5.7, 2.9 tur | 17.6, 4.7 tur | 36.6, 8.2 tur |

- Desteler arası fark: eskiden 49 puan, şimdi **24 puan** (%55–79).
- Elite'in can bedeli 45'ten **~16–21'e** düştü.
- Ölümler artık haritanın ikinci yarısında ve boss'ta toplanıyor.

**Bilinen kalıntılar**
- **5. katmanda can basamağı.** Güçlü havuza geçişte can eğrisinde bir basamak var (%90 → %75). "Ortada birden zorlaştı" geri bildirimi gelirse ilk ayar, `earlyEnemies`'e Fire Arrows'u eklemek.
- **Güç-3 destesi hâlâ en üstte (%79).** Sebep, element seçimine bağlı olmaması. Fark büyük gelirse ilk ayar, güç-3 kartları 20'den 19 hasara çekmek.
- **Tek elementli destelerde elite uzun sürüyor (~12 tur).** Elite'in yarısı Toprak elementli Protector ve onun minyonları. Hava (0.5x) ya da Su/Toprak (1x) destesi onu yavaş kesiyor. Bu, element seçiminin bilinçli bedeli. Oyunda sıkıcı gelirse Protector'ın canı 70'ten 60'a indirilebilir.

## 9. Uygulama durumu

| Parça | Nerede | Durum |
|---|---|---|
| Kart, düşman, boss, eşya sayıları ve açıklamaları | `Assets/CardData`, `EnemyData`, `ItemData` (.asset) | ✅ Uygulandı |
| Enerjinin savaş başında dolması | `EnergyManager.RefillToMax`, `TurnManager.SetupBattle` | ✅ |
| Açılış çekişi, tur başı çekme alanı | `TurnManager.openingDraw`, `DeckManager.drawPerTurn` | ✅ |
| Erken/geç düşman havuzu, elite çarpanları ve sayısı | `TurnManager` (`earlyEnemies`, `elite*`) | ✅ Kod. Havuz ataması Editor aracıyla |
| Elite/dükkan katman kuralı | `MapGenerator.eliteMinLayer`, `shopMinLayer` | ✅ |
| Protector iyileşmesi | `Boss_Protector.healAmount` | ✅ |
| Gambler's Hand yeni etkisi | `ItemEffectType.IncreaseDrawPerTurn`, `ShopManager` | ✅ |
| Water/Earth/Air Supportive kartları, güçlendirici bedeli 1 | `Assets/CardData/*Supportive.asset` | ✅ Asset'ler hazır. Havuza ve Günlüğe Editor aracı ekler |
| **Sahnedeki değerler** (can 80, enerji 6, el 8, harita oranları, düşman havuzları, minyon, Exchange bedeli, yeni kartların başlangıç havuzuna ve Günlüğe eklenmesi) | `SampleScene`, `Menu Scene` | ⏳ **Unity'de: Path of Elements > Balance > 1 - Preview, sonra 2 - Apply** |

Sahne dosyası buradan okunamadığı ve YAML elle düzenlenmediği için sahne değerlerini Editor aracı yazıyor. Kodda varsayılanı değiştirmek, sahnede kayıtlı eski değeri ezmez. Preview, her alanın şu anki ve yeni değerini Console'a yazar.

## 10. Varsayımlar ve sınırlar

- Simülasyondaki oyuncu açgözlü: her tur en çok hasar/iyileşme veren kartları seçiyor, Exchange'e saldırı kartı atmıyor, ayrık komboyu kullanmıyor. Bu yüzden insan oyuncudan biraz zayıf.
- Başlangıç destesi havuzu sahnede (`DeckManager.allCardsInDeck`). Simülasyon üç örnek 12'lik deste kullanıyor (`DECKS`).
- Diviner simülasyonda etkisiz sayıldı. Gerçekte bir sonraki düğümün türünü değiştiriyor.
- Kart/düşman sayıları `balance_sim.py` içine elle girildi. Asset'ler değişirse oradaki tablo da güncellenmeli.

## 11. Oyun testi listesi (Unity'de)

1. Console'da derleme hatası yok.
2. **Balance > Apply** çalıştırıldı ve Console çıktısı beklenen değişiklikleri gösteriyor.
3. İkinci savaşa enerji 6/6 ile başlanıyor.
4. İlk 2 katmanda elite yok, ilk katmanda dükkan yok.
5. Gambler's Hand alındıktan sonra her tur 3 kart çekiliyor.
6. Marauders ve Mushroom doğru element ikonu ve çerçevesiyle görünüyor.
7. Protector savaşında minyonlar Bloom Sprout, boss 3. turda +10 iyileştiriyor.
8. Ice Spear, Magma Punch ve Quake kartlarında 3 enerji yazıyor. Kart metinleri taşmıyor.
9. Tam bir koşu: final boss 5–7 tur sürüyor.
10. Deste seçim ekranında Water/Earth/Air Supportive kartları görünüyor ve doğru element çerçevesiyle çıkıyor.
11. Bir güçlendiriciyi aynı elementteki kartın sağına koyunca toplam bedel +1 enerji oluyor ve hasar %50 artıyor.
