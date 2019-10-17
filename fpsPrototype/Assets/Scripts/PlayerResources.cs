using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerResources : MonoBehaviour {

    private int currXP;
    //private int currLvl;
    private int currResourceA; //currency?
    private int currResourceB; //rare gem? battery? turn in to ship for xp + currency

    [SerializeField] private GameObject inventory;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI resourceAText;
    [SerializeField] private TextMeshProUGUI resourceBText;

    private void Awake () {
        //set base text
    }

    private void Update () {
        //inventory.SetActive(Input.GetKey(KeyCode.Tab));
    }

    private void OnTriggerEnter (Collider other) {
        if (other.gameObject.name == "Ship") { DepositResourceB(); }

        if (other.gameObject.GetComponent<XPMoveToPlayer>()) {
            AddXP(1);
            other.gameObject.SetActive(false);
        }
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

    void UpdateUI () {
        //update text values
    }

}
