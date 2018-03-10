/*PART OF THIS CODE IS PROTECTED BY COPYRIGHTS (2018) AND CONSISTS A PART OF GEAR VR PROJECT                           |
 * FOR THE CENTRE FOR APPLIED RESEARCH IN NEUROPLASTICITY (SPONSORED BY SAMSUNG AUSTRALIA) .                           |
 * --------------------------------------------------------------------------------------------------------------------|
 * AUTHORS: DIMITRIS ELEFTHERIADIS, OCULUS                                                                             |
 * --------------------------------------------------------------------------------------------------------------------|
 * IF YOU WANT TO USE THE CODE OR A PART OF IT YOU HAVE TO GET AN AUTHORIZATION                                        |
 * FROM EITHER THE FIRST AUTHOR OR THE CENTRE OF APPLIED RESEARCH IN NEUROPLASTICITY                                   |
 * AND CREDIT BOTH THE AUTHORS AND THE CENTRE FOR APPLIED RESEARCH IN NEUROPLASTICITY.                                 |
 * FAILING TO DO SO WILL RESULT IN LAW ENFORCEMENT.                                                                    |
 * TO GET AN AUTHORIZATION CONTACT DIMITRIS ELEFTHERIADIS AT eleftheriadis_dimitris@yahoo.gr                           |
 *                                                                                                                     |
 ----------------------------------------------------------------------------------------------------------------------|
 
 
 */






using System;
using UnityEngine;

namespace VRStandardAssets.Utils
{
    // In order to interact with objects in the scene
    // this class casts a ray into the scene and if it finds
    // a VRInteractiveItem it exposes it for other classes to use.
    // This script should be generally be placed on the camera.
    public class VREyeRaycaster : MonoBehaviour
    {
        public event Action<RaycastHit> OnRaycasthit;                   // This event is called every frame that the user's gaze is over a collider.



        [SerializeField] private LineRenderer m_LineRenderer = null; // For supporting Laser Pointer
        public bool ShowLineRenderer = true;                         // Laser pointer visibility
        [SerializeField] private Transform m_TrackingSpace = null;   // Tracking space (for line renderer)

        [SerializeField] private Transform m_Camera;
        [SerializeField] private LayerMask m_ExclusionLayers;           // Layers to exclude from the raycast.
        [SerializeField] private Reticle m_Reticle;                     // The reticle, if applicable.
        [SerializeField] private VRInput m_VrInput;                     // Used to call input based events on the current VRInteractiveItem.
        [SerializeField] private bool m_ShowDebugRay;                   // Optionally show the debug ray.
        [SerializeField] private float m_DebugRayLength = 5f;           // Debug ray length.
        [SerializeField] private float m_DebugRayDuration = 1f;         // How long the Debug ray will remain visible.
        [SerializeField] private float m_RayLength = 500f;              // How far into the scene the ray is cast.


        private VRInteractiveItem m_CurrentInteractible;                //The current interactive item
        private VRInteractiveItem m_LastInteractible;                   //The last interactive item
        public string m_IIName;
        private string[] objArrayName1 = { "Clock", "Train", "Chair", "Pant", "Bed", "BlueHat" };
        private string[] objArrayName2 = { "BookOld", "Lambo", "NotebookGRN", "Airplane", "WhiteCup", "Teddy" };
        private string phase = "A";
        private int counter = 0;           // A counter of the user's choices for each trial
        private int correctChoiceA = 0;   //Correct choices of trial A
        private int wrongChoiceA = 0;   //Wrong choices of trial A
        private int correctChoiceB = 0;   //Correct choices of trial B
        private int wrongChoiceB = 0;   //Wrong choices of trial A
        private int totalCorrect = 0;   //Total correct choices of both trials
        private int totalWrong = 0;   //Total wrong choices of both trials
        private GameObject[] objNameArrayCorrect = new GameObject[12]; // Store all the correct objects in this array
        private GameObject[] objNameArrayWrong = new GameObject[12];   // Store all the wrong objects in this array



       

        // Utility for other classes to get the current interactive item
        public VRInteractiveItem CurrentInteractible
        {
            get { return m_CurrentInteractible; }
        }


        private void OnEnable()
        {
            m_VrInput.OnClick += HandleClick;
            m_VrInput.OnDoubleClick += HandleDoubleClick;
            m_VrInput.OnUp += HandleUp;
            m_VrInput.OnDown += HandleDown;
        }


        private void OnDisable()
        {
            m_VrInput.OnClick -= HandleClick;
            m_VrInput.OnDoubleClick -= HandleDoubleClick;
            m_VrInput.OnUp -= HandleUp;
            m_VrInput.OnDown -= HandleDown;
        }


        private void Update()
        {
            EyeRaycast();
        }


        private void EyeRaycast()
        {
            // Show the debug ray if required
            if (m_ShowDebugRay)
            {
                Debug.DrawRay(m_Camera.position, m_Camera.forward * m_DebugRayLength, Color.blue, m_DebugRayDuration);
            }

            // Create a ray that points forwards from the camera.
            Ray ray = new Ray(m_Camera.position, m_Camera.forward);
            RaycastHit hit;

            Vector3 worldStartPoint = Vector3.zero;
            Vector3 worldEndPoint = Vector3.zero;
            if (m_LineRenderer != null)
            {
                m_LineRenderer.enabled = ControllerIsConnected && ShowLineRenderer;
            }

            if (ControllerIsConnected && m_TrackingSpace != null)
            {
                Matrix4x4 localToWorld = m_TrackingSpace.localToWorldMatrix;
                Quaternion orientation = OVRInput.GetLocalControllerRotation(Controller);

                Vector3 localStartPoint = OVRInput.GetLocalControllerPosition(Controller);
                Vector3 localEndPoint = localStartPoint + ((orientation * Vector3.forward) * 500.0f);

                worldStartPoint = localToWorld.MultiplyPoint(localStartPoint);
                worldEndPoint = localToWorld.MultiplyPoint(localEndPoint);

                // Create new ray
                ray = new Ray(worldStartPoint, worldEndPoint - worldStartPoint);
            }
            // Debug.Log("counter is: " +counter);

            // Do the raycast forwards to see if we hit an interactive item
            if (Physics.Raycast(ray, out hit, m_RayLength, ~m_ExclusionLayers))
            {
                VRInteractiveItem interactible = hit.collider.GetComponent<VRInteractiveItem>(); //attempt to get the VRInteractiveItem on the hit object
              
                if (interactible)
                {
                    worldEndPoint = hit.point;
                }
                m_CurrentInteractible = interactible;
                Debug.Log("The ray has hit the: " + m_CurrentInteractible.ToString());
                m_CurrentInteractible.Over();



                 Debug.Log("Name of Object: " + m_CurrentInteractible.nameOfObject);

                // If we hit an interactive item and it's not the same as the last interactive item, then call Over
                if (interactible && interactible != m_LastInteractible)
                    interactible.Over();


                // Deactive the last interactive item 
                if (interactible != m_LastInteractible)
                    DeactiveLastInteractible();

                m_LastInteractible = interactible;


                // Something was hit, set at the hit position.
                if (m_Reticle)
                    m_Reticle.SetPosition(hit);

                if (OnRaycasthit != null)
                    OnRaycasthit(hit);

               

               


            }
            else
            {
                // Nothing was hit, deactive the last interactive item.
                DeactiveLastInteractible();
                m_CurrentInteractible = null;

                // Position the reticle at default distance.
                if (m_Reticle)
                    m_Reticle.SetPosition(ray.origin, ray.direction);
            }

            if (ControllerIsConnected && m_LineRenderer != null)
            {
                m_LineRenderer.SetPosition(0, worldStartPoint);
                m_LineRenderer.SetPosition(1, worldEndPoint);
            }
        }

        public bool ControllerIsConnected
        {
            get
            {
                OVRInput.Controller controller = OVRInput.GetConnectedControllers() & (OVRInput.Controller.LTrackedRemote | OVRInput.Controller.RTrackedRemote);
                return controller == OVRInput.Controller.LTrackedRemote || controller == OVRInput.Controller.RTrackedRemote;
            }
        }
        public OVRInput.Controller Controller
        {
            get
            {
                OVRInput.Controller controller = OVRInput.GetConnectedControllers();
                if ((controller & OVRInput.Controller.LTrackedRemote) == OVRInput.Controller.LTrackedRemote)
                {
                    return OVRInput.Controller.LTrackedRemote;
                }
                else if ((controller & OVRInput.Controller.RTrackedRemote) == OVRInput.Controller.RTrackedRemote)
                {
                    return OVRInput.Controller.RTrackedRemote;
                }
                return OVRInput.GetActiveController();
            }
        }


        private void DeactiveLastInteractible()
        {
            if (m_LastInteractible == null)
                return;

            m_LastInteractible.Out();
            m_LastInteractible = null;
        }



        private void HandleUp()
        {
            if (m_CurrentInteractible != null)
                m_CurrentInteractible.Up();
        }


        private void HandleDown()
        {
            if (m_CurrentInteractible != null)
                m_CurrentInteractible.Down();
        }


        private void HandleClick()
        {
            if (m_CurrentInteractible != null)
            {
                //*********************************************************************************    STARTS HERE (copyrigthed as mentioned above)     *****************************************************************************

               
                
                    if (counter < 6)
                    {

                        if (m_CurrentInteractible.CompareTag("PhaseA"))// If the selected object belongs in trial A
                        {
                            if (m_CurrentInteractible.selected == false) // If the item was not selected before
                            {

                                if (phase == "A")  //If the selected object belongs to trial A
                                {
                                    correctChoiceA++; //Correct choices incremented by 1
                                    totalCorrect++;   //Increament total correct choices by 1
                                    m_CurrentInteractible.gameObject.SetActive(false); //This is for Debugging purposes. We set the visibility of the object to false
                                counter++;
                            }

                                else
                                {
                                    wrongChoiceA++; //Wrong choices increamented by 1
                                    totalWrong++;   //Increament total wrong choices by 1;
                                   

                                }

                                m_CurrentInteractible.selected = true; //we select the item turning the flag true
                                //counter++; //We increse the counter of total selected items
                                
                            }
                            else //The item was selected before and is now deselected
                            {

                                if (CurrentInteractible.CompareTag("PhaseA")) //if the item belonged to trial A
                                {

                                    correctChoiceA--;    //The correct choices are decreased by 1
                                    totalCorrect--;      //Decrease total correct choices by 1

                                }
                                else
                                { //If the item belonged in trial B then the wrong choices are decreased
                                    wrongChoiceA--;     // Wrong choises decreased by 1
                                    totalWrong--;       //Decrease total wrong choices by 1
                                }
                                m_CurrentInteractible.selected = false; //we deselect the item turning the select flag to false
                                counter--; //We decrease the counter of total selected items

                            }
                        }

                        else if (m_CurrentInteractible.CompareTag("PhaseB")) // If the selected object belongs to trial B
                        {

                            if (m_CurrentInteractible.selected == false)  // If the item was not selected before
                            {

                                if (phase == "B")  //If the selected object belongs to trial B
                                {
                                    correctChoiceB++; //Correct choices incremented by 1
                                    totalCorrect++;   //Increament total correct by 1
                                    m_CurrentInteractible.gameObject.SetActive(false);
                                    counter++;
                            }

                                else
                                {
                                    wrongChoiceB++; //Wrong choices increamented by 1
                                    totalWrong++;   //Increament total wrong choices by 1
                                }

                                CurrentInteractible.selected = true;
                                //counter++;
                            }

                            else //The item was selected before and is now deselected
                            {
                                m_CurrentInteractible.selected = false;  //we deselect the item turning the select flag to false
                                if (CurrentInteractible.CompareTag("PhaseB")) //if the item belonged to trial B
                                {

                                    correctChoiceB--;    //The correct choices are decreased by 1
                                totalCorrect--;          //Decrease total correct choices by 1

                                }
                                else
                                { //If the item belonged in phase A then the wrong choices are decreased
                                    wrongChoiceB--;     // Wrong choises decreased by 1
                                totalWrong--; //Decrease total wrong choices by 1
                                }

                                CurrentInteractible.selected = false; // Object was deselected
                                counter--; //Decrease the counter by 1
                               

                            }
                        }
                    }
                    
                 //If Button ends here
                else
                {

                    if (counter >= 5) // If the user has selected all 6 objects and they are ready to go to the next trial or end the game
                    {
                        if (phase == "A")
                        {
                            //Load Trial B
                            phase = "B";  // Load trial B                 
                            counter = 0; //Reset counter
                           

                        }
                        else
                        {
                           
                            //End game
                        }


                    }



                }






                //*********************************************************************************** ENDS HERE **************************************************************************************

               
                m_CurrentInteractible.Click();
            }
        }


        private void HandleDoubleClick()
        {
            if (m_CurrentInteractible != null)
                m_CurrentInteractible.DoubleClick();

        }
    }
}