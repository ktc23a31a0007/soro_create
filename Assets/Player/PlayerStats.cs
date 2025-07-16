using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [Header("基本ステータス")]
    public int baseAttackDamage = 10;
    public float baseDefense = 0f; // ダメージ軽減率 (0-1, 0.1 = 10%軽減)
    public int currentLevel = 1;
    public int currentExperience = 0;
    public int experienceToNextLevel = 100;

    [Header("属性攻撃力ボーナス")]
    [Tooltip("各属性に対応する攻撃力ボーナス (例: Fire: +5)")]
    public Dictionary<ElementType, int> elementalAttackBonus = new Dictionary<ElementType, int>()
    {
        { ElementType.Fire, 0 },
        { ElementType.Water, 0 },
        { ElementType.Wind, 0 },
        { ElementType.Earth, 0 },
        { ElementType.Lightning, 0 },
        { ElementType.Ice, 0 },
        { ElementType.Darkness, 0 },
        { ElementType.Light, 0 },
        { ElementType.Magic, 0 }
    };

    [Header("装備中の武器属性")]
    public ElementType equippedWeaponElement = ElementType.None; // 現在装備している武器の属性

    void Awake()
    {
        InitializeElementalAttackBonus();
    }

    // Dictionaryをインスペクターで表示できないため、初期化処理
    void InitializeElementalAttackBonus()
    {
        // 既にAwakeで初期化されているので、ここでは何もしない
        // もしインスペクターから設定できるようにしたい場合は、
        // ScriptableObjectやカスタムエディタが必要になります。
    }

    // 経験値の追加とレベルアップ
    public void AddExperience(int amount)
    {
        currentExperience += amount;
        Debug.Log($"経験値を {amount} 獲得しました。現在経験値: {currentExperience}");

        while (currentExperience >= experienceToNextLevel)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentExperience -= experienceToNextLevel;
        currentLevel++;
        baseAttackDamage += 2; // 例: レベルアップで攻撃力増加
        // baseDefense += 0.01f; // 例: 防御力も微増
        experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * 1.5f); // 次のレベルまでの経験値が増加

        Debug.Log($"レベルアップ！レベル: {currentLevel}, 攻撃力: {baseAttackDamage}");
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"レベルアップ！レベル {currentLevel}", 3f);
    }

    // 属性攻撃力ボーナスを設定
    public void SetElementalAttackBonus(ElementType element, int bonus)
    {
        if (elementalAttackBonus.ContainsKey(element))
        {
            elementalAttackBonus[element] = bonus;
        }
        else
        {
            elementalAttackBonus.Add(element, bonus);
        }
    }

    // 特定の属性の攻撃力ボーナスを取得
    public int GetElementalAttackBonus(ElementType element)
    {
        if (elementalAttackBonus.ContainsKey(element))
        {
            return elementalAttackBonus[element];
        }
        return 0;
    }

    // 装備中の武器の属性を変更
    public void SetEquippedWeaponElement(ElementType element)
    {
        equippedWeaponElement = element;
        Debug.Log($"装備中の武器属性が {equippedWeaponElement} に変更されました。");
    }

    // セーブ/ロード用メソッド
    public void SetStats(int level, int exp, int atk, float def, ElementType weaponElement)
    {
        currentLevel = level;
        currentExperience = exp;
        baseAttackDamage = atk;
        baseDefense = def;
        equippedWeaponElement = weaponElement;
        // 経験値計算も更新
        experienceToNextLevel = CalculateExpToNextLevel(level);
    }

    private int CalculateExpToNextLevel(int level)
    {
        // レベルに応じた経験値計算ロジック
        return Mathf.RoundToInt(100 * Mathf.Pow(1.5f, level - 1));
    }
}