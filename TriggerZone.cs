using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.pigsels.BubbleTrouble
{
    /// <summary>
    /// Determines collision zone. Wrappers collision enter and leave events.
    /// Used as a detector by <see cref="ZoneTriggerGameTrigger"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TriggerZone : MonoBehaviour
    {
        public delegate void TriggerHandler(TriggerZone triggerZone, Collider2D collider1, Collider2D collider2, MonoBehaviour collidedMonoBehaviour);

        public event TriggerHandler OnTriggerExit;
        public event TriggerHandler OnTriggerEnter;

        /// <summary>
        /// Gameobjects collider. Determines collision zone borders
        /// </summary>
        private Collider2D objectCollider;

        /// <summary>
        /// Dictionary of objects inside trigger zone and their number of colliders which are inside trigger zone too.
        /// </summary>
        private Dictionary<MonoBehaviour, int> objectsInsideZone = new Dictionary<MonoBehaviour, int>();

        private void Awake()
        {
            // Make sure this GameObject won't affect Trigger colliders.
            gameObject.tag = "Detector";

            objectCollider = gameObject.GetComponent<Collider2D>();

            if (!objectCollider.isTrigger)
            {
                Debug.LogWarning($"Collider of {objectCollider.gameObject} is not set to trigger mode. Collisions may be ignored");
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            //Debug.Log($"On trigger enter for : {other.gameObject}. Parent: {other.transform.parent?.name}");

            // If a GameEntity has multiple children with their own colliders
            // then in case when two or more of children enter zone's collider during same fixUpdate then methods OnTriggerEnter2D and OnTriggerExit2D may trigger in wrong order.
            // For instance if GameEntity has 5 children and all of them get inside collider during the same FixedUpdate then the sequence of  OnTriggerEnter2D and  OnTriggerExit2D events
            // may be the following: OnTriggerEnter2D for child_1 (child_1 gets destroyed or deactivated during this call) -> OnTriggerExit2D for child_1 -> 
            // -> OnTriggerExit2D for child_2,child_3, ... child_5 -> OnTriggerEnter2D for child_2,child_3, ... child_5.
            // Subsequently, this misorder causes misorder in object registration and unregistration in zone order.
            // Extra explanations can be found in comments for this task https://trello.com/c/yRvGhiJZ/300-triggerzonegametrigger-add-invisible-trigger-zones

            // The two following checks solve the issue described above:

            // Checking if the object is active.
            // If it's not then it means that the object has already been deactivated and left zone (OnTriggerExit2D was already called).
            if (!other.gameObject.activeInHierarchy)
            {
                // Debug.LogWarning("Inactive object caught");
                return;
            }

            // Checking if the objects' collider component is still enabled.
            // If it's not then it means that the object  has already been destroyed and left zone (OnTriggerExit2D was already called).
            if (!other.isActiveAndEnabled)
            {
                // Debug.LogWarning("Destroyed object caught");
                return;
            }

            if (!FilterCollision(other, out MonoBehaviour collidedObject))
            {
                return;
            }

            // Debug.Log($"Filtered enter GameObject : {collidedObject.gameObject}. Parent: {other.transform.parent?.name}.");

            if (objectsInsideZone.ContainsKey(collidedObject)) // Has object already entered?
            {
                objectsInsideZone[collidedObject]++; // Just adding new collider of object inside zone trigger. 
                return;
            }

            //Adding new object to the zone trigger.
            objectsInsideZone.Add(collidedObject, 1);

            OnTriggerEnter?.Invoke(this, objectCollider, other, collidedObject);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            //Debug.Log($"On trigger exit for : {other.gameObject}. Parent: {other.transform.parent?.name}");

            if (!FilterCollision(other, out MonoBehaviour collidedObject))
            {
                return;
            }

            // Debug.Log($"Filtered exit GameObject : {collidedObject.gameObject}. Parent: {other.transform.parent?.name}.");

            if (!objectsInsideZone.ContainsKey(collidedObject)) //Has object already left the trigger zone? This could have been caused by misorder of trigger OnEnter and OnExit calls. 
            {
                //Doing nothing cause object already left.
                return;
            }

            //Removing objects collider that left trigger zone.
            objectsInsideZone[collidedObject]--;

            if (objectsInsideZone[collidedObject] > 0) // Are any of object's colliders still left in a trigger zone. 
            {
                //Doing nothing cause there a still some colliders.
                return;
            }

            //Removing object from trigger zone.
            objectsInsideZone.Remove(collidedObject);

            OnTriggerExit?.Invoke(this, objectCollider, other, collidedObject);
        }

        /// <summary>
        /// Does base filtering. Checks that collided object doesn't have specified tags.
        /// Also checks if collided object is a <see cref="Wagon"/> or a <see cref="GameEntity"/>.
        /// </summary>
        /// <param name="other">Collided object's collider</param>
        /// <param name="collidedObject"><see cref="GameEntity"/> or a <see cref="Wagon"/> component of filtered gameobject or null if gameobject didn't pass the filter.</param>
        /// <returns>Result of filtering. True if filter has been passed. False over-wise</returns>
        private bool FilterCollision(Collider2D other, out MonoBehaviour collidedObject)
        {
            collidedObject = null;

            if (other.gameObject.CompareTag("Detector") || other.gameObject.CompareTag("Droplet"))
            {
                return false;
            }

            MonoBehaviour collidedMonoBehaviour = other.gameObject.transform.root.gameObject.GetComponent<Wagon>();

            if (collidedMonoBehaviour == null)
            {
                collidedMonoBehaviour = other.gameObject.transform.root.gameObject.GetComponent<GameEntity>();
            }

            if (collidedMonoBehaviour == null)
            {
                return false;
            }

            collidedObject = collidedMonoBehaviour;

            return true;
        }
    }
}