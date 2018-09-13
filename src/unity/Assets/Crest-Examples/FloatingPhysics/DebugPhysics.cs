using UnityEngine;

//To be able to change the different physics parameters real time
public class DebugPhysics : MonoBehaviour
{
    public static DebugPhysics current;

    //Force 2 - Pressure Drag Force
    [Header("Force 2 - Pressure Drag Force")]
    public float velocityReference;

    [Header("Pressure Drag")]
    public float C_PD1 = 10f;
    public float C_PD2 = 10f;
    public float f_P = 0.5f;

    [Header("Suction Drag")]
    public float C_SD1 = 10f;
    public float C_SD2 = 10f;
    public float f_S = 0.5f;

    //Force 3 - Slamming Force
    [Header("Force 3 - Slamming Force")]
    //Power used to ramp up slamming force
    public float p = 2f;
    public float acc_max;
    public float slammingCheat;

    void Start()
    {
        current = this;
    }
}
