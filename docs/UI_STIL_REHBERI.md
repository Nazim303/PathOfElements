# UI Stil Rehberi — Taslak (2026-11-26)

Amaç: oyunun her ekranı aynı dünyadan gelmiş gibi görünsün. Kaynak olarak bitmiş kart çizimleri ve kart çerçeveleri kullanıldı: karanlık, boyalı fantastik, altın süslemeli çerçeveler.

## 1. Fontlar

### Şu anki durum
| Font | Nerede | Sorun |
|---|---|---|
| **Comfortaa** | Neredeyse tüm metinler (~75 yer) | Yuvarlak, modern, "uygulama" havasında. Karanlık fantastik çizimlerle çelişiyor. |
| **Quintessential** | Birkaç başlık | Kaligrafik, temaya uygun ama küçük boyda okunmuyor |
| LiberationSans | SampleScene'de 2 metin | TMP'nin varsayılanı, unutulmuş |
| Ancient Medium, BlackFlag | `Assets/` kökünde, **kullanılmıyor** | Lisansları belirsiz |

### Öneri: 2 font
| Rol | Font | Neden |
|---|---|---|
| **Başlık** (kart adı, ekran başlıkları, butonlar) | **Cinzel** (Bold) | Roma yazıtı havası, kart çerçevesindeki altın süslerle uyumlu, büyük harfte çok okunaklı |
| **Metin** (kart açıklaması, tutorial, tooltip) | **Alegreya** veya **Crimson Pro** | Kitap serifi, küçük boyda okunaklı, "günlük" temasına uyuyor |

- İkisi de Google Fonts'ta, **SIL Open Font License** ile ücretsiz ve ticari kullanıma açık. Türkçe karakterleri destekliyorlar.
- Quintessential sadece özel anlarda kalabilir (boss adı, "VICTORY" gibi). Bu isteğe bağlı.
- Comfortaa, LiberationSans, Ancient Medium ve BlackFlag projeden çıkar.

### Nasıl uygulanır
Fontlar indirildi: `Assets/Font/Cinzel`, `Assets/Font/Alegreya` (lisans dosyaları yanlarında). Önizleme: `docs/font_onizleme.png`.
Unity'de **Path of Elements > Fonts** menüsünden (`Assets/Editor/FontSwapTool.cs`):
1. **1 - Create TMP Font Assets**: SDF asset'leri oluşturur.
2. **2 - Preview (Dry Run)**: Console'da hangi metnin hangi fonta geçeceğini listeler, hiçbir şeyi kaydetmez.
3. **3 - Apply**: prefab'ları ve Build Settings'teki sahneleri günceller ve kaydeder.

Kural: punto ≥ 40, eski font Quintessential ya da obje adında title/header/name geçiyorsa **Cinzel**; geri kalan her şey **Alegreya**.
Not: Eski metinlerde özel materyal preset'i (outline, glow) varsa varsayılan materyale döner. Uygulamadan sonra ekranlar tek tek kontrol edilmeli.

## 2. Renkler
Kart çerçevelerinden alındı:

| Rol | Renk | Kullanım |
|---|---|---|
| Ateş | `#C8321E` | element etiketleri, hasar sayıları |
| Su | `#1E5FC8` | |
| Toprak | `#D2781E` | |
| Hava | `#28B478` | |
| Nötr | `#7A7A80` | destek kartları, pasif durumlar |
| **Altın (vurgu)** | `#D4AF37` | çerçeveler, seçili durum, önemli butonlar |
| Panel arka planı | `#14101C`, %90–95 opak | tüm paneller (GameOver paneli zaten `0.08, 0.06, 0.12`, buna yakın) |
| Ana metin | `#EDE6D6` (fildişi) | saf beyaz yerine, daha sıcak |
| İkincil metin | `#A89F8C` | açıklamalar, ipuçları |

**Kural:** Element renkleri sadece elementi anlatmak için kullanılır. Butonlar ve dekor altın ya da nötr olur.

## 3. Paneller ve butonlar
- **Paneller:** koyu arka plan, ince altın kenarlık, köşelerde süs. `EPIC NEW UI_s` setindeki çerçeveler kullanılır.
- **Butonlar:** üç durum: normal, üzerine gelince (altın parlama ve %5 büyüme, DOTween) ve basılı. Tüm butonlarda aynı süreler: 0.12 sn.
- **Tema sprite'ları (2026-11-26):** `Assets/Sprites/UI Theme/` — `Tools/ui_theme_sprites.py` üretir (deterministik, tekrar çalıştırmak güvenli). Önizleme: `docs/ui_tema_onizleme.png`.
  | Sprite | Kullanım | 9-slice |
  |---|---|---|
  | `UI_PanelFrame` | ana paneller (GameOver, PopUp, Journal) — altın çerçeve, köşe elmasları | 64 |
  | `UI_PanelPlain` | iç paneller, liste öğeleri, küçük paneller (<260 px) | 20 |
  | `UI_Backdrop` | tam ekran arka plan (vinyet) | yok |
  | `UI_Button` (+ `_Hover`, `_Pressed`, `_Disabled`) | butonlar, Sprite Swap geçişi | 36×30 |
  | `UI_BarTrack` / `UI_BarFill` / `UI_Knob` | slider'lar (Fill gri tonlu, `Image.color` ile boyanır) | 18 |
  | `UI_BarTrack` / `UI_ScrollHandle` | scrollbar'lar | 18 |
  | `UI_Divider` | başlık altı ayırıcı (elle eklenir) | yok |
- **Uygulama:** Unity'de **Path of Elements > UI Theme** (`Assets/Editor/UIThemeTool.cs`): önce **1 - Preview**, sonra **2 - Apply**. Sadece Unity'nin varsayılan sprite'larını kullanan, görünür Image'lara dokunur; script'lerin referans tuttuğu Image'lar (runtime'da sprite atananlar) atlanır. Koyu panele düşen koyu metinler fildişine çevrilir, temalanan butonlara `UIButtonFx` (hover %5 büyüme + altın metin, 0.12 sn) eklenir.
- **Metin hiyerarşisi:** ekran başlığı 48, panel başlığı 32, gövde 22, küçük not 16 (1080p referans).
- **Animasyon:** panel açılırken 0.25 sn fade ve hafif ölçek. Bu zaten DOTween ile birçok yerde var, süreler eşitlenir.

## 4. Ekran sırası (cilalama yapılırken)
1. **Savaş ekranı:** oyuncunun en çok gördüğü ekran.
2. Kart prefab'ı (`CardPrefab`): font, sayıların okunaklılığı, enerji ikonu.
3. Harita.
4. Ana menü.
5. Dükkan, Taverna, Game Over/Victory.
6. Tutorial popup'ları.

> Bu dosya bir taslak. Font ve renk kararı verilince "Öneri" ifadeleri kurala dönüşecek.
