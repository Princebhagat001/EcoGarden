using ithappy.Animals_FREE;
using UnityEngine;
[RequireComponent(typeof(CreatureMover))]
public class AnimalController : MonoBehaviour
{
private CreatureMover m_Mover;
private Vector2 m_Axis;
private Vector3 m_Target;
private bool m_IsRun = false;
private bool m_IsJump = false;
[SerializeField]
private GameObject waypoint1;
[SerializeField]
private GameObject waypoint2;
[SerializeField]
private GameObject waypoint3;
[SerializeField]
private GameObject waypoint4;
[SerializeField]
private GameObject target;
private const float CLOSE_DISTANCE = 1;
[SerializeField]
private float WaitTime = 4;
private float currentTime = 0;
private void Awake()
{
m_Mover = GetComponent<CreatureMover>();
currentTime = WaitTime;
}
public void BindMover(CreatureMover mover)
{
m_Mover = mover;
}
// Start is called once before the first
// execution of Update after the MonoBehaviour is created
void Start()
{
}
// Update is called once per frame
void Update()
{
// Determine the direction to the current target waypoint.
Vector3 direction = target.transform.position - transform.position;
direction.y = 0;
// Calculates the length of the relative position vector
float distance = direction.magnitude;
// Calculate the normalised direction to the target from a game object.
Vector3 normDirection = direction / distance;
// Set the movement direction.
m_Axis.x = normDirection.x;
m_Axis.y = normDirection.z;
// Set target to zero. Asset needs this.
m_Target = Vector3.zero;
// Check if close to the current target.
if (distance < CLOSE_DISTANCE)
{
if (currentTime <= 0)
{
// Change the target.
if (target.Equals(waypoint1))
{
target = waypoint2;
}
else if (target.Equals(waypoint2))
{
target = waypoint3;
}
else if (target.Equals(waypoint3))
{
target = waypoint4;
}
else if (target.Equals(waypoint4))
{
target = waypoint1;
}
// Rest the timer.
currentTime = WaitTime;
}
else
{
// Wait at location.
currentTime -= Time.deltaTime;
m_Axis.x = 0;
m_Axis.y = 0;
}
}
// Move instruction to the asset.
if (m_Mover != null)
{
m_Mover.SetInput(in m_Axis, in m_Target, in m_IsRun, m_IsJump);
}
}
}