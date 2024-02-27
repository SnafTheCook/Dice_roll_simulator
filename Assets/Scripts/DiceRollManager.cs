using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Runtime.CompilerServices;
using System.Linq;

public class DiceRollManager : MonoBehaviour
{
    //singleton
    public static DiceRollManager instance;

    //vmouse following
    private Camera _mainCam;
    private Vector3 _lastFrameMousePos; //variable held to calculate direction of mouse movement
    private Vector3 _currentFrameMousePos;

    //dice related
    private List<Dice> _dice = new List<Dice>(); //futureproof: it works with more than one dice! 
    [SerializeField] private LayerMask _dicePlaneLayer; //invisible plane for raycast to hold a static height of collision point of raycast
    [SerializeField] private LayerMask _diceLayer;
    private bool _areDiceHeld = false;

    //colliders
    [SerializeField] private List<BoxCollider> _boxColliders;

    //UI
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _totalText;
    [SerializeField] private Button _rollButton;
    private bool _isResultTextDefault = true;

    //misc vars
    private int _totalPoints = 0;

    //events
    public static UnityAction OnRollingEnd;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance);

        instance = this;

        _mainCam = Camera.main;
        FindAllDiceInScene();
        ToggleBoxColliders(false); //toggle colliders off so dice thrown from far away can freely enter the desktop space //could probably get rid of that after adding SafetyBox
        SetResultText(-1, 0); //set initial text values

        OnRollingEnd += () => _rollButton.interactable = true; //at first I used this action in a different way but it would be a shame to delete it
    }

    private void Update()
    {
        if (CheckForDiceRolling()) //guard clause, no interactions allowed when dice are rolling
            return;

        if (Input.GetMouseButtonDown(0) && !_areDiceHeld) //raycast check if dice is under the mouse
        {
            if (Physics.Raycast(_mainCam.ScreenPointToRay(Input.mousePosition), out RaycastHit rayOut, 100f, _diceLayer))
            {
                if (rayOut.transform.tag == "Dice")
                {
                    SetResultText(-1, 0);
                    
                    _currentFrameMousePos = rayOut.point;
                    HoldTheDice();

                    _rollButton.interactable = false;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0) && _areDiceHeld) //dice are cast, last calculation of mouse positions to add force to dice
        {
            if (Physics.Raycast(_mainCam.ScreenPointToRay(Input.mousePosition), out RaycastHit rayOut, 100f, _dicePlaneLayer))
            {
                _lastFrameMousePos = _currentFrameMousePos;
                _currentFrameMousePos = rayOut.point;

                Vector3 dir = _currentFrameMousePos - _lastFrameMousePos;
                dir = new Vector3(dir.x, 0.05f, dir.z);

                if (dir.magnitude < 0.3f) //checking if mouse moves fast enough for a realiable roll
                    ResetTheDice();
                else
                    DropTheDice(dir);
            }
        }
        else if (Input.GetMouseButton(0) && _areDiceHeld) //calculating mouse positions to simulate dice movement and rotation when dice are held
        {
            if (Physics.Raycast(_mainCam.ScreenPointToRay(Input.mousePosition), out RaycastHit rayOut, 100f, _dicePlaneLayer))
            {
                _lastFrameMousePos = _currentFrameMousePos;
                _currentFrameMousePos = rayOut.point;

                MoveDiceTowardsMouse(_currentFrameMousePos, _lastFrameMousePos);
            }
        }
    }

    private void FindAllDiceInScene() //adds only active dice to dicelist
    {
        List<Dice> temp = FindObjectsOfType<Dice>().ToList();
        foreach (var item in temp)
        {
            if (item.gameObject.activeInHierarchy)
                _dice.Add(item);
        }
        
    }

    private void HoldTheDice() //grab all dice
    {
        ToggleBoxColliders(false);
        _areDiceHeld = true;

        foreach (var item in _dice)
        {
            item.HoldTheDice();
        }
    }

    private void DropTheDice(Vector3 dir) //drops dice and adds force to each one in a direction of mouse movement
    {
        _areDiceHeld = false;

        foreach (var item in _dice)
        {
            item.DropTheDice(dir);
        }

        StartCoroutine(WaitForAllDiceToStopRolling());
    }
    public void ResetTheDice() //resets positions, forces, values, etc.
    {
        _areDiceHeld = false;
        StopAllCoroutines(); //stop waiting for rolling to end, it's not gonna end

        foreach (var item in _dice)
        {
            item.ResetTheDice();
        }

        OnRollingEnd?.Invoke(); //roll button interactable
    }
    private void MoveDiceTowardsMouse(Vector3 target, Vector3 deltaPoint) //move all dice towards mouse pos
    {
        foreach (var item in _dice)
        {
            item.MoveDiceTowardsMouse(target, deltaPoint);
        }
    }
    private void ForceRoll() //roll button, adds random force to each dice
    {
        ResetTheDice();
        ToggleBoxColliders(false);
        SetResultText(-1, 0);
        _areDiceHeld = false;

        foreach (var item in _dice)
        {
            item.ForceRoll();
        }

        StartCoroutine(WaitForAllDiceToStopRolling());
    }

    private bool CheckForDiceRolling() //checks if all dice are in place
    {
        foreach (var item in _dice)
        {
            if (item.isRolling)
                return true;
        }

        return false;
    }

    private IEnumerator WaitForAllDiceToStopRolling()
    {
        yield return new WaitForSeconds(2f); //no need to check for the first two seconds, maybe even more

        while (CheckForDiceRolling())
        {
            yield return new WaitForSeconds(0.2f);
        }

        OnRollingEnd?.Invoke();
    }


    public void ToggleBoxColliders(bool b) //colliders that close dice over the desktop
    {
        foreach (var item in _boxColliders)
        {
            item.enabled = b;
        }
    }

    public void SetResultText(int diceID, int value) //resets text in case of new roll, adds text in case of more than one dice
    {
        if (diceID == -1)
        {
            _resultText.text = "Result: ?";
            _isResultTextDefault = true;
        }
        else
        {
            if (_isResultTextDefault)
            {
                _isResultTextDefault = false;
                _resultText.text = "ResultD" + diceID.ToString() + ": " + value.ToString() + "\n";
            }
            else
            {
                _resultText.text += "ResultD" + diceID.ToString() + ": " + value.ToString() + "\n";
            }

            _totalPoints += value;
        }

        SetTotalText();
    }

    private void SetTotalText()
    {
        _totalText.text = "Total: " + _totalPoints.ToString();
    }

    public void RollButton()
    {
        ForceRoll();

        _rollButton.interactable = false;
    }
}
