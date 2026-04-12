using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// --- YENÝ: ÝSTEDÝÐÝN KADAR FARKLI KOMBO YARATABÝLMEN ÝÇÝN ---
[System.Serializable]
public class UltimateCombo
{
    public string comboName;            // Komboya vereceðin isim (Örn: "Ejderin Uyanýþý")
    public List<CardData> requiredCards;// Hangi kartlar masada olmalý?
    public int ultimateDamage = 9999;   // Bu kombo kaç vuracak?
}


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

    [Header("Ultimate Combos (Kadim Birleþimler)")]
    public List<UltimateCombo> ultimateCombos;
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

            float enemyScale = GlobalScaleManager.instance != null ? GlobalScaleManager.instance.normalEnemyScale : 1f;
            newEnemyObj.transform.localScale = new Vector3(enemyScale, enemyScale, enemyScale);

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
        UltimateCombo activeUltimate = CheckUltimateCombo();
        bool isFullCombo = CheckElementalCombo();
        bool isSplitCombo = CheckTwoPairCombo();

        EnemyDisplay frontEnemy = null;
        if (activeEnemies.Count > 0) frontEnemy = activeEnemies[0];

        Debug.Log("--- TUR BAÞLIYOR ---");

        // --- ULTIMATE ENERJÝ KONTROLÜ (Önceden hesaplýyoruz) ---
        bool canExecuteUltimate = false;
        List<Transform> ultimateCards = new List<Transform>();
        List<CardData> ultimateData = new List<CardData>();

        if (activeUltimate != null)
        {
            int totalUltimateEnergyCost = 0;
            for (int i = 0; i < playerSlots.Length; i++)
            {
                if (playerSlots[i] != null) totalUltimateEnergyCost += playerSlots[i].energyCost;
            }

            // Eðer tüm parçalara yetecek enerji varsa TOPLU ANÝMASYON onayý ver!
            if (EnergyManager.instance.currentEnergy >= totalUltimateEnergyCost)
            {
                canExecuteUltimate = true;

                // Gerçekten enerjiyi harca ve listeleri doldur
                for (int i = 0; i < playerSlots.Length; i++)
                {
                    if (playerSlots[i] != null && slotTransforms[i].childCount > 0)
                    {
                        EnergyManager.instance.TrySpendEnergy(playerSlots[i].energyCost);
                        ultimateCards.Add(slotTransforms[i].GetChild(0));
                        ultimateData.Add(playerSlots[i]);
                    }
                }
            }
            else
            {
                Debug.Log("Ultimate için yeterli enerji yok! Kadim bað koptu, normal saldýrýya dönülüyor...");
            }
        }

        // ========================================================
        // ?? DURUM A: ULTIMATE BAÞARILI (HEPSÝ BÝRDEN KALKIP VURUR)
        // ========================================================
        if (canExecuteUltimate)
        {
            Debug.Log($"?? {activeUltimate.comboName} UYANDI! ??");

            // 1. Hepsini Ayný Anda Yukarý Kaldýr
            float animT = 0;
            List<Vector3> originalPosList = new List<Vector3>();
            List<Vector3> punchPosList = new List<Vector3>();

            foreach (Transform t in ultimateCards)
            {
                originalPosList.Add(t.localPosition);
                punchPosList.Add(t.localPosition + new Vector3(0, 50f, 0));
            }

            while (animT < 1)
            {
                animT += Time.deltaTime * 10;
                for (int k = 0; k < ultimateCards.Count; k++)
                {
                    ultimateCards[k].localPosition = Vector3.Lerp(originalPosList[k], punchPosList[k], animT);
                }
                yield return null;
            }

            // 2. Devasa Hasarý Uygula
            List<EnemyDisplay> targets = new List<EnemyDisplay>(activeEnemies);
            foreach (EnemyDisplay enemy in targets)
            {
                if (enemy != null)
                {
                    ApplyDamageToEnemy(ultimateData[0], enemy, activeUltimate);
                    StartCoroutine(enemy.TakeDamageEffect());
                }
            }
            if (UIShake.instance != null) UIShake.instance.TriggerShake(0.5f, 20f);

            // 3. Hepsini Ayný Anda Geri Ýndir
            animT = 0;
            while (animT < 1)
            {
                animT += Time.deltaTime * 10;
                for (int k = 0; k < ultimateCards.Count; k++)
                {
                    ultimateCards[k].localPosition = Vector3.Lerp(punchPosList[k], originalPosList[k], animT);
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
            HandleDeaths();
        }
        else
        {
            // ========================================================
            // ?? DURUM B: NORMAL SALDIRI / EKSÝK ULTIMATE (Sýrayla tek tek)
            // ========================================================
            if (isFullCombo) Debug.Log("?? TAM ELEMENTAL KOMBO (AOE) AKTÝF! ??");
            else if (isSplitCombo) Debug.Log("?? AYRIK ÝKÝLÝ KOMBO (SPLIT) AKTÝF! ??");

            for (int i = 0; i < playerSlots.Length; i++)
            {
                if (playerSlots[i] != null && slotTransforms[i].childCount > 0)
                {
                    Transform cardTransform = slotTransforms[i].GetChild(0);
                    CardData card = playerSlots[i];

                    // 1. Enerji Kontrolü (Yetmezse Fizze animasyonunu bekler ve sonrakine geçer)
                    if (!EnergyManager.instance.TrySpendEnergy(card.energyCost))
                    {
                        yield return StartCoroutine(FizzleCardRoutine(i, cardTransform, card));
                        continue;
                    }

                    // 2. Tekil Animasyon (Yukarý)
                    Vector3 originalPos = cardTransform.localPosition;
                    Vector3 punchPos = originalPos + new Vector3(0, 50f, 0);
                    float animT = 0;
                    while (animT < 1)
                    {
                        animT += Time.deltaTime * 10;
                        cardTransform.localPosition = Vector3.Lerp(originalPos, punchPos, animT);
                        yield return null;
                    }

                    // 3. Efektlerin Uygulanmasý
                    if (card.cardType == CardType.Companion)
                    {
                        switch (card.specialEffect)
                        {
                            case SpecialEffect.HalveEnemyHealth:
                                if (frontEnemy != null)
                                {
                                    int halfHp = frontEnemy.currentHealth / 2;
                                    frontEnemy.currentHealth = halfHp;
                                    frontEnemy.UpdateHealthUI(frontEnemy.currentHealth);
                                    StartCoroutine(frontEnemy.TakeDamageEffect());
                                }
                                break;
                            case SpecialEffect.FullPlayerHeal:
                                if (PlayerManager.instance != null)
                                    PlayerManager.instance.Heal(PlayerManager.instance.maxHealth - PlayerManager.instance.currentHealth);
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
                        if (PlayerManager.instance != null) PlayerManager.instance.Heal(card.healAmount);
                    }
                    else // Normal Saldýrý Kartlarý (Düzeltilmiþ Kombo Mantýðý)
                    {
                        if (activeEnemies.Count > 0)
                        {
                            if (isFullCombo)
                            {
                                // TAM KOMBO (AOE)
                                List<EnemyDisplay> targets = new List<EnemyDisplay>(activeEnemies);
                                foreach (EnemyDisplay enemy in targets)
                                {
                                    if (enemy != null) { ApplyDamageToEnemy(card, enemy); StartCoroutine(enemy.TakeDamageEffect()); }
                                }
                            }
                            else if (isSplitCombo)
                            {
                                // AYRIK KOMBO (0-1 Öne, 2-3 Arkaya)
                                EnemyDisplay target = null;
                                if (i < 2) { if (activeEnemies.Count > 0) target = activeEnemies[0]; }
                                else { if (activeEnemies.Count > 0) target = activeEnemies[activeEnemies.Count - 1]; }

                                if (target != null) { ApplyDamageToEnemy(card, target); StartCoroutine(target.TakeDamageEffect()); }
                            }
                            else
                            {
                                // KOMBO YOK (Düz Vuruþ)
                                frontEnemy = activeEnemies[0];
                                ApplyDamageToEnemy(card, frontEnemy);
                                StartCoroutine(frontEnemy.TakeDamageEffect());
                            }
                        }
                    }

                    // 4. Tekil Animasyon (Geri Ýner)
                    animT = 0;
                    while (animT < 1)
                    {
                        animT += Time.deltaTime * 10;
                        cardTransform.localPosition = Vector3.Lerp(punchPos, originalPos, animT);
                        yield return null;
                    }

                    yield return new WaitForSeconds(0.2f);
                    HandleDeaths();
                }
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

        newEnemyObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        activeEnemyCount = activeEnemies.Count;
    }

    // --- YENÝ PARAMETRE: UltimateCombo activeUltimate ---
    void ApplyDamageToEnemy(CardData card, EnemyDisplay target, UltimateCombo activeUltimate = null)
    {
        if (target == null) return;

        BossBehavior boss = target.GetComponent<BossBehavior>();
        if (boss != null && boss.IsInvulnerable() && activeUltimate == null)
        {
            Debug.Log("SALDIRI ÝÞLEMEDÝ! Boss þu an ölümsüz (Minyonlarý öldür).");
            return;
        }

        int finalDamage = 0;

        if (activeUltimate != null)
        {
            // ULTIMATE KURALLARI: Sýnýfýndan gelen özel hasarý vur!
            finalDamage = activeUltimate.ultimateDamage;
            Debug.Log($" {activeUltimate.comboName} AKTÝF! ");
        }
        else
        {
            // NORMAL HASAR KURALLARI
            float multiplier = ElementSystem.GetDamageMultiplier(card.element, target.enemyData.element);

            if (TerrainManager.instance != null && TerrainManager.instance.currentTerrain != ElementType.None)
            {
                ElementType activeTerrain = TerrainManager.instance.currentTerrain;

                if (card.element == activeTerrain) multiplier += 0.5f;
                else if (ElementSystem.GetDamageMultiplier(activeTerrain, card.element) > 1.0f)
                {
                    multiplier -= 0.5f;
                    if (multiplier < 0.1f) multiplier = 0.1f;
                }
            }

            finalDamage = Mathf.RoundToInt(card.baseDamage * multiplier);
        }

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

    // Hangi kombonun tuttuðunu bulur ve onu geri döndürür. (Bulamazsa null döner)
    UltimateCombo CheckUltimateCombo()
    {
        if (ultimateCombos == null || ultimateCombos.Count == 0) return null;

        // Inspector'a eklediðin her bir komboyu sýrayla kontrol et
        foreach (UltimateCombo combo in ultimateCombos)
        {
            if (combo.requiredCards == null || combo.requiredCards.Count == 0) continue;

            List<CardData> tempPieces = new List<CardData>(combo.requiredCards);

            for (int i = 0; i < playerSlots.Length; i++)
            {
                if (playerSlots[i] == null) continue;

                if (tempPieces.Contains(playerSlots[i]))
                {
                    tempPieces.Remove(playerSlots[i]);
                }
            }

            // Eðer bu kombonun aradýðý tüm kartlar masada bulunduysa (liste boþaldýysa)
            if (tempPieces.Count == 0)
            {
                return combo; // Baþarýlý komboyu geri gönder!
            }
        }

        return null; // Hiçbir kombo tutmadý
    }

    // Enerjisi yetmeyen kartlarýn titreyip yok olma animasyonu
    IEnumerator FizzleCardRoutine(int slotIndex, Transform cardTransform, CardData card)
    {
        Debug.Log($"Yetersiz Enerji! {card.cardName} yutuluyor...");

        CardDisplay display = cardTransform.GetComponent<CardDisplay>();
        if (display != null && display.artImage != null) display.artImage.color = Color.red;

        Vector3 basePos = cardTransform.localPosition;
        float shakeElapsed = 0f;
        while (shakeElapsed < 0.4f)
        {
            cardTransform.localPosition = basePos + new Vector3(Mathf.Sin(shakeElapsed * 50f) * 5f, 0, 0);
            shakeElapsed += Time.deltaTime;
            yield return null;
        }
        cardTransform.localPosition = basePos;

        float shrinkElapsed = 0f;
        Vector3 startScale = cardTransform.localScale;
        while (shrinkElapsed < 0.3f)
        {
            cardTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, shrinkElapsed / 0.3f);
            shrinkElapsed += Time.deltaTime;
            yield return null;
        }
        cardTransform.localScale = Vector3.zero;

        DeckManager dm = FindObjectOfType<DeckManager>();
        if (dm != null)
        {
            dm.AddToDiscard(card);
            dm.StartCoroutine(dm.AnimateCardToDiscard(cardTransform.gameObject));
        }
        else Destroy(cardTransform.gameObject);

        playerSlots[slotIndex] = null;
    }
}