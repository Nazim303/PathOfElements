using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("Player Board")]
    public CardData[] playerSlots = new CardData[4];
    public Transform[] slotTransforms;

    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public Transform enemySpawnPoint;

    public List<EnemyDisplay> activeEnemies = new List<EnemyDisplay>();

    [Header("Stats Debug")]
    public int activeEnemyCount;

    public BossBehavior currentBoss; 

    public EnemyData defaultMinion;

    public GameObject enemyDeathEffectPrefab;

    [Header("Fusion System")]
    public List<FusionRecipe> allFusionRecipes; // Inspector'dan tüm tarifleri buraya at
    public GameObject cardPrefab;

    [Header("Visuals")]
    public Transform discardPoint;

    // LAG FIX
    public void SetupEnemies(List<EnemyData> enemiesToSpawn)
    {
        ClearEnemies();
        currentBoss = null; 

        StartCoroutine(SpawnEnemiesRoutine(enemiesToSpawn));
    }

    IEnumerator SpawnEnemiesRoutine(List<EnemyData> enemiesToSpawn)
    {

       

        // Düþmanlarý tek seferde, beklemeden yarat
        foreach (EnemyData data in enemiesToSpawn)
        {
            GameObject newEnemyObj = Instantiate(enemyPrefab, enemySpawnPoint);
            EnemyDisplay display = newEnemyObj.GetComponent<EnemyDisplay>();

            // Verileri yükle
            display.SetupEnemy(data, data.maxHealth);

            // Listeye ekle
            activeEnemies.Add(display);

            // Animasyon YOK. Direkt görünür olsunlar.
            newEnemyObj.transform.localScale = Vector3.one;
            if (data.enemyName.Contains("Protector"))
            {
                currentBoss = newEnemyObj.AddComponent<Boss_Protector>();
                ((Boss_Protector)currentBoss).minionData = defaultMinion;
                // GEÇÝCÝ ÇÖZÜM: Minyon olarak kendisinin kopyasýný deðil, rastgele bir normal düþman atasýn
                // Bunun için TurnManager'dan AllEnemies listesine eriþebiliriz veya basitçe inspector'dan atanmasýný bekleriz.
                // Þimdilik koddan atamayý kaldýrýyorum, Boss_Protector scriptine inspector'dan minyon atamalýsýn.
                // ((Boss_Protector)currentBoss).minionData = enemiesToSpawn[0]; <--- BU SATIRI SÝL

                currentBoss.Setup(this, display);
            }
            else if (data.enemyName.Contains("Elementalist"))
            {
                currentBoss = newEnemyObj.AddComponent<Boss_Elementalist>();
                currentBoss.Setup(this, display);
            }
        }

        activeEnemyCount = activeEnemies.Count;
        Debug.Log($"Savaþ Baþladý! {activeEnemies.Count} düþman anýnda belirdi.");

        yield return null; // Bir kare bekle (Sistem nefes alsýn)
    }

    void ClearEnemies()
    {
        foreach (EnemyDisplay enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
    }

    public void UpdateSlot(int index, CardData data)
    {
        if (index >= 0 && index < playerSlots.Length)
        {
            playerSlots[index] = data;
            RecalculateTotalEnergyCost(); // --- ÇAÐIR ---
        }
    }

    public void ClearSlot(int index)
    {
        if (index >= 0 && index < playerSlots.Length)
        {
            playerSlots[index] = null;
            RecalculateTotalEnergyCost(); // --- ÇAÐIR ---
        }
    }

    // --- SALDIRI MANTIÐI (COMBO & HEAL MEVCUT) ---
    // --- GÜNCELLENMÝÞ SALDIRI MANTIÐI ---
    public IEnumerator OnAttackPhase()
    {
        // 1. TAM KOMBO (4 Ayný Element)
        bool isFullCombo = CheckElementalCombo();
        if (isFullCombo) Debug.Log("?? TAM ELEMENTAL KOMBO (AOE) AKTÝF! ??");

        // 2. ÝKÝLÝ KOMBO (2 Ayný - 2 Ayný)
        bool isSplitCombo = CheckTwoPairCombo();
        if (isSplitCombo) Debug.Log("?? AYRIK ÝKÝLÝ KOMBO (SPLIT) AKTÝF! ??");

        // Varsayýlan Hedef (Öndeki)
        EnemyDisplay frontEnemy = null;
        if (activeEnemies.Count > 0) frontEnemy = activeEnemies[0];

        Debug.Log("--- TUR BAÞLIYOR ---");

        for (int i = 0; i < playerSlots.Length; i++)
        {
            // Slot dolu mu ve içinde obje var mý kontrol et
            if (playerSlots[i] != null && slotTransforms[i].childCount > 0)
            {
                Transform cardTransform = slotTransforms[i].GetChild(0);
                CardData card = playerSlots[i];

                // --- 1. ADIM: ENERJÝ KONTROLÜ ---
                bool canAfford = EnergyManager.instance.TrySpendEnergy(card.energyCost);

                if (!canAfford)
                {
                    // >>> KART YUTULMA (FIZZLE) EFEKTÝ <<<
                    Debug.Log($"Yetersiz Enerji! {card.cardName} yutuluyor...");

                    // A. Görseli Kýrmýzý Yap (Hata hissi)
                    CardDisplay display = cardTransform.GetComponent<CardDisplay>();
                    if (display != null && display.artImage != null) 
                        display.artImage.color = Color.red;

                    // B. Titreme Animasyonu (Saða sola salla)
                    Vector3 basePos = cardTransform.localPosition;
                    float shakeDuration = 0.4f;
                    float shakeElapsed = 0f;

                    while (shakeElapsed < shakeDuration)
                    {
                        // Sinüs dalgasý ile hýzlý titreme
                        float xOffset = Mathf.Sin(shakeElapsed * 50f) * 5f; 
                        cardTransform.localPosition = basePos + new Vector3(xOffset, 0, 0);
                        shakeElapsed += Time.deltaTime;
                        yield return null;
                    }
                    cardTransform.localPosition = basePos; // Yerine oturt

                    // C. Küçülerek Yok Olma (Shrink)
                    float shrinkDuration = 0.3f;
                    float shrinkElapsed = 0f;
                    Vector3 startScale = cardTransform.localScale;

                    while (shrinkElapsed < shrinkDuration)
                    {
                        float t = shrinkElapsed / shrinkDuration;
                        cardTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                        shrinkElapsed += Time.deltaTime;
                        yield return null;
                    }
                    cardTransform.localScale = Vector3.zero;

                    // D. Temizlik (Ýskartaya at ve yok et)
                    DeckManager dm = FindObjectOfType<DeckManager>();
                    if (dm != null)
                    {
                        dm.AddToDiscard(card);
                        // YENÝ: Direk yok etmek yerine uçur
                        dm.StartCoroutine(dm.AnimateCardToDiscard(cardTransform.gameObject));
                    }
                    else
                    {
                        Destroy(cardTransform.gameObject);
                    }

                    playerSlots[i] = null; // Veriyi de temizle

                    yield return new WaitForSeconds(0.2f);
                    continue; // Sonraki karta geç
                }

                // --- 2. ADIM: BAÞARILI SALDIRI ANÝMASYONU ---
                // (Enerji yetti, normal akýþ devam ediyor)
                
                Vector3 originalPos = cardTransform.localPosition;
                Vector3 punchPos = originalPos + new Vector3(0, 50f, 0);
                float animT = 0;
                while (animT < 1) 
                { 
                    animT += Time.deltaTime * 10; 
                    cardTransform.localPosition = Vector3.Lerp(originalPos, punchPos, animT); 
                    yield return null; 
                }

                // --- 3. ADIM: EFEKTLERÝN UYGULANMASI ---

                if (card.cardType == CardType.Companion)
                {
                    // --- COMPANION EFEKTLERÝ ---
                    switch (card.specialEffect)
                    {
                        case SpecialEffect.HalveEnemyHealth:
                            if (frontEnemy != null)
                            {
                                int halfHp = frontEnemy.currentHealth / 2;
                                int damage = frontEnemy.currentHealth - halfHp;
                                frontEnemy.currentHealth = halfHp;
                                frontEnemy.UpdateHealthUI(frontEnemy.currentHealth);
                                StartCoroutine(frontEnemy.TakeDamageEffect());
                            }
                            break;
                        case SpecialEffect.FullPlayerHeal:
                            if (PlayerManager.instance != null)
                            {
                                int missingHealth = PlayerManager.instance.maxHealth - PlayerManager.instance.currentHealth;
                                PlayerManager.instance.Heal(missingHealth);
                            }
                            break;
                        case SpecialEffect.AOE_Damage:
                            List<EnemyDisplay> targets = new List<EnemyDisplay>(activeEnemies);
                            foreach (EnemyDisplay enemy in targets)
                            {
                                if (enemy != null) { ApplyDamageToEnemy(card, enemy); StartCoroutine(enemy.TakeDamageEffect()); }
                            }
                            break;
                    }
                    if (FindObjectOfType<DeckManager>() != null) FindObjectOfType<DeckManager>().RemoveCardFromDeck(card);
                }
                else if (card.cardType == CardType.Heal)
                {
                    // --- HEAL KARTI ---
                    if (PlayerManager.instance != null) PlayerManager.instance.Heal(card.healAmount);
                }
                else // --- NORMAL SALDIRI KARTLARI ---
                {
                    if (activeEnemies.Count > 0)
                    {
                        if (isFullCombo)
                        {
                            // TAM KOMBO
                            List<EnemyDisplay> targets = new List<EnemyDisplay>(activeEnemies);
                            foreach (EnemyDisplay enemy in targets)
                            {
                                if (enemy != null) { ApplyDamageToEnemy(card, enemy); StartCoroutine(enemy.TakeDamageEffect()); }
                            }
                        }
                        else if (isSplitCombo)
                        {
                            // AYRIK KOMBO
                            EnemyDisplay target = null;
                            if (i < 2) 
                            {
                                if (activeEnemies.Count > 0) target = activeEnemies[0];
                            }
                            else 
                            {
                                if (activeEnemies.Count > 0) target = activeEnemies[activeEnemies.Count - 1];
                            }

                            if (target != null)
                            {
                                ApplyDamageToEnemy(card, target);
                                StartCoroutine(target.TakeDamageEffect());
                            }
                        }
                        else
                        {
                            // KOMBO YOK
                            if (activeEnemies.Count > 0)
                            {
                                frontEnemy = activeEnemies[0];
                                ApplyDamageToEnemy(card, frontEnemy);
                                StartCoroutine(frontEnemy.TakeDamageEffect());
                            }
                        }
                    }
                }

                // Geri gel
                animT = 0;
                while (animT < 1) 
                { 
                    animT += Time.deltaTime * 10; 
                    cardTransform.localPosition = Vector3.Lerp(punchPos, originalPos, animT); 
                    yield return null; 
                }

                yield return new WaitForSeconds(0.2f);
                HandleDeaths(); // Ölüleri temizle
            }
        }
    }

    bool CheckTwoPairCombo()
    {
        // 1. Tüm slotlar dolu mu?
        for (int i = 0; i < playerSlots.Length; i++) { if (playerSlots[i] == null) return false; }

        // 2. Ýlk çift ayný mý? (0 ve 1)
        bool firstPairMatch = playerSlots[0].element == playerSlots[1].element;

        // 3. Son çift ayný mý? (2 ve 3)
        bool secondPairMatch = playerSlots[2].element == playerSlots[3].element;

        // 4. Ýki grup BÝRBÝRÝNDEN FARKLI mý? 
        // (Eðer hepsi aynýysa o zaman Tam Kombo olur, bu fonksiyon false dönmeli ki çakýþmasýn)
        bool pairsAreDifferent = playerSlots[0].element != playerSlots[2].element;

        return firstPairMatch && secondPairMatch && pairsAreDifferent;
    }

    bool CheckElementalCombo()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] == null) return false;
        }

        ElementType firstElement = playerSlots[0].element;

        for (int i = 1; i < playerSlots.Length; i++)
        {
            if (playerSlots[i].element != firstElement) return false;
        }

        return true;
    }

    public void SpawnExtraEnemy(EnemyData data)
    {
        GameObject newEnemyObj = Instantiate(enemyPrefab, enemySpawnPoint);
        EnemyDisplay display = newEnemyObj.GetComponent<EnemyDisplay>();
        display.SetupEnemy(data, data.maxHealth);

        // --- KRÝTÝK DEÐÝÞÝKLÝK ---
        // Eskiden: activeEnemies.Add(display); -> Sona ekliyordu.
        // Þimdi: Insert(0, display) yapýyoruz.
        // Bu sayede minyon listenin EN BAÞINA geçiyor ve oyuncu önce ona vuruyor.
        activeEnemies.Insert(0, display);

        // Görsel olarak da baþa al (Layout Group soldan saða diziyorsa sol baþa geçer)
        newEnemyObj.transform.SetAsFirstSibling();

        newEnemyObj.transform.localScale = Vector3.one;
        activeEnemyCount = activeEnemies.Count;
    }

    void ApplyDamageToEnemy(CardData card, EnemyDisplay target)
    {
        if (target == null) return;

        BossBehavior boss = target.GetComponent<BossBehavior>();
        if (boss != null && boss.IsInvulnerable())
        {
            Debug.Log("SALDIRI ÝÞLEMEDÝ! Boss þu an ölümsüz (Minyonlarý öldür).");
            // Kalkan efekti sesi veya görseli eklenebilir
            return;
        }

        float multiplier = ElementSystem.GetDamageMultiplier(card.element, target.enemyData.element);
        int finalDamage = Mathf.RoundToInt(card.baseDamage * multiplier);

        target.currentHealth -= finalDamage;
        target.UpdateHealthUI(target.currentHealth);

        Debug.Log($"{card.cardName} vurdu -> {target.enemyData.enemyName} ({finalDamage} dmg)");
    }

    void HandleDeaths()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i].currentHealth <= 0)
            {
                EnemyDie(activeEnemies[i]);
            }
        }
    }

    void EnemyDie(EnemyDisplay enemy)
    {
        Debug.Log($"{enemy.enemyData.enemyName} öldü!");

        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.AddGold(enemy.enemyData.goldReward);
        }

        activeEnemies.Remove(enemy);

        // --- YENÝ: EFEKT VE ANÝMASYON ---

        // 1. Ölüm efektini yarat (Patlama)
        if (enemyDeathEffectPrefab != null)
        {
            GameObject effect = Instantiate(enemyDeathEffectPrefab, enemy.transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Efekti temizle
        }

        // 2. Düþmaný anýnda yok etmek yerine, kendi yok olma animasyonunu baþlat
        // Destroy(enemy.gameObject); // ESKÝ KOD
        StartCoroutine(enemy.DieAnimationAndDestroy()); // YENÝ KOD

        // -------------------------------

        activeEnemyCount = activeEnemies.Count;
    }


    void RecalculateTotalEnergyCost()
    {
        int totalCost = 0;
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null)
            {
                totalCost += playerSlots[i].energyCost;
            }
        }

        // EnergyManager'a bildir
        if (EnergyManager.instance != null)
        {
            EnergyManager.instance.UpdatePendingCost(totalCost);
        }
    }

    public FusionRecipe CheckFusion(CardData a, CardData b)
    {
        foreach (FusionRecipe recipe in allFusionRecipes)
        {
            // A+B veya B+A kombinasyonlarýný kontrol et (Sýra önemli olmasýn)
            if ((recipe.cardA == a && recipe.cardB == b) || (recipe.cardA == b && recipe.cardB == a))
            {
                return recipe;
            }
        }
        return null; // Fizyon yok
    }

    public IEnumerator DestroyPlayedCards()
    {
        DeckManager deckManager = FindObjectOfType<DeckManager>();

        for (int i = 0; i < slotTransforms.Length; i++)
        {
            if (slotTransforms[i].childCount > 0)
            {
                foreach (Transform child in slotTransforms[i])
                {
                    GameObject cardObj = child.gameObject;

                    if (playerSlots[i] != null && deckManager != null)
                    {
                        if (playerSlots[i].cardType != CardType.Companion)
                        {
                            deckManager.AddToDiscard(playerSlots[i]);
                        }
                    }

                    // --- YENÝ: Kartý iskartaya uçur ---
                    if (deckManager != null)
                    {
                        deckManager.StartCoroutine(deckManager.AnimateCardToDiscard(cardObj));
                    }
                    else
                    {
                        Destroy(cardObj);
                    }
                }
            }
            playerSlots[i] = null;
        }

        yield return new WaitForSeconds(0.4f); // Uçma animasyonunun bitmesini bekle
        Debug.Log("Masa temizlendi ve kartlar iskartaya yollandý.");
        RecalculateTotalEnergyCost();
    }
}