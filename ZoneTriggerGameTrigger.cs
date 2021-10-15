using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using com.pigsels.tools;
using UnityEngine;

namespace com.pigsels.BubbleTrouble
{
    /// <summary>
    /// Triggers when wagon collides with scenes <see cref="TriggerZone"/>.
    /// Allows filtering for collided <see cref="TriggerZone"/> and collided object.
    /// </summary>
    [EditorGroup("Game Level")]
    public class ZoneTriggerGameTrigger : GameTriggerSO
    {
#region Serialized fields

        /// <summary>
        /// Determines if filtering for collided zone must be applied.
        /// </summary>
        [Tooltip("Should objects be filetered by zone name when collided?")]
        public FilterMode filterTriggersZoneName = FilterMode.Any;

        /// <summary>
        /// Regex pattern used for zone name filtering if <see cref="filterTriggersZoneName"/> is true.
        /// </summary>
        [ShowIf("filterTriggersZoneName != Any"), ChangeNesting(1)]
        [Icon("warning", "Pattern to match zone name. This is the regular expression string. Click the icon for details.", null, RegexHintURL)]
        public string triggerZoneNamePattern;

        /// <summary>
        /// Determines if filtering for collided object type must be applied.
        /// </summary>
        [Tooltip("Should objects be filetered by their type when collided?")]
        public FilterMode filterObjectType = FilterMode.Any;

        /// <summary>
        ///Determines if collisions with wagon must be detected. Used in case when <see cref="filterObjectType"/> is true.
        /// </summary>
        [ShowIf("filterObjectType != Any"), ChangeNesting(1)]
        public bool reactToWagonCollision;

        /// <summary>
        ///Determines if collisions with specified <see cref="GameEntity"/> must be detected. Used in case when <see cref="filterObjectType"/> is true.
        /// </summary>
        [ShowIf("filterObjectType != Any"), ChangeNesting(1)]
        public GameEntityList EntityList;

        /// <summary>
        /// Determines if filtering for collided object name must be applied.
        /// </summary>
        [Tooltip("Should objects be filetered by their name when collided?")]
        public FilterMode filterObjectName = FilterMode.Any;

        /// <summary>
        /// Regex pattern used for object name filtering if <see cref="filterObjectName"/> is true.
        /// </summary>
        [ShowIf("filterObjectName != Any"), ChangeNesting(1)]
        [Icon("warning", "Pattern to match gameobject name. This is the regular expression string. Click the icon for details.", null, RegexHintURL)]
        public string gameObjectNamePattern;

        /// <summary>
        /// Determines if trigger must be fired on zones enter events.
        /// </summary>
        [Tooltip("Should the trigger be activated when an object enters the trigger zone.")]
        public bool triggerOnZoneEnter;

        /// <summary>
        /// Determines if trigger must be fired on zones leave events.
        /// </summary>
        [Tooltip("Should the trigger be activated when an object leaves the trigger zone.")]
        public bool triggerOnZoneLeave;

        /// <summary>
        /// Determines if all filtered objects must be treated as one.
        /// In this case OnZoneExit will trigger only when zone is empty and OnZoneEnter will trigger only if the first object has entered.
        /// </summary>
        [Tooltip("Whether all filtered objects must be treated as one. When enabled, ZoneEnter will be triggered only when first object enters the zone and the last leaves it.")]
        public bool treatObjectsAsOne = false;

#endregion


#region Dynamic fields

        private List<TriggerZone> TriggerZones;

        /// <summary>
        /// Number of filtered objects inside the zone. Works in pair with <see cref="treatObjectsAsOne"/>
        /// </summary>
        private int zoneObjectsCount = 0;

#endregion


        public override void Init()
        {
            base.Init();

            //TriggerZones = GameObject.FindObjectsOfType<TriggerZone>();
            TriggerZones = HelperTools.GetSceneBehaviors<TriggerZone>(false);

            foreach (var triggerZone in TriggerZones)
            {
                triggerZone.OnTriggerEnter += HandleTriggerEnter;
                triggerZone.OnTriggerExit += HandleTriggerExit;
            }
        }

        public override void Deinit()
        {
            foreach (var triggerZone in TriggerZones)
            {
                triggerZone.OnTriggerEnter -= HandleTriggerEnter;
                triggerZone.OnTriggerExit -= HandleTriggerExit;
            }
        }

        /// <summary>
        /// Handler of the collision enter event
        /// </summary>
        /// <param name="triggerZone">collided zone</param>
        /// <param name="collider1">collided zone collider</param>
        /// <param name="collider2">collided object collider</param>
        private void HandleTriggerEnter(TriggerZone triggerZone, Collider2D collider1, Collider2D collider2, MonoBehaviour collidedMonoBehaviour)
        {
            //Debug.Log("Handle trigger enter");

            if (DoFilter(triggerZone, collidedMonoBehaviour))
            {
                zoneObjectsCount++;

                if (triggerOnZoneEnter)
                {
                    if (!treatObjectsAsOne || zoneObjectsCount == 1)
                    {
                        Trigger(collidedMonoBehaviour);
                    }
                }
            }
        }

        /// <summary>
        /// Handler of the collision leave event
        /// </summary>
        /// <param name="triggerZone">collided zone</param>
        /// <param name="collider1">collided zone collider</param>
        /// <param name="collider2">collided object collider</param>
        private void HandleTriggerExit(TriggerZone triggerZone, Collider2D collider1, Collider2D collider2, MonoBehaviour collidedMonoBehaviour)
        {
            if (DoFilter(triggerZone, collidedMonoBehaviour))
            {
                zoneObjectsCount--;

                if (triggerOnZoneLeave)
                {
                    if (!treatObjectsAsOne || zoneObjectsCount == 0)
                    {
                        Trigger(collidedMonoBehaviour);
                    }
                }
            }
        }

        /// <summary>
        /// Does required filtering for collided object and trigger zone.
        /// </summary>
        /// <param name="triggerZone"></param>
        /// <param name="collidedMonoBehaviour"></param>
        /// <returns>True if filter control succeeds. False over-wise</returns>
        private bool DoFilter(TriggerZone triggerZone, MonoBehaviour collidedMonoBehaviour)
        {
            if (filterTriggersZoneName != FilterMode.Any)
            {
                Regex zoneNameRegex = new Regex(triggerZoneNamePattern);

                if ((filterTriggersZoneName == FilterMode.Require && !zoneNameRegex.IsMatch(triggerZone.gameObject.name))
                    || (filterTriggersZoneName == FilterMode.Exclude && zoneNameRegex.IsMatch(triggerZone.gameObject.name)))
                {
                    return false;
                }
            }

            if (filterObjectType != FilterMode.Any)
            {
                if (collidedMonoBehaviour is Wagon)
                {
                    if ((filterObjectType == FilterMode.Require && !reactToWagonCollision)
                        || (filterObjectType == FilterMode.Exclude && reactToWagonCollision))
                    {
                        return false;
                    }
                }
                else // at this point collided monobeh is a GameEntity cause ZoneTrigger fires collisions only with either Wagon or GameEntity.
                {
                    if ((filterObjectType == FilterMode.Require && !EntityList.selectedEntities.Contains(collidedMonoBehaviour.GetType().AssemblyQualifiedName))
                        || (filterObjectType == FilterMode.Exclude && EntityList.selectedEntities.Contains(collidedMonoBehaviour.GetType().AssemblyQualifiedName)))
                    {
                        return false;
                    }
                }
            }

            if (filterObjectName != FilterMode.Any)
            {
                Regex gameobjectNameRegex = new Regex(gameObjectNamePattern);

                if ((filterObjectName == FilterMode.Require && !gameobjectNameRegex.IsMatch(collidedMonoBehaviour.gameObject.name))
                    || (filterObjectName == FilterMode.Exclude && gameobjectNameRegex.IsMatch(collidedMonoBehaviour.gameObject.name)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Fires trigger.
        /// Sets event data such as collided object position and collided entities.
        /// </summary>
        /// <param name="collidedCollider"></param>
        private void Trigger(MonoBehaviour collidedObject)
        {
            //Debug.Log($"Doing TRIGGER for collider {collidedObject.gameObject}");

            SetEventPoint(collidedObject.gameObject.transform.position);

            if (collidedObject is GameEntity)
            {
                SetEventGameEntities(new List<GameEntity>() {(GameEntity)collidedObject});
            }

            Activate();
        }
    }
}