using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [Header("��{�X�e�[�^�X")]
    public int baseAttackDamage = 10;
    public float baseDefense = 0f; // �_���[�W�y���� (0-1, 0.1 = 10%�y��)
    public int currentLevel = 1;
    public int currentExperience = 0;
    public int experienceToNextLevel = 100;

    [Header("�����U���̓{�[�i�X")]
    [Tooltip("�e�����ɑΉ�����U���̓{�[�i�X (��: Fire: +5)")]
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

    [Header("�������̕��푮��")]
    public ElementType equippedWeaponElement = ElementType.None; // ���ݑ������Ă��镐��̑���

    void Awake()
    {
        InitializeElementalAttackBonus();
    }

    // Dictionary���C���X�y�N�^�[�ŕ\���ł��Ȃ����߁A����������
    void InitializeElementalAttackBonus()
    {
        // ����Awake�ŏ���������Ă���̂ŁA�����ł͉������Ȃ�
        // �����C���X�y�N�^�[����ݒ�ł���悤�ɂ������ꍇ�́A
        // ScriptableObject��J�X�^���G�f�B�^���K�v�ɂȂ�܂��B
    }

    // �o���l�̒ǉ��ƃ��x���A�b�v
    public void AddExperience(int amount)
    {
        currentExperience += amount;
        Debug.Log($"�o���l�� {amount} �l�����܂����B���݌o���l: {currentExperience}");

        while (currentExperience >= experienceToNextLevel)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentExperience -= experienceToNextLevel;
        currentLevel++;
        baseAttackDamage += 2; // ��: ���x���A�b�v�ōU���͑���
        // baseDefense += 0.01f; // ��: �h��͂�����
        experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * 1.5f); // ���̃��x���܂ł̌o���l������

        Debug.Log($"���x���A�b�v�I���x��: {currentLevel}, �U����: {baseAttackDamage}");
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"���x���A�b�v�I���x�� {currentLevel}", 3f);
    }

    // �����U���̓{�[�i�X��ݒ�
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

    // ����̑����̍U���̓{�[�i�X���擾
    public int GetElementalAttackBonus(ElementType element)
    {
        if (elementalAttackBonus.ContainsKey(element))
        {
            return elementalAttackBonus[element];
        }
        return 0;
    }

    // �������̕���̑�����ύX
    public void SetEquippedWeaponElement(ElementType element)
    {
        equippedWeaponElement = element;
        Debug.Log($"�������̕��푮���� {equippedWeaponElement} �ɕύX����܂����B");
    }

    // �Z�[�u/���[�h�p���\�b�h
    public void SetStats(int level, int exp, int atk, float def, ElementType weaponElement)
    {
        currentLevel = level;
        currentExperience = exp;
        baseAttackDamage = atk;
        baseDefense = def;
        equippedWeaponElement = weaponElement;
        // �o���l�v�Z���X�V
        experienceToNextLevel = CalculateExpToNextLevel(level);
    }

    private int CalculateExpToNextLevel(int level)
    {
        // ���x���ɉ������o���l�v�Z���W�b�N
        return Mathf.RoundToInt(100 * Mathf.Pow(1.5f, level - 1));
    }
}