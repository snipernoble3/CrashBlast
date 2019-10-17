using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerResources : MonoBehaviour {

    private int currXP;
    //private int currLvl;
    private int currResourceA; //currency?
    private int currResourceB; //rare gem? battery? turn in to ship for xp + currency

    private bool canDeposit;
    

    private void OnTriggerEnter (Collider other) {
        if (other.gameObject.name == "Ship") { DepositResourceB(); }
    }

    void AddXP (int amount) { currXP += amount; }
    //void RemoveXP (int amount) { currXP -= amount; }

    void AddResourceA (int amount) { currResourceA += amount; }
    void RemoveResourceA (int amount) { currResourceA -= amount; }

    void AddResourceB (int amount) { currResourceB += amount; }
    void RemoveResourceB (int amount) { currResourceB -= amount; }
    void EmptyResourceB () { currResourceB = 0; }

    void DepositResourceB () {
        if (currResourceB == 0) return;
        //1 B : 25 XP, 10 A
        AddXP(currResourceB * 25);
        AddResourceA(currResourceB * 10);
        EmptyResourceB();
    }

    void DepositResourceB (int amount) {
        if (currResourceB == 0) return;
        //1 B : 25 XP, 10 A
        int temp = (int)Mathf.Min(amount, currResourceB);
        AddXP(temp * 25);
        AddResourceA(temp * 10);
        RemoveResourceB(temp);
    }

}
