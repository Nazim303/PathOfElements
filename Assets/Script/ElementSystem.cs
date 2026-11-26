using UnityEngine;

// Element türlerini tanımlıyoruz
public enum ElementType
{
    None,
    // --- ANA ELEMENTLER ---
    Fire,
    Water,
    Earth,  // (Doğa)
    Air,

    // --- YAN ELEMENTLER (FÜZYON) ---
    Magma,  // Ateş + Doğa
    Steam,  // Su + Ateş
    Ice,    // Su + Hava
    Dust    // Doğa + Hava
}

public static class ElementSystem
{
    // Hasar çarpanını hesaplayan fonksiyon
    public static float GetDamageMultiplier(ElementType attacker, ElementType defender)
    {
        if (attacker == ElementType.None || defender == ElementType.None) return 1.0f;
        if (attacker == defender) return 1.0f;

        // --- 1. ANA ELEMENTLERİN DÖNGÜSÜ (Ateş > Toprak > Hava > Su > Ateş) ---
        // Kritik Vuruşlar (2.0x)
        if (attacker == ElementType.Fire && defender == ElementType.Earth) return 2.0f;
        if (attacker == ElementType.Earth && defender == ElementType.Air) return 2.0f;
        if (attacker == ElementType.Air && defender == ElementType.Water) return 2.0f;
        if (attacker == ElementType.Water && defender == ElementType.Fire) return 2.0f;

        // Zayıf Vuruşlar (0.5x)
        if (attacker == ElementType.Fire && defender == ElementType.Water) return 0.5f;
        if (attacker == ElementType.Water && defender == ElementType.Air) return 0.5f;
        if (attacker == ElementType.Air && defender == ElementType.Earth) return 0.5f;
        if (attacker == ElementType.Earth && defender == ElementType.Fire) return 0.5f;

        // --- 2. FÜZYON SALDIRILARI (Füzyon Kartı -> Normal Düşman) ---
        // Magma (Ateş + Doğa): Toprak ve Havaya karşı güçlü, Suya karşı zayıf
        if (attacker == ElementType.Magma)
        {
            if (defender == ElementType.Earth || defender == ElementType.Air) return 2.0f;
            if (defender == ElementType.Water) return 0.5f;
        }

        // Steam (Su + Ateş): Ateş ve Toprağa karşı güçlü, Havaya karşı zayıf
        if (attacker == ElementType.Steam)
        {
            if (defender == ElementType.Fire || defender == ElementType.Earth) return 2.0f;
            if (defender == ElementType.Air) return 0.5f;
        }

        // Ice (Su + Hava): Ateş ve Suya karşı güçlü, Toprağa karşı zayıf
        if (attacker == ElementType.Ice)
        {
            if (defender == ElementType.Fire || defender == ElementType.Water) return 2.0f;
            if (defender == ElementType.Earth) return 0.5f;
        }

        // Dust (Doğa + Hava): Hava ve Suya karşı güçlü, Ateşe karşı zayıf
        if (attacker == ElementType.Dust)
        {
            if (defender == ElementType.Air || defender == ElementType.Water) return 2.0f;
            if (defender == ElementType.Fire) return 0.5f;
        }

        // --- 3. NORMAL SALDIRILAR (Normal Kart -> Füzyon Düşman) ---
        if (defender == ElementType.Magma)
        {
            if (attacker == ElementType.Water) return 1.5f; // Su, Magma'yı söndürür
            if (attacker == ElementType.Earth || attacker == ElementType.Air) return 0.5f;
        }
        if (defender == ElementType.Steam)
        {
            if (attacker == ElementType.Air) return 1.5f; // Hava, Buharı dağıtır
            if (attacker == ElementType.Fire || attacker == ElementType.Earth) return 0.5f;
        }
        if (defender == ElementType.Ice)
        {
            if (attacker == ElementType.Earth) return 1.5f; // Toprak/Kaya, Buzu kırar
            if (attacker == ElementType.Fire || attacker == ElementType.Water) return 0.5f;
        }
        if (defender == ElementType.Dust)
        {
            if (attacker == ElementType.Fire) return 1.5f; // Ateş, Tozu yakar
            if (attacker == ElementType.Air || attacker == ElementType.Water) return 0.5f;
        }

        return 1.0f; // Diğer durumlar nötr
    }
}
