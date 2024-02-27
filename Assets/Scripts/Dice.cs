using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class Dice : MonoBehaviour
{
    //general
    private Rigidbody _rb;
    private float _mouseFollowSpeed = 20f;

    //dice values
    [NonSerialized] public bool isHeld = false;
    [NonSerialized] public bool isRolling = false;
    private float _startingMass;
    private Vector3 _startingPosition;
    public int diceID;

    //sides array
    [SerializeField] public DiceSideAndValue[] diceSides; //list of structs of DiceSide and values
       
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _startingPosition = transform.position;
        _startingMass = _rb.mass;
    }

    private void Start()
    {
        SetDiceTextValues();
    }

    public void SetDiceTextValues() //sets text values of values in inspector
    {
        for (int i = 0; i < diceSides.Length; i++)
        {
            diceSides[i].diceSide.SideValue = diceSides[i].value;
        }
    }

    public void HoldTheDice()
    {
        Debug.Log("HoldTheDice()");

        _rb.useGravity = false;
        _rb.mass = _startingMass;
    }

    public void DropTheDice(Vector3 dir)
    {
        Debug.Log("DropTheDice()");

        isRolling = true;
        _rb.AddForce(dir * 100, ForceMode.Impulse); //adds force to throw in direction of mouse movement so it doesn't flop
        _rb.useGravity = true;

        StartCoroutine(WaitForRollingToStop()); //coroutine to wait for dice to stop rolling
    }

    public void ResetTheDice()
    {
        Debug.Log("ResetTheDice()");

        StopAllCoroutines(); //stops WaitForRollingToStop()
        _rb.velocity = Vector3.zero; //reset forces on rigidbody
        
        isRolling = false;
        _rb.useGravity = false;

        _rb.MovePosition(_startingPosition); //reset pos
    }

    public void MoveDiceTowardsMouse(Vector3 target, Vector3 deltaPoint)
    {
        if (Vector3.Distance(target, deltaPoint) > 0.12f) //checks if mouse movement was long enough to add torque that doesn't look weird
        {
            Vector3 deltaDir = transform.TransformDirection(deltaPoint.normalized); //calculate direction of last frame mouse position
            Vector3 dirToDice = (transform.position - target).normalized; //calculates direction from current mouse pos towards dice

            Vector3 cross = Vector3.Cross(dirToDice, deltaDir); //cross of those two is the direction in which the dice should spin
            Vector3 torque = cross * 100f;

            _rb.AddTorque(torque, ForceMode.Force);    
        }

        _rb.MovePosition(Vector3.Lerp(_rb.position, target, Time.deltaTime * _mouseFollowSpeed)); //smoothly moves dice towards mouse pos
    }

    private IEnumerator WaitForRollingToStop()
    {
        float safetyTimer = 5f;
        yield return new WaitForSeconds(0.1f);

        DiceRollManager.instance.ToggleBoxColliders(true); //colliders are activated so the dice doesn't escape

        while (!_rb.IsSleeping()) //waiting for the dice to stop moving
        {
            yield return new WaitForSeconds(0.1f);
            safetyTimer -= 0.1f;

            if (safetyTimer <= 0f) //in case the dice gets stuck in a weird position on a vertice or something
            {
                Debug.Log("Safety timer activated");
                _rb.AddForce(new Vector3(RandomFloat(), 0f, RandomFloat()));
                safetyTimer = 5f;
            }
        }

        isRolling = false;

        int rolledValue = ReadDiceValue().diceSide.SideValue; //gets the top side value
        DiceRollManager.instance.SetResultText(diceID, rolledValue);
    }

    public void ForceRoll() //adds random force and torque to simulate a dice cast
    {
        _rb.mass = _startingMass;
        isRolling = true;
        _rb.useGravity = true;

        Vector3 randomVector = new Vector3 (RandomFloat() , 0.07f , RandomFloat());
        _rb.AddTorque(-randomVector * UnityEngine.Random.Range(100f, 200f), ForceMode.Impulse);
        _rb.AddForce(randomVector * UnityEngine.Random.Range(150f, 200f), ForceMode.Impulse);

        StartCoroutine(WaitForRollingToStop());
    }

    private float RandomFloat() //generates a random float that is not too close to 0f
    {
        float toReturn = 0f;

        float rnd = UnityEngine.Random.Range(0f,10f);
        if (rnd > 5f)
            toReturn = UnityEngine.Random.Range(-1f, -0.25f);
        else
            toReturn = UnityEngine.Random.Range(0.25f, 1f);

        return toReturn;
    }

    private DiceSideAndValue ReadDiceValue() //checking which side of the dice has the highest transform.y value, the highest one is gonna be the "top" side
    {
        DiceSideAndValue toReturn = new DiceSideAndValue();

        foreach (var item in diceSides)
        {
            if (toReturn.diceSide == null)
                toReturn = item;
            else if (item.diceSide.transform.position.y > toReturn.diceSide.transform.position.y)
            {
                toReturn = item;
            }
        }

        return toReturn;
    }

    private void OnTriggerExit(Collider col) //safety trigger in case a dice runs out of the cage
    {
        if (col.gameObject.transform.tag == "SafetyBox")
        {
            DiceRollManager.instance.ResetTheDice();
        }
    }

    [Serializable]
    public struct DiceSideAndValue //struct for keeping dice sides' values
    {
        public DiceSide diceSide;
        public int value;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(Dice))]
public class DiceEditor : Editor
{
    private SerializedProperty diceSides;
    private SerializedProperty diceID;

    private void OnEnable()
    {
        diceSides = serializedObject.FindProperty("diceSides");
        diceID = serializedObject.FindProperty("diceID");
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        serializedObject.Update();

        EditorGUILayout.Space(5f);

        EditorGUILayout.PropertyField(diceID, new GUIContent("Dice ID: "));

        EditorGUILayout.Space(15f);

        for (int i = 0; i < diceSides.arraySize; i++) //takes the list of dice info struct, and paints propertyfields of values of dice sides
        {
            SerializedProperty element = diceSides.GetArrayElementAtIndex(i);
            SerializedProperty val = element.FindPropertyRelative("value");

            EditorGUILayout.PropertyField(val, new GUIContent("Side " + (i + 1).ToString() + " value: "));
            EditorGUILayout.Space(1f);
        }

        EditorGUILayout.Space(15f);

        Dice dice = (Dice)target;

        if (GUILayout.Button("Apply values"))
        {
            dice.SetDiceTextValues();
            EditorUtility.SetDirty(dice);
        }

        serializedObject.ApplyModifiedProperties();
    }

}

#endif
