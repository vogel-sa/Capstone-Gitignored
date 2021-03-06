﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Pathfinding;

public class PlayerCharacterStats : MonoBehaviour, ICharacterStats
{
    #region
    [SerializeField]
    string _name;
    public string Name
    {
        get
        {
            return _name;
        }

        set
        {
            _name = value;
        }
    }

    [SerializeField]
    private int _maxHP;
    public int MaxHP
    {
        get
        {
            return _maxHP;
        }

        set
        {
            _maxHP = value;
        }
    }

    [SerializeField]
    private int _currHP;
    public int CurrHP
    {
        get
        {
            return _currHP;
        }

        set
        {
            if (CurrHP + value > _maxHP)
            {
                _currHP = _maxHP;
            }
            else if (CurrHP - value < 0)
            {
                _currHP = 0;
            }
    
            _currHP = value;
        }
    }

    /* Represents a scalar damage reduction value   
    A value of 1 would reduce all incoming damage by 1
    Value of 2 reduces all incoming damage by 2, etc.*/
    [SerializeField]
    private int _mitigationValue = 0;
    public int MitigationValue
    {
        get
        {
            return _mitigationValue;
        }

        set
        {
            _mitigationValue = value;
        }
    }

    [SerializeField]
    private int _movementRange;
    public int MovementRange
    {
        get
        {
            return _movementRange;
        }

        set
        {
            if (value <= 0)
            {
                throw new ArgumentException("Movement Range cannot be less than 1");
            }
            _movementRange = value;
        }
    }

    public bool hasMoved;

    [SerializeField]
    private int _maxActions = 1;
    /// <summary>
    /// Number of Actions a character has in a turn. Default at turn start is 1,
    /// Every attack on the bar costs 1. Free Actions cost 0.
    /// </summary>
    [SerializeField]
    private int _actionsleft;
    public int Actionsleft
    {
        get
        {
            return _actionsleft;
        }

        set
        {
            _actionsleft = value;
        }
    }

    [SerializeField]
    Texture2D _portrait;
    public Texture2D Portrait
    {
        get
        {
            return _portrait;
        }

        set
        {
            _portrait = value;
        }
    }

	[SerializeField]
	private AudioClip _movementSound;
	public AudioClip MovementSound
	{
		get
		{
			return _movementSound;
		}

	}

	[SerializeField]
	private AudioClip _deathSound;
	public AudioClip DeathSound
	{
		get
		{
			return _deathSound;
		}

	}

	[SerializeField]
	private AudioClip _damageSound;
	public AudioClip DamageSound
	{
		get
		{
			return _damageSound;
		}

	}


	[SerializeField]
	bool _isCoveringFire;
	public bool isCoveringFire
	{
		get
		{
			return _isCoveringFire;
		}

		set
		{
			_isCoveringFire = value;
		}
	}


	[SerializeField]
	bool _isFortifying;
	public bool isFortifying
	{
		get
		{
			return _isFortifying;
		}

		set
		{
			_isFortifying = value;
		}
	}


    #endregion

    #region
    [SerializeField]
    AbilityData[] _abilityData;
    public AbilityData[] AbilityData
    {
        get
        {
            return _abilityData;
        }
    }
    #endregion




	public void startCoverFire() {
		isCoveringFire = true;
	}

    void Awake()
    {
        //CurrHP = MaxHP;
		isCoveringFire = false;
    }

    void OnEnable()
    {
		FindObjectOfType<TurnManager>().OnTurnChange += OnTurnStart;
    }

    public bool IsDead()
    {
        return CurrHP <= 0;
    }

    public void TakeDamage(int damage)
    {
        if (MitigationValue <= damage)
        {
            GetComponentInChildren<Animator>().SetTrigger("TakeDamage");
            var dmgtaken = damage - MitigationValue;
			var audio = GetComponent<AudioManager>();
			if (audio != null) {
				audio.playSoundEffect (DamageSound);
			}

			if (GetComponent<DamageText>() != null) {
            GetComponent<DamageText>().displayText(dmgtaken, 1.1f);
			}
            CurrHP = Math.Max(CurrHP - dmgtaken, 0);
            if (IsDead())
            {
                StartCoroutine(DeadCleanup());
            }
        }
    }


    public IEnumerator DeadCleanup() {

        var manager = FindObjectOfType<TurnManager>();
		var audio = manager.GetComponent<AudioManager>();
        manager.playerList.Remove(this);
        var pm = FindObjectOfType<PathManager>();
        pm.allies.Remove(GetComponent<SingleNodeBlocker>());
        
        GetComponentInChildren<Animator>().SetBool("Die", true);
        yield return new WaitForSeconds(1);

		if (audio != null) {
			audio.playSoundEffect (DeathSound);
		}
        yield return new WaitForSeconds(5);
        gameObject.SetActive(false);
        manager.CheckGameOver();    
        //Destroy(this.gameObject);

    }

	public void CheckCharacterCannotMove()
	{
		if (Actionsleft == 0 && hasMoved)
		{
			// Turn character greyscale. Undo this at the start of the player turn.
			GetComponentInChildren<MeshRenderer>().material.color = Color.gray;
		}
	}

    private void OnTurnStart(IList<PlayerCharacterStats> players, IList<EnemyStats> enemies, TurnManager.GAMESTATE turn)
    {
        if (turn == TurnManager.GAMESTATE.PLAYERTURN)
        {
            foreach (var ability in AbilityData)
            {
                ability.Currcooldown = Mathf.Max(ability.Currcooldown - 1, 0);
				if (ability.Name == "Fortify" && isFortifying) {
					MitigationValue -= ability.DamageAmount;
				}

			}
            hasMoved = false;
            Actionsleft = _maxActions;
			isCoveringFire = false;
			isFortifying = false;
			//MitigationValue = 1;
        }
    }
}
