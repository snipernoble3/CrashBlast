using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour {

    public enum AlignmentTag { Player, Ally, Enemy, Neutral };
    [SerializeField] AlignmentTag alignment;

    [SerializeField] int maxHealth = 3;
    private int currHealth;

    private bool alive = true;
    
    void Awake() {
        currHealth = maxHealth;
    }
    
    public void GainHealth (int heal) {
        if (alive) {
            currHealth = Mathf.Min(currHealth + heal, maxHealth);
        }
    }

    public void TakeDamage (int damage) {
        currHealth -= damage;
        if (currHealth <= 0) {
            alive = false;
            OnDeath();
        }
    }

    public void Kill () {
        currHealth = 0;
        alive = false;
        OnDeath();
    }

    private void OnDeath () {
        this.gameObject.SetActive(false);
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
