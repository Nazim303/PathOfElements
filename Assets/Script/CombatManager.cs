using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // YENÝ: Image materyallerine eriþmek için

[System.Serializable]
public class UltimateCombo
{
    public string comboName;
    public List<CardData> requiredCards;
    public int ultimateDamage = 9999;
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
    public int activeEnemyCount;
    public BossBehavior currentBoss;
    public EnemyData defaultMinion;
    public GameObject enemyDeathEffectPrefab;

    [Header("Fusion System")]
    public List<FusionRecipe> allFusionRecipes;
    public GameObject cardPrefab;

    [Header("Visuals")]
    public Transform discardPoint;

    [Header("Ultimate Combos (Kadim Birleþimler)")]
    public List<UltimateCombo> ultimateCombos;

    // --- YENÝ EFEKT REFERANSLARI ---
    [Header("Particle & Shader Effects")]
    public GameObject dissolveBurningPrefab; // Kart yanma partikülü
    public GameObject spellChargePrefab;     // Kartýn üstünde toplanan enerji
    public GameObject spellTrailPrefab;      // Düþmana giden mermi/ateþ
    public GameObject spellImpactPrefab;     // Düþmanda patlama

    // LAG FIX
    public void SetupEnemies(List<EnemyData> enemiesToSpawn)
    {
        ClearEnemies();
        currentBoss = null;
        StartCoroutine(SpawnEnemiesRoutine(enemiesToSpawn));
    }

    IEnumerator SpawnEnemiesRoutine(List<EnemyData> enemiesToSpawn)
    {
        foreach (EnemyData data in enemiesToSpawn)
        {
            GameObject newEnemyObj = Instantiate(enemyPrefab, enemySpawnPoint);
            EnemyDisplay display = newEnemyObj.GetComponent<EnemyDisplay>();

            display.SetupEnemy(data, data.maxHealth);
            activeEnemies.Add(display);

            float enemyScale = GlobalScaleManager.instance != null ? GlobalScaleManager.instance.normalEnemyScale : 1f;
            newEnemyObj.transform.localScale = new Vector3(enemyScale, enemyScale, enemyScale);
            newEnemyObj.transform.localScale = Vector3.one;

            if (data.enemyName.Contains("Protector"))
            {
                currentBoss = newEnemyObj.AddComponent<Boss_Protector>();
                ((Boss_Protector)currentBoss).minionData = defaultMinion;
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
        yield return null;
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
            RecalculateTotalEnergyCost();
        }
    }

    public void ClearSlot(int index)
    {
        if (index >= 0 && index < playerSlots.Length)
        {
            playerSlots[index] = null;
            RecalculateTotalEnergyCost();
        }
    }

    // --- YENÝ: EFEKTLERÝ EN ÖNE ÇEKME ASÝSTANI ---
    void BringEffectToFront(GameObject effectObj)
    {
        if (effectObj == null) return;

        // Hiyerarþide en alta (öne) al
        effectObj.transform.SetAsLastSibling();

        // Ýçindeki tüm partikül sistemlerini bul ve çizim sýrasýný UI'ýn üstüne (200) çýkar
        ParticleSystemRenderer[] renderers = effectObj.GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (ParticleSystemRenderer r in renderers)
        {
            r.sortingOrder = 200;
        }
    }

    // Elementin türüne göre uygun rengi döndürür
    Color GetElementColor(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return Color.red;
            case ElementType.Water: return Color.blue;
            case ElementType.Earth: return new Color(0.6f, 0.4f, 0.2f); // Kahverengi
            case ElementType.Air: return Color.cyan;
            default: return Color.white;
        }
    }

    // Yaratýlan partikülün (ve içindeki tüm alt partiküllerin) rengini deðiþtirir
    void SetParticleColor(GameObject effectObj, Color color)
    {
        if (effectObj == null) return;

        ParticleSystem[] particles = effectObj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in particles)
        {
            var main = ps.main;
            main.startColor = color;
        }
    }

    // --- YENÝ: 3 AÞAMALI BÜYÜ EFEKTÝ SÝSTEMÝ ---
    // --- GÜNCELLENEN FONKSÝYON: 3 AÞAMALI BÜYÜ SÝSTEMÝ ---
    // --- GÜNCELLENEN FONKSÝYON: 3 AÞAMALI BÜYÜ SÝSTEMÝ ---
    // --- YENÝ: ElementType parametresi eklendi ---
    IEnumerator PlaySpellEffects(Transform startTransform, List<EnemyDisplay> targets, ElementType cardElement)
    {
        if (targets == null || targets.Count == 0) yield break;

        // Kartýn elementine göre rengi al
        Color spellColor = GetElementColor(cardElement);

        // 1. CHARGE (Enerji Toplama)
        GameObject charge = null;
        if (spellChargePrefab != null)
        {
            charge = Instantiate(spellChargePrefab, startTransform);
            charge.transform.localPosition = Vector3.zero;

            SetParticleColor(charge, spellColor); // Rengi Uygula
            BringEffectToFront(charge);

            yield return new WaitForSeconds(1.2f);
        }

        if (charge != null) Destroy(charge);

        // 2. TRAIL (Mermilerin Düþmana Gidiþi)
        List<GameObject> trails = new List<GameObject>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> endPositions = new List<Vector3>();

        if (spellTrailPrefab != null)
        {
            foreach (var target in targets)
            {
                if (target != null)
                {
                    GameObject trail = Instantiate(spellTrailPrefab, startTransform.position, Quaternion.identity);

                    SetParticleColor(trail, spellColor); // Rengi Uygula
                    BringEffectToFront(trail);

                    trails.Add(trail);
                    startPositions.Add(trail.transform.position);

                    Transform targetCenter = target.visualRoot != null ? target.visualRoot : target.transform;
                    endPositions.Add(targetCenter.position);
                }
            }

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * 6f;
                for (int i = 0; i < trails.Count; i++)
                {
                    if (trails[i] != null)
                        trails[i].transform.position = Vector3.Lerp(startPositions[i], endPositions[i], t);
                }
                yield return null;
            }

            foreach (var trail in trails) if (trail != null) Destroy(trail);
        }

        // 3. IMPACT (Patlama)
        if (spellImpactPrefab != null)
        {
            foreach (var target in targets)
            {
                if (target != null)
                {
                    Transform targetCenter = target.visualRoot != null ? target.visualRoot : target.transform;
                    GameObject impact = Instantiate(spellImpactPrefab, targetCenter.position, Quaternion.identity);

                    SetParticleColor(impact, spellColor); // Rengi Uygula
                    BringEffectToFront(impact);

                    Destroy(impact, 2f);
                }
            }
        }
    }

    public IEnumerator OnAttackPhase()
    {
        UltimateCombo activeUltimate = CheckUltimateCombo();
        bool isFullCombo = CheckElementalCombo();
        bool isSplitCombo = CheckTwoPairCombo();

        EnemyDisplay frontEnemy = null;
        if (activeEnemies.Count > 0) frontEnemy = activeEnemies[0];

        Debug.Log("--- TUR BAÞLIYOR ---");

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

            if (EnergyManager.instance.currentEnergy >= totalUltimateEnergyCost)
            {
                canExecuteUltimate = true;
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
            else Debug.Log("Ultimate için yeterli enerji yok! Kadim bað koptu, normal saldýrýya dönülüyor...");
        }

        // ========================================================
        // ?? DURUM A: ULTIMATE BAÞARILI
        // ========================================================
        if (canExecuteUltimate)
        {
            Debug.Log($"?? {activeUltimate.comboName} UYANDI! ??");

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

            List<EnemyDisplay> targets = new List<EnemyDisplay>(activeEnemies);
            List<Coroutine> spellRoutines = new List<Coroutine>();
            for (int k = 0; k < ultimateCards.Count; k++)
            {
                // Artýk ultimateData listesinden kartýn elementini de yolluyoruz
                spellRoutines.Add(StartCoroutine(PlaySpellEffects(ultimateCards[k], targets, ultimateData[k].element)));
            }
            if (spellRoutines.Count > 0) yield return spellRoutines[0];

            foreach (EnemyDisplay enemy in targets)
            {
                if (enemy != null)
                {
                    ApplyDamageToEnemy(ultimateData[0], enemy, activeUltimate);
                    StartCoroutine(enemy.TakeDamageEffect());
                }
            }
            if (UIShake.instance != null) UIShake.instance.TriggerShake(0.5f, 20f);

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
            // ?? DURUM B: NORMAL SALDIRI
            // ========================================================
            // ========================================================
            // ?? DURUM B: NORMAL SALDIRI (Ve Element Güçlendiricileri)
            // ========================================================
            if (isFullCombo) Debug.Log("?? TAM ELEMENTAL KOMBO (AOE) AKTÝF! ??");
            else if (isSplitCombo) Debug.Log("?? AYRIK ÝKÝLÝ KOMBO (SPLIT) AKTÝF! ??");

            // Kartlarýn saðdaki güçlendirici olarak kullanýlýp kullanýlmadýðýný takip etmek için dizi:
            bool[] processed = new bool[4];

            for (int i = 0; i < playerSlots.Length; i++)
            {
                // Kart saðdaki güçlendirici (modifier) olarak kullanýldýysa turunu atla!
                if (processed[i]) continue;

                if (playerSlots[i] != null && slotTransforms[i].childCount > 0)
                {
                    Transform cardTransform = slotTransforms[i].GetChild(0);
                    CardData card = playerSlots[i];

                    if (!EnergyManager.instance.TrySpendEnergy(card.energyCost))
                    {
                        yield return StartCoroutine(FizzleCardRoutine(i, cardTransform, card));
                        yield return new WaitForSeconds(0.4f);
                        continue;
                    }

                    Transform supportTransform = null;
                    CardData supportCard = null;
                    float currentSupportMultiplier = 1.0f;

                    // --- SAÐDAKÝ GÜÇLENDÝRÝCÝYÝ KONTROL ET ---
                    // Eðer kartýn saðýnda (i+1) bir kart varsa ve bu kart bir "Modifier" ise:
                    if (i < 3 && playerSlots[i + 1] != null && playerSlots[i + 1].isElementalModifier)
                    {
                        supportCard = playerSlots[i + 1];
                        if (slotTransforms[i + 1].childCount > 0)
                        {
                            supportTransform = slotTransforms[i + 1].GetChild(0);
                        }

                        // Destek kartýnýn da enerjisini (0) harca ve onu iþlenmiþ say
                        if (EnergyManager.instance.TrySpendEnergy(supportCard.energyCost))
                        {
                            processed[i + 1] = true;

                            if (card.element == supportCard.element)
                            {
                                currentSupportMultiplier = 1.5f; // Ayný Element: %50 Fazla Vurur
                                Debug.Log($"?? MÜKEMMEL UYUM! {supportCard.cardName}, hasarý artýrdý!");
                            }
                            else if (IsAntiElement(card.element, supportCard.element))
                            {
                                currentSupportMultiplier = 0.4f; // Zýt Element: Hasar çakýlýr
                                Debug.Log($"?? ZIT ELEMENT! {supportCard.cardName} yüzünden hasar çok düþtü!");
                            }
                            else
                            {
                                currentSupportMultiplier = 0.8f; // Farklý Element: %20 Azaltýr
                                Debug.Log($"?? UYUMSUZ ELEMENT. {supportCard.cardName} yüzünden hasar düþtü.");
                            }
                        }
                    }

                    // --- ANÝMASYON (Ýki Kart Varsa Ayný Anda Kalkar) ---
                    Vector3 originalPos = cardTransform.localPosition;
                    Vector3 punchPos = originalPos + new Vector3(0, 50f, 0);

                    Vector3 supOriginalPos = Vector3.zero;
                    Vector3 supPunchPos = Vector3.zero;

                    if (supportTransform != null)
                    {
                        supOriginalPos = supportTransform.localPosition;
                        supPunchPos = supOriginalPos + new Vector3(0, 50f, 0);
                    }

                    float animT = 0;
                    while (animT < 1)
                    {
                        animT += Time.deltaTime * 10;
                        cardTransform.localPosition = Vector3.Lerp(originalPos, punchPos, animT);
                        if (supportTransform != null) supportTransform.localPosition = Vector3.Lerp(supOriginalPos, supPunchPos, animT);
                        yield return null;
                    }

                    // --- HEDEFLERÝ BELÝRLE ---
                    List<EnemyDisplay> currentTargets = new List<EnemyDisplay>();

                    if (card.cardType == CardType.Companion)
                    {
                        if (card.specialEffect == SpecialEffect.HalveEnemyHealth && frontEnemy != null) currentTargets.Add(frontEnemy);
                        else if (card.specialEffect == SpecialEffect.AOE_Damage) currentTargets.AddRange(activeEnemies);
                    }
                    else if (card.cardType == CardType.Attack)
                    {
                        if (activeEnemies.Count > 0)
                        {
                            if (isFullCombo) currentTargets.AddRange(activeEnemies);
                            else if (isSplitCombo)
                            {
                                EnemyDisplay target = (i < 2) ? activeEnemies[0] : activeEnemies[activeEnemies.Count - 1];
                                if (target != null) currentTargets.Add(target);
                            }
                            else currentTargets.Add(activeEnemies[0]);
                        }
                    }

                    // --- KURAL: GÜÇLENDÝRÝCÝ TEK BAÞINA OYNANDIYSA ---
                    if (card.isElementalModifier && supportCard == null)
                    {
                        // Sadece kalkýp iner, bedelsiz elden çýkarma durumu
                        Debug.Log($"{card.cardName} tek baþýna oynandý. Etki etmeden kayboluyor.");
                    }
                    else
                    {
                        // Büyü Efektini Oynat
                        if (currentTargets.Count > 0)
                        {
                            yield return StartCoroutine(PlaySpellEffects(cardTransform, currentTargets, card.element));
                        }

                        // Hasarý Uygula (Multiplier parametresi ile!)
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
                                    foreach (EnemyDisplay enemy in currentTargets)
                                    {
                                        if (enemy != null) { ApplyDamageToEnemy(card, enemy, null, currentSupportMultiplier); StartCoroutine(enemy.TakeDamageEffect()); }
                                    }
                                    break;
                            }
                            if (FindObjectOfType<DeckManager>() != null) FindObjectOfType<DeckManager>().RemoveCardFromDeck(card);
                        }
                        else if (card.cardType == CardType.Heal)
                        {
                            if (PlayerManager.instance != null) PlayerManager.instance.Heal(card.healAmount);
                        }
                        else // Normal Saldýrý Hasarý
                        {
                            foreach (EnemyDisplay enemy in currentTargets)
                            {
                                if (enemy != null) { ApplyDamageToEnemy(card, enemy, null, currentSupportMultiplier); StartCoroutine(enemy.TakeDamageEffect()); }
                            }
                        }
                    }

                    // --- ANÝMASYON GERÝ DÖNÜÞ (Kartlar masaya oturur) ---
                    animT = 0;
                    while (animT < 1)
                    {
                        animT += Time.deltaTime * 10;
                        cardTransform.localPosition = Vector3.Lerp(punchPos, originalPos, animT);
                        if (supportTransform != null) supportTransform.localPosition = Vector3.Lerp(supPunchPos, supOriginalPos, animT);
                        yield return null;
                    }

                    yield return new WaitForSeconds(0.2f);
                    HandleDeaths();

                    // YENÝ: Kart yutulduktan veya oynandýktan sonra biraz beklesin ki diðerine geçmesin
                    yield return new WaitForSeconds(0.6f);
                }
            }
        }
    }

    bool CheckTwoPairCombo()
    {
        for (int i = 0; i < playerSlots.Length; i++) { if (playerSlots[i] == null) return false; }
        bool firstPairMatch = playerSlots[0].element == playerSlots[1].element;
        bool secondPairMatch = playerSlots[2].element == playerSlots[3].element;
        bool pairsAreDifferent = playerSlots[0].element != playerSlots[2].element;
        return firstPairMatch && secondPairMatch && pairsAreDifferent;
    }

    bool CheckElementalCombo()
    {
        for (int i = 0; i < playerSlots.Length; i++) if (playerSlots[i] == null) return false;
        ElementType firstElement = playerSlots[0].element;
        for (int i = 1; i < playerSlots.Length; i++) if (playerSlots[i].element != firstElement) return false;
        return true;
    }

    public void SpawnExtraEnemy(EnemyData data)
    {
        GameObject newEnemyObj = Instantiate(enemyPrefab, enemySpawnPoint);
        EnemyDisplay display = newEnemyObj.GetComponent<EnemyDisplay>();
        display.SetupEnemy(data, data.maxHealth);
        activeEnemies.Insert(0, display);
        newEnemyObj.transform.SetAsFirstSibling();
        newEnemyObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        activeEnemyCount = activeEnemies.Count;
    }

    void ApplyDamageToEnemy(CardData card, EnemyDisplay target, UltimateCombo activeUltimate = null, float supportMultiplier = 1.0f)
    {
        if (target == null) return;
        BossBehavior boss = target.GetComponent<BossBehavior>();
        if (boss != null && boss.IsInvulnerable() && activeUltimate == null) return;

        int finalDamage = 0;
        if (activeUltimate != null) finalDamage = activeUltimate.ultimateDamage;
        else
        {
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
            // YENÝ: Support çarpaný ile asýl hasarý çarpýyoruz
            finalDamage = Mathf.RoundToInt(card.baseDamage * multiplier * supportMultiplier);
        }

        target.currentHealth -= finalDamage;
        target.UpdateHealthUI(target.currentHealth);
    }

    void HandleDeaths()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i].currentHealth <= 0) EnemyDie(activeEnemies[i]);
        }
    }

    void EnemyDie(EnemyDisplay enemy)
    {
        if (PlayerManager.instance != null) PlayerManager.instance.AddGold(enemy.enemyData.goldReward);
        activeEnemies.Remove(enemy);
        if (enemyDeathEffectPrefab != null)
        {
            GameObject effect = Instantiate(enemyDeathEffectPrefab, enemy.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (enemyDeathEffectPrefab != null)
        {
            GameObject effect = Instantiate(enemyDeathEffectPrefab, enemy.transform.position, Quaternion.identity);
            BringEffectToFront(effect); // YENÝ EKLENDÝ
            Destroy(effect, 2f);
        }
        StartCoroutine(enemy.DieAnimationAndDestroy());
        activeEnemyCount = activeEnemies.Count;
    }

    void RecalculateTotalEnergyCost()
    {
        int totalCost = 0;
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null) totalCost += playerSlots[i].energyCost;
        }
        if (EnergyManager.instance != null) EnergyManager.instance.UpdatePendingCost(totalCost);
    }

    public FusionRecipe CheckFusion(CardData a, CardData b)
    {
        foreach (FusionRecipe recipe in allFusionRecipes)
        {
            if ((recipe.cardA == a && recipe.cardB == b) || (recipe.cardA == b && recipe.cardB == a)) return recipe;
        }
        return null;
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
                        if (playerSlots[i].cardType != CardType.Companion) deckManager.AddToDiscard(playerSlots[i]);
                    }
                    if (deckManager != null) deckManager.StartCoroutine(deckManager.AnimateCardToDiscard(cardObj));
                    else Destroy(cardObj);
                }
            }
            playerSlots[i] = null;
        }
        yield return new WaitForSeconds(0.4f);
        RecalculateTotalEnergyCost();
    }

    UltimateCombo CheckUltimateCombo()
    {
        if (ultimateCombos == null || ultimateCombos.Count == 0) return null;
        foreach (UltimateCombo combo in ultimateCombos)
        {
            if (combo.requiredCards == null || combo.requiredCards.Count == 0) continue;
            List<CardData> tempPieces = new List<CardData>(combo.requiredCards);
            for (int i = 0; i < playerSlots.Length; i++)
            {
                if (playerSlots[i] == null) continue;
                if (tempPieces.Contains(playerSlots[i])) tempPieces.Remove(playerSlots[i]);
            }
            if (tempPieces.Count == 0) return combo;
        }
        return null;
    }

    // --- YENÝ: DISSOLVE EFEKTÝ VE KART YANMASI ---
    IEnumerator FizzleCardRoutine(int slotIndex, Transform cardTransform, CardData card)
    {
        Debug.Log($"Yetersiz Enerji! {card.cardName} yanarak kül oluyor...");

        CardDisplay display = cardTransform.GetComponent<CardDisplay>();
        Material dissolveMat = null;

        // 1. Arkadaþýnýn Notu: Shader Instance (Kopya) Alýmý
        if (display != null && display.artImage != null)
        {
            // Tüm kartlar ayný anda erimesin diye materyalin KOPYASINI oluþturuyoruz
            dissolveMat = new Material(display.artImage.material);
            display.artImage.material = dissolveMat;

            // Eðer çerçevenin de erimesini istersen:
            if (display.banderimage != null) display.banderimage.material = dissolveMat;
        }

        // 2. Burning (Yanma) Partikülünü Yarat
        GameObject burningEffect = null;
        if (dissolveBurningPrefab != null)
        {
            burningEffect = Instantiate(dissolveBurningPrefab, cardTransform.position, Quaternion.identity, cardTransform);
        }

        if (dissolveBurningPrefab != null)
        {
            burningEffect = Instantiate(dissolveBurningPrefab, cardTransform.position, Quaternion.identity, cardTransform);
            BringEffectToFront(burningEffect); // YENÝ EKLENDÝ
        }

        // 3. Titreme ve Erime Döngüsü
        Vector3 basePos = cardTransform.localPosition;
        float duration = 1.0f; // Erimeyi görebilmek için 1 saniye verdik
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Titre
            cardTransform.localPosition = basePos + new Vector3(Mathf.Sin(elapsed * 50f) * 3f, 0, 0);

            // Eri (Shader Graph'taki _DissolveAmount parametresini 0'dan 1'e çekiyoruz)
            if (dissolveMat != null)
            {
                // Not: Shader'da özelliðin adý "_DissolveAmount" ise bu çalýþýr. Farklýysa ismini ona göre güncelle.
                dissolveMat.SetFloat("_DissolveAmount", elapsed / duration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (burningEffect != null) Destroy(burningEffect);

        // 4. Ýskartaya yolla ve objeyi sil (Artýk küçülüp uçmasýna gerek yok, eridi bitti!)
        DeckManager dm = FindObjectOfType<DeckManager>();
        if (dm != null) dm.AddToDiscard(card);
        Destroy(cardTransform.gameObject);

        playerSlots[slotIndex] = null;
    }

    // Hangi elementlerin birbirine zýt (Anti) olduðunu belirler
    bool IsAntiElement(ElementType attackElem, ElementType supportElem)
    {
        if (attackElem == ElementType.Fire && supportElem == ElementType.Water) return true;
        if (attackElem == ElementType.Water && supportElem == ElementType.Fire) return true;
        if (attackElem == ElementType.Earth && supportElem == ElementType.Air) return true;
        if (attackElem == ElementType.Air && supportElem == ElementType.Earth) return true;
        return false;
    }
}