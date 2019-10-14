using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class Health : MonoBehaviour {

    public enum AlignmentTag { Player, Ally, Enemy, Neutral };
    [SerializeField] AlignmentTag alignment;

    [SerializeField] int maxHealth = 3;
    private int currHealth;

    public TextMeshProUGUI healthText;
    private string baseText;

    private bool alive = true;
    
    void Awake() {
        currHealth = maxHealth;
        try {
            baseText = healthText.text;
        } catch (NullReferenceException e) {

        }
        
        UpdateUI();
    }
    
    public void GainHealth (int heal) {
        if (alive) {
            currHealth = Mathf.Min(currHealth + heal, maxHealth);
            UpdateUI();
        }
    }

    public void TakeDamage (int damage) {
        currHealth -= damage;
        UpdateUI();
        if (currHealth <= 0) {
            alive = false;
            OnDeath();
        }
    }

    void UpdateUI () {
        try {
            healthText.text = baseText + currHealth;
        } catch (NullReferenceException e) {

        }
    }

    public void Kill () {
        currHealth = 0;
        alive = false;
        OnDeath();
    }

    private void OnDeath () {
        switch (alignment) {
            case AlignmentTag.Player:
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;
            default:
                this.gameObject.SetActive(false);
                break;
        }
        
    }
    /*
    IEnumerator OnDeath () {
        //play death animation
        yield return new WaitForEndOfFrame();
        this.gameObject.SetActive(false);
    }
    */

    //if tag != player on mouse over show healthbar if not full health

    public bool IsAlive () { return alive; }

    public AlignmentTag GetTag () { return alignment; }
    

}
