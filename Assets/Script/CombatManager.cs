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

    // --- YENÝ EFEKT REFERANSLARI (GÜNCELLENDÝ) ---
    [Header("Particle & Shader Effects")]
    public GameObject dissolveBurningPrefab;
    public GameObject enemySmokeDeathPrefab;
    public GameObject spellChargePrefab;

    [Header("Level 1 Attack Effects")]
    public GameObject vfx_FireAttack01;
    public GameObject vfx_WaterAttack01;
    public GameObject vfx_EarthAttack01;
    public GameObject vfx_AirAttack01;

    [Header("Level 2 Attack Effects")]
    public GameObject vfx_FireAttack02;
    public GameObject vfx_WaterAttack02;
    public GameObject vfx_EarthAttack02;
    public GameObject vfx_AirAttack02;

    [Header("Level 3 Attack Effects")]
    public GameObject vfx_FireAttack03;
    public GameObject vfx_WaterAttack03;
    public GameObject vfx_EarthAttack03;
    public GameObject vfx_AirAttack03;

    [Header("Level 1 Impact Effects")]
    public GameObject vfx_FireImpact01;
    public GameObject vfx_WaterImpact01;
    public GameObject vfx_EarthImpact01;
    public GameObject vfx_AirImpact01;

    [Header("Level 2 Impact Effects")]
    public GameObject vfx_FireImpact02;
    public GameObject vfx_WaterImpact02;
    public GameObject vfx_EarthImpact02;
    public GameObject vfx_AirImpact02;

    [Header("Level 3 Impact Effects")]
    public GameObject vfx_FireImpact03;
    public GameObject vfx_WaterImpact03;
    public GameObject vfx_EarthImpact03;
    public GameObject vfx_AirImpact03;



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

    // Element ve Güç seviyesine göre doðru prefabý seçer
    GameObject GetAttackVFXPrefab(ElementType element, int power)
    {
        switch (power)
        {
            case 1:
                if (element == ElementType.Fire) return vfx_FireAttack01;
                if (element == ElementType.Water) return vfx_WaterAttack01;
                if (element == ElementType.Earth) return vfx_EarthAttack01;
                if (element == ElementType.Air) return vfx_AirAttack01;
                break;
            case 2:
                if (element == ElementType.Fire) return vfx_FireAttack02;
                if (element == ElementType.Water) return vfx_WaterAttack02;
                if (element == ElementType.Earth) return vfx_EarthAttack02;
                if (element == ElementType.Air) return vfx_AirAttack02;
                break;
            case 3:
            default: // 3 veya beklenmedik durumlarda en güçlüsü
                if (element == ElementType.Fire) return vfx_FireAttack03;
                if (element == ElementType.Water) return vfx_WaterAttack03;
                if (element == ElementType.Earth) return vfx_EarthAttack03;
                if (element == ElementType.Air) return vfx_AirAttack03;
                break;
        }
        return null;
    }

    // Element ve Güç seviyesine göre doðru Impact (Çarpma) prefabýný seçer
    GameObject GetImpactVFXPrefab(ElementType element, int power)
    {
        switch (power)
        {
            case 1:
                if (element == ElementType.Fire) return vfx_FireImpact01;
                if (element == ElementType.Water) return vfx_WaterImpact01;
                if (element == ElementType.Earth) return vfx_EarthImpact01;
                if (element == ElementType.Air) return vfx_AirImpact01;
                break;
            case 2:
                if (element == ElementType.Fire) return vfx_FireImpact02;
                if (element == ElementType.Water) return vfx_WaterImpact02;
                if (element == ElementType.Earth) return vfx_EarthImpact02;
                if (element == ElementType.Air) return vfx_AirImpact02;
                break;
            case 3:
            default:
                if (element == ElementType.Fire) return vfx_FireImpact03;
                if (element == ElementType.Water) return vfx_WaterImpact03;
                if (element == ElementType.Earth) return vfx_EarthImpact03;
                if (element == ElementType.Air) return vfx_AirImpact03;
                break;
        }
        return null;
    }

    // Elementin türüne göre uygun rengi döndürür
    Color GetElementColor(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return Color.red;
            case ElementType.Water: return Color.blue;
            case ElementType.Earth: return new Color(0.6f, 0.4f, 0.2f); // Kahverengi
            case ElementType.Air: return new Color(0.85f, 0.38f, 0.55f, 0.5f);
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


   // --- GÜNCELLENEN FONKSÝYON: CANVAS ÝÇÝNDE UÇAN MERMÝLER ---
    IEnumerator PlaySpellEffects(Transform startTransform, List<EnemyDisplay> targets, CardData cardData)
    {
        if (targets == null || targets.Count == 0 || startTransform == null || cardData == null) yield break;

        // --- KRÝTÝK ÇÖZÜM: Game Manager'ý deðil, Canvas'ý bul ve parent yap! ---
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        Transform vfxParent = mainCanvas != null ? mainCanvas.transform : transform;

        Color elementColor = GetElementColor(cardData.element);

        // 1. KARTIN PARLAMASI 
        CardDisplay display = startTransform.GetComponent<CardDisplay>();
        if (display != null && display.artImage != null)
        {
            Material mat = display.artImage.material;
            if (mat.HasProperty("_FlashColor") && mat.HasProperty("_FlashAmount"))
            {
                mat.SetColor("_FlashColor", elementColor);
                float flashDuration = 0.15f;
                float t = 0;

                while (t < 1) { t += Time.deltaTime / flashDuration; mat.SetFloat("_FlashAmount", Mathf.Lerp(0f, 1f, t)); yield return null; }
                t = 0;
                while (t < 1) { t += Time.deltaTime / flashDuration; mat.SetFloat("_FlashAmount", Mathf.Lerp(1f, 0f, t)); yield return null; }
                mat.SetFloat("_FlashAmount", 0f);
            }
        }

        // 1.5 CHARGE (Enerji Toplama) 
        GameObject charge = null;
        if (spellChargePrefab != null)
        {
            charge = Instantiate(spellChargePrefab, vfxParent); // Artýk Canvas'ta oluþuyor
            charge.transform.position = startTransform.position;
            charge.transform.localScale = Vector3.one;

            Vector3 chargePos = charge.transform.localPosition;
            chargePos.z = -50f; // Öne itele
            charge.transform.localPosition = chargePos;

            SetParticleColor(charge, elementColor);
            BringEffectToFront(charge);

            yield return new WaitForSeconds(0.5f);
        }
        if (charge != null) Destroy(charge);

        // 2. MERMÝ MANTIÐI
        GameObject vfxPrefabToUse = GetAttackVFXPrefab(cardData.element, cardData.cardPower);
        List<GameObject> spawnedAttacks = new List<GameObject>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> endPositions = new List<Vector3>();

        if (vfxPrefabToUse != null)
        {
            foreach (var target in targets)
            {
                if (target != null && target.gameObject != null)
                {
                    Transform targetCenter = target.visualRoot != null ? target.visualRoot : target.transform;

                    GameObject attackVFX = Instantiate(vfxPrefabToUse, vfxParent); // Artýk Canvas'ta oluþuyor
                    attackVFX.transform.position = startTransform.position;
                    attackVFX.transform.localScale = Vector3.one;

                    Vector3 atkPos = attackVFX.transform.localPosition;
                    atkPos.z = -50f; // Öne itele
                    attackVFX.transform.localPosition = atkPos;

                    BringEffectToFront(attackVFX);

                    spawnedAttacks.Add(attackVFX);
                    startPositions.Add(attackVFX.transform.position);
                    endPositions.Add(targetCenter.position);
                }
            }

            float moveT = 0;
            while (moveT < 1)
            {
                moveT += Time.deltaTime * 6f;
                for (int i = 0; i < spawnedAttacks.Count; i++)
                {
                    if (spawnedAttacks[i] != null)
                    {
                        spawnedAttacks[i].transform.position = Vector3.Lerp(startPositions[i], endPositions[i], moveT);
                    }
                }
                yield return null;
            }

            foreach (var attack in spawnedAttacks)
            {
                if (attack != null) Destroy(attack, 0.5f);
            }
        }

        // 3. IMPACT (Patlama) 
        GameObject impactPrefabToUse = GetImpactVFXPrefab(cardData.element, cardData.cardPower);
        if (impactPrefabToUse != null)
        {
            foreach (var target in targets)
            {
                if (target != null && target.gameObject != null)
                {
                    Transform targetCenter = target.visualRoot != null ? target.visualRoot : target.transform;

                    GameObject impact = Instantiate(impactPrefabToUse, vfxParent); // Artýk Canvas'ta oluþuyor
                    impact.transform.position = targetCenter.position;
                    impact.transform.localScale = Vector3.one;

                    Vector3 impPos = impact.transform.localPosition;
                    impPos.z = -50f; // Öne itele
                    impact.transform.localPosition = impPos;

                    BringEffectToFront(impact);
                    Destroy(impact, 1f);
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
                        // YENÝ: Sadece CardDisplay olan objeyi (Gerçek Kartý) bul
                        CardDisplay cardDisplay = slotTransforms[i].GetComponentInChildren<CardDisplay>();
                        if (cardDisplay != null)
                        {
                            EnergyManager.instance.TrySpendEnergy(playerSlots[i].energyCost);
                            ultimateCards.Add(cardDisplay.transform); // Efekti deðil, kartý ekle
                            ultimateData.Add(playerSlots[i]);
                        }
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
                // ESKÝ: spellRoutines.Add(StartCoroutine(PlaySpellEffects(ultimateCards[k], targets, ultimateData[k].element)));
                // YENÝ:
                spellRoutines.Add(StartCoroutine(PlaySpellEffects(ultimateCards[k], targets, ultimateData[k])));
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
            // ?? DURUM B: NORMAL SALDIRI (Ve Element Güçlendiricileri)
            // ========================================================
            if (isFullCombo) Debug.Log("?? TAM ELEMENTAL KOMBO (AOE) AKTÝF! ??");
            else if (isSplitCombo) Debug.Log("?? AYRIK ÝKÝLÝ KOMBO (SPLIT) AKTÝF! ??");

            bool[] processed = new bool[4];

            for (int i = 0; i < playerSlots.Length; i++)
            {
                if (processed[i]) continue;

                if (playerSlots[i] != null && slotTransforms[i].childCount > 0)
                {
                    // YENÝ: GetChild(0) yerine GERÇEK kartý bul!
                    CardDisplay display = slotTransforms[i].GetComponentInChildren<CardDisplay>();
                    if (display == null) continue; // Kart yoksa (sadece efekt kalmýþsa) atla

                    Transform cardTransform = display.transform;
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
                    if (i < 3 && playerSlots[i + 1] != null && playerSlots[i + 1].isElementalModifier)
                    {
                        supportCard = playerSlots[i + 1];
                        if (slotTransforms[i + 1].childCount > 0)
                        {
                            // YENÝ: Saðdaki slotun da GERÇEK kartýný bul!
                            CardDisplay supDisplay = slotTransforms[i + 1].GetComponentInChildren<CardDisplay>();
                            if (supDisplay != null) supportTransform = supDisplay.transform;
                        }

                        if (EnergyManager.instance.TrySpendEnergy(supportCard.energyCost))
                        {
                            processed[i + 1] = true;

                            if (card.element == supportCard.element)
                            {
                                currentSupportMultiplier = 1.5f;
                                Debug.Log($"?? MÜKEMMEL UYUM! {supportCard.cardName}, hasarý artýrdý!");
                            }
                            else if (IsAntiElement(card.element, supportCard.element))
                            {
                                currentSupportMultiplier = 0.4f;
                                Debug.Log($"?? ZIT ELEMENT! {supportCard.cardName} yüzünden hasar çok düþtü!");
                            }
                            else
                            {
                                currentSupportMultiplier = 0.8f;
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

                    if (card.isElementalModifier && supportCard == null)
                    {
                        Debug.Log($"{card.cardName} tek baþýna oynandý. Etki etmeden kayboluyor.");
                    }
                    else
                    {
                        if (currentTargets.Count > 0)
                        {
                            yield return StartCoroutine(PlaySpellEffects(cardTransform, currentTargets, card));
                        }

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
                                    if (PlayerManager.instance != null) PlayerManager.instance.Heal(PlayerManager.instance.maxHealth - PlayerManager.instance.currentHealth);
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
                        else
                        {
                            foreach (EnemyDisplay enemy in currentTargets)
                            {
                                if (enemy != null) { ApplyDamageToEnemy(card, enemy, null, currentSupportMultiplier); StartCoroutine(enemy.TakeDamageEffect()); }
                            }
                        }
                    }

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

        // --- KRÝTÝK ÇÖZÜM: Game Manager'ý deðil, Canvas'ý bul ve parent yap! ---
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        Transform vfxParent = mainCanvas != null ? mainCanvas.transform : transform;

        // 1. Orijinal Yanma/Erime Efekti
        if (enemyDeathEffectPrefab != null)
        {
            GameObject effect = Instantiate(enemyDeathEffectPrefab, vfxParent);
            effect.transform.position = enemy.transform.position;
            effect.transform.localScale = Vector3.one;

            // Öne itele
            Vector3 pos = effect.transform.localPosition;
            pos.z = -50f;
            effect.transform.localPosition = pos;

            BringEffectToFront(effect);
            Destroy(effect, 2f);
        }

        // 2. Ekstra Duman Efekti
        if (enemySmokeDeathPrefab != null)
        {
            GameObject smokeEffect = Instantiate(enemySmokeDeathPrefab, vfxParent);
            smokeEffect.transform.position = enemy.transform.position;
            smokeEffect.transform.localScale = Vector3.one;

            // Öne itele
            Vector3 pos = smokeEffect.transform.localPosition;
            pos.z = -50f;
            smokeEffect.transform.localPosition = pos;

            BringEffectToFront(smokeEffect);
            Destroy(smokeEffect, 2.5f);
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
                // Slotun içindeki tüm objeleri (Kart + Efektler) bir listeye alalým
                List<Transform> children = new List<Transform>();
                foreach (Transform child in slotTransforms[i]) children.Add(child);

                foreach (Transform child in children)
                {
                    GameObject childObj = child.gameObject;

                    // YENÝ: Bu obje bir kart mý, yoksa efekt mi?
                    CardDisplay isCard = childObj.GetComponent<CardDisplay>();

                    if (isCard != null)
                    {
                        // EÐER KART ÝSE: Ýskartaya yolla
                        if (playerSlots[i] != null && deckManager != null)
                        {
                            if (playerSlots[i].cardType != CardType.Companion)
                                deckManager.AddToDiscard(playerSlots[i]);
                        }

                        if (deckManager != null)
                            deckManager.StartCoroutine(deckManager.AnimateCardToDiscard(childObj));
                        else
                            Destroy(childObj);
                    }
                    else
                    {
                        // EÐER SADECE EFEKT ÝSE (VFX_Element_Effect): Olduðu yerde direkt yok et
                        Destroy(childObj);
                    }
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
    // --- YENÝ: DISSOLVE EFEKTÝ VE KART YANMASI ---
    IEnumerator FizzleCardRoutine(int slotIndex, Transform cardTransform, CardData card)
    {
        Debug.Log($"Yetersiz Enerji! {card.cardName} yanarak kül oluyor...");

        CardDisplay display = cardTransform.GetComponent<CardDisplay>();
        Material dissolveMat = null;

        if (display != null && display.artImage != null)
        {
            dissolveMat = new Material(display.artImage.material);
            display.artImage.material = dissolveMat;

            if (display.banderimage != null) display.banderimage.material = dissolveMat;
        }

        // 2. Burning (Yanma) Partikülünü Yarat
        GameObject burningEffect = null;
        if (dissolveBurningPrefab != null)
        {
            // Sadece bir kere, ölen kartýn içine (çocuk olarak) yaratýyoruz!
            burningEffect = Instantiate(dissolveBurningPrefab, cardTransform);

            // Merkeze al ve arkada kalmasýn diye Z eksenini -50'ye itele
            burningEffect.transform.localPosition = new Vector3(0, 0, -50f);
            burningEffect.transform.localScale = Vector3.one;

            BringEffectToFront(burningEffect);
        }

        // 3. Titreme ve Erime Döngüsü
        Vector3 basePos = cardTransform.localPosition;
        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            cardTransform.localPosition = basePos + new Vector3(Mathf.Sin(elapsed * 50f) * 3f, 0, 0);

            if (dissolveMat != null)
            {
                dissolveMat.SetFloat("_DissolveAmount", elapsed / duration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (burningEffect != null) Destroy(burningEffect);

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