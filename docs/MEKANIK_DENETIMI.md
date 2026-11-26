# Mekanik Denetimi — Sadeleştirme (2026-11-26)

## ✅ Kararlar ve uygulama durumu
| # | Sistem | Karar | Durum |
|---|---|---|---|
| 1 | Füzyon elementleri (Magma/Steam/Ice/Dust) | Çıktı | ✅ Enum ve hasar tablosundan çıkarıldı, Magma Punch → Fire |
| 3 | Tam kombo | Kalsın | — |
| 3 | Ayrık kombo | **Kalsın (kullanıcı kararı)** | değişmedi |
| 4 | Güçlendirici | Sadeleşti | ✅ Sadece aynı element +%50, ceza yok, tek başına normal saldırı |
| 5 | Füzyon | Çıktı (A) | ✅ Kod, tarif, tutorial aşaması (yerine "Finish It!" final saldırısı) |
| 6 | Exchange slotu | **Aynı kalsın (kullanıcı kararı)** | değişmedi |
| 7 | Diviner / Mystery | **İkisi de kalsın (kullanıcı kararı)** | değişmedi |
| 10 | Bosslar | Elementalist final, Protector ara boss | ✅ Boss düğümü Elementalist'i seçer; Elite düğümü %50 Protector |
| 11 | Düşman isimleri | — | Gerek yok: oyun içi isimler zaten görsellere uygun (sadece dosya adları farklı). "Shininig Worm" yazım hatası düzeltildi |

Aşağıdaki metin karar öncesi analizdir.

Hexodia ve zemin kartları çıkarıldıktan sonra oyunda kalan sistemler bunlar. Her biri için bir öneri yazdım: **Kalsın / Sadeleşsin / Çıksın**. Karar senin. `[ ]` kutusuna işaret koyman yeterli, sonra uygularım.

Ölçüt: Bir sistem oyuncuya ilk 5 dakikada açıklanamıyorsa, ya da çizim eksikliği yüzünden yarım görünüyorsa sadeleştirilir.

## Çekirdek (oyunun kimliği)

### 1. Element döngüsü: **Kalsın**
Ateş > Toprak > Hava > Su > Ateş. Avantajlı vuruş 2x, dezavantajlı 0.5x. Basit, okunaklı, oyunun adını taşıyor.
- **Sadeleşsin:** `ElementType` içinde Magma, Steam, Ice ve Dust füzyon elementleri de var, ama bunları sadece *Magma Punch* kullanıyor. Füzyon sadeleşirse (bkz. 5) bu 4 elementin hasar tablosu da çıkabilir.
- [ ] Kalsın  [ ] Füzyon elementleri çıksın

### 2. Enerji: **Kalsın, sayılar gözden geçirilsin**
Maksimum 11 enerji, savaş dolu başlıyor, her tur +2 geliyor. Kartlar 0–5 enerji tutuyor. Sistem çalışıyor. Ama 11 ile +2 alışılmadık sayılar; denge tablosu çıkınca bakılacak.
- [ ] Kalsın

### 3. Dört slot ve kombolar: **Sadeleşsin**
- **Tam kombo:** 4 kart aynı elementse herkese vurur (AOE). Anlaşılır, **kalsın**.
- **Ayrık kombo:** 1–2 ve 3–4 numaralı slotlar ikişer kart aynı elementse sol çift ilk düşmana, sağ çift son düşmana vurur. Ekranda nasıl tetiklendiği belli değil, oyuncuya öğretmesi zor. **Çıksın.**
- [ ] Tam kombo kalsın  [ ] Ayrık kombo çıksın

### 4. Güçlendirici kartlar (`isElementalModifier`): **Sadeleşsin**
Bir saldırı kartının sağına konan güçlendirici, aynı elementteyse hasarı 1.5x yapıyor. Farklı elementteyse 0.8x, zıt elementteyse 0.4x'e düşürüyor. Tek başına oynanırsa hiçbir şey yapmadan kayboluyor. Exchange'e atılırsa 2 kat can bedeli istiyor.
Ceza tarafı oyuncuyu cezalandırıyor ama ne olduğunu göstermiyor.
- **Öneri:** Sadece aynı elementte +50% versin, diğer durumlarda etkisiz olsun ama ceza da olmasın. Tek başına oynandığında küçük bir etkisi olsun.
- [ ] Önerildiği gibi sadeleşsin  [ ] Olduğu gibi kalsın  [ ] Tamamen çıksın

### 5. Füzyon: **Karar gerekiyor**
Tutorial füzyonu "en güçlü mekanik" diye tanıtıyor, ama oyunda **tek bir tarif** var (`Fizyon1` → Magma Punch). Her yeni tarif yeni bir kart ve yeni bir çizim demek.
- **A) Çıksın:** Tutorial'ın son aşaması da gider. En sade seçenek.
- **B) Küçük kalsın:** Mevcut kartlarla 3–4 tarif kurulur. Sonuç kartları renklendirilmiş eskiz olabilir.
- [ ] A  [ ] B

### 6. Exchange slotu ve can feda: **Sadeleşsin**
Exchange slotunun iki işi var:
(a) Destek kartları buraya atılarak oynanıyor (bedava).
(b) Saldırı kartı buraya atılırsa 10 can (güçlendiriciyse 20) karşılığında kart değişiyor.
Aynı slotun iki farklı kuralı olması tutorial'da uzun bir açıklama gerektiriyor.
- **Öneri:** Destek kartları normal slotlara oynansın. Exchange sadece "can karşılığı kart değiştir" işini yapsın.
- [ ] Önerildiği gibi  [ ] Olduğu gibi kalsın

## Harita ve olaylar

### 7. Düğüm türleri: **7'den 5'e**
Mevcut türler: Combat, Elite, Diviner, Shop, Boss, Companion, Mystery.
- **Diviner:** Kartları karıştırıp sıradaki düğümün türünü değiştiren bir mini oyun. **Mystery** ise türü gizli bir düğüm. İkisi aynı fikre ("kader / belirsizlik") hizmet ediyor.
- **Öneri:** Diviner çıksın, Mystery kalsın. Ya da tersi.
- [ ] Diviner çıksın  [ ] Mystery çıksın  [ ] İkisi de kalsın

### 8. Companion (Taverna): **Kalsın**
3 yoldaş kartı var: düşmanın canını yarıya indiren, oyuncuyu tam iyileştiren ve herkese vuran. Az, net, güçlü. Üçünün görseli de şu an renklendirilmiş eskiz.
- [ ] Kalsın

### 9. Dükkan ve eşyalar: **Kalsın, eşya sayısı artmasın**
3 eşya var (maks. can, bonus hasar, el boyutu). Kodda 2 tür daha tanımlı (maks. enerji, tam iyileşme). Yeni eşya yeni ikon çizimi demek.
- [ ] Kalsın

## Düşmanlar ve bosslar

### 10. Bosslar: **v1 için tek boss**
- **Boss_Protector:** 2 minyon çağırıyor, minyonlar yaşadıkça hasar almıyor, 3 turda bir herkesi iyileştiriyor. Görseli gri tonlu.
- **Boss_Elementalist:** Her tur element değiştiriyor ve element döngüsünü test eden iyi bir final. Ama **görseli yok.**
- **Öneri:** Elementalist final boss olarak kalsın, oyunun temasına daha uygun. Görseli öncelikli olarak bulunsun. Protector elite/ara boss olabilir.
- [ ] Önerildiği gibi  [ ] Sadece Protector

### 11. Düşman isimleri ve görseller
İsimler ile görseller uyuşmuyor (bkz. `GORSEL_DURUMU.md`). Görsellere göre yeniden adlandırmak çok daha ucuz.
- [ ] İsimler görsellere göre değişsin

## Yan sistemler
### 12. Günlük (Journal): **Kalsın, düşük öncelik**
### 13. Başlangıç destesi seçimi: **Kalsın**

## Uygulama sırası (kararlardan sonra)
1. Çıkarılacaklar: kod, sahne ve tutorial temizliği (Hexodia'daki gibi).
2. Sadeleşen kurallar: modifier ve Exchange.
3. Tutorial metinlerinin yeni kurallara göre yeniden yazılması.
4. Denge tablosu (`docs/OYUN_VERISI.md`) ve sayı ayarları.
