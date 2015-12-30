using UnityEngine;
using System.Collections;

/// MouseLook rotates the transform based on the mouse delta.
/// Minimum and Maximum values can be used to constrain the possible rotation

/// To make an FPS style character:
/// - Create a capsule.
/// - Add the MouseLook script to the capsule.
///   -> Set the mouse look to use LookX. (You want to only turn character but not tilt it)
/// - Add FPSInputController script to the capsule
///   -> A CharacterMotor and a CharacterController component will be automatically added.

/// - Create a camera. Make the camera a child of the capsule. Reset it's transform.
/// - Add a MouseLook script to the camera.
///   -> Set the mouse look to use LookY. (You want the camera to tilt up and down like a head. The character already turns.)
[AddComponentMenu("Camera-Control/Mouse Look")]
public class MouseLook : MonoBehaviour {

	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;

	public float minimumX = -360F;
	public float maximumX = 360F;

	public float minimumY = -60F;
	public float maximumY = 60F;

	float rotationY = 0F;

    public Camera gameCam;
    public bool focused = false;

    Vector3 origin = Vector3.zero;
    Vector3 point = Vector3.zero;

	void Update ()
	{
        Ray ray = gameCam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        if (focused) {

            if (axes == RotationAxes.MouseXAndY) {
                float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

                rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
            }
            else if (axes == RotationAxes.MouseX) {
                transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
            }
            else {
                rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);

                if (Physics.Raycast(ray, out hit, 100)) {
                    origin = hit.point;
                    point = origin;
                    point += (new Vector3(hit.normal.x, hit.normal.y, hit.normal.z) * -((1f / 3f) / 4f));
                    Vector3 localPos = transform.InverseTransformPoint(point);
                    if (Input.GetKeyDown(KeyCode.Mouse0)) {
                        Chunk chunk = hit.collider.GetComponent<Chunk>();
                        if (chunk) {
                            //chunk.RemoveBlock(hit);
                            Debug.Log("Attempting to remove block");
                        }
                    }
                    if (Input.GetKeyDown(KeyCode.Mouse1)) {
                        Chunk chunk = hit.collider.GetComponent<Chunk>();
                        if (chunk) {
                            //chunk.AddBlock(hit, 5);
                            Debug.Log("Attempting to add block");
                        }
                    }
                }
                else {
                    origin = Vector3.zero;
                }
            }
        }

        if (origin != Vector3.zero) {
            Debug.DrawLine(ray.origin, origin, Color.red);
            Debug.DrawLine(origin, point, Color.blue);
        }

        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            focused = true;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            focused = false;
        }

        Cursor.visible = !focused;
        Screen.lockCursor = focused;
	}
	
	void Start ()
	{
		// Make the rigid body not change rotation
		if (GetComponent<Rigidbody>())
			GetComponent<Rigidbody>().freezeRotation = true;
	}

    void OnGUI() {
        GUI.Box(new Rect(Screen.width / 2 - 2.5f, Screen.height / 2 - 2.5f, 5, 5), "");
    }
}