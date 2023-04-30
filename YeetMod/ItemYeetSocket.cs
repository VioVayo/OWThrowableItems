﻿using System.Linq;
using UnityEngine;

namespace YeetMod
{
    public class ItemYeetSocket : MonoBehaviour
    {
        private static Mesh cylinderMesh;

        private OWRigidbody owRigidbody;
        private GameObject detectorObj = new("YeetDetector");
        private OWItem attachedItem;
        private bool switchedFromInteractible;
        private float initialVelocity;


        public static ItemYeetSocket Create(OWItem item, Vector3 startingPosition, float startingVelocity)
        {
            var socketObj = new GameObject("ItemYeetBody");
            socketObj.SetActive(false);
            socketObj.transform.position = startingPosition;
            socketObj.AddComponent<OWAudioSource>().SetTrack(OWAudioMixer.TrackName.Environment);
            var socket = socketObj.AddComponent<ItemYeetSocket>();
            socket.attachedItem = item;
            socket.initialVelocity = startingVelocity;
            socketObj.SetActive(true);
            return socket;
        }


        private void Awake()
        {
            detectorObj.layer = LayerMask.NameToLayer("PhysicalDetector");
            detectorObj.tag = "DynamicPropDetector";

            detectorObj.transform.SetParent(transform, false);
            owRigidbody = gameObject.AddComponent<OWRigidbody>();
            AddShapesAndColliders();
            detectorObj.AddComponent<DynamicForceDetector>();
            detectorObj.AddComponent<DynamicFluidDetector>();
            detectorObj.transform.SetParent(attachedItem.transform, false);

            foreach (var volume in Locator.GetPlayerBody()._attachedForceDetector._activeVolumes) owRigidbody._attachedForceDetector.AddVolume(volume);
            foreach (var volume in Locator.GetPlayerBody()._attachedFluidDetector._activeVolumes) owRigidbody._attachedFluidDetector.AddVolume(volume);
            owRigidbody._attachedFluidDetector._buoyancy = Locator.GetProbe().GetOWRigidbody()._attachedFluidDetector._buoyancy;
            owRigidbody._attachedFluidDetector._splashEffects = Locator.GetProbe().GetOWRigidbody()._attachedFluidDetector._splashEffects;

            gameObject.AddComponent<ImpactSensor>();
            var objectImpactAudio = gameObject.AddComponent<ObjectImpactAudio>();
            objectImpactAudio._minPitch = 0.4f;
            objectImpactAudio._maxPitch = 0.6f;
            objectImpactAudio.Reset();

            attachedItem.onPickedUp += OnPickUpItem;

            owRigidbody._rigidbody.angularDrag = 5;
            owRigidbody.SetMass(0.001f);
            owRigidbody.SetVelocity(Locator.GetPlayerBody().GetPointVelocity(transform.position) + Locator.GetPlayerCamera().transform.forward * initialVelocity);
        }

        private void Start() //stuff that has to happen after DropItemInstantly
        {
            owRigidbody.SetCenterOfMass(owRigidbody.transform.InverseTransformPoint(detectorObj.transform.position));
        }

        private void AddShapesAndColliders()
        {
            if (cylinderMesh == null)
            {
                var cylinderObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinderMesh = cylinderObj.GetComponent<MeshFilter>().sharedMesh;
                Destroy(cylinderObj);
            }

            switch (attachedItem._type)
            {
                case ItemType.Scroll:
                    var scrollShape = detectorObj.AddComponent<CapsuleShape>();
                    var scrollCollider = detectorObj.AddComponent<CapsuleCollider>();
                    scrollCollider.height = 0.8f;
                    scrollCollider.radius = 0.2f;
                    scrollShape.CopySettingsFromCollider();
                    scrollShape.height += 2 * scrollShape.radius;
                    scrollShape.SetCollisionMode(Shape.CollisionMode.Detector);
                    OrientDetector(new Vector3(0, 0, -0.37f), Quaternion.Euler(90, 0, 0));
                    break;

                case ItemType.WarpCore:
                    var warpCore = attachedItem as WarpCoreItem;
                    if (warpCore.IsVesselCoreType())
                    {
                        var awcShape = detectorObj.AddComponent<BoxShape>();
                        var awcCollider = detectorObj.AddComponent<BoxCollider>();
                        awcCollider.size = new Vector3(0.35f, 1.15f, 0.35f);
                        awcShape.CopySettingsFromCollider();
                        awcShape.SetCollisionMode(Shape.CollisionMode.Detector);
                        OrientDetector(Vector3.zero, Quaternion.Euler(0, 45, 0));
                    }
                    else
                    {
                        var wcShape = detectorObj.AddComponent<BoxShape>();
                        var wcCollider = detectorObj.AddComponent<BoxCollider>();
                        wcCollider.size = new Vector3(0.4f, 0.15f, 0.4f);
                        wcShape.CopySettingsFromCollider();
                        wcShape.SetCollisionMode(Shape.CollisionMode.Detector);
                        OrientDetector(new Vector3(-0.01f, -0.04f, 0), Quaternion.Euler(45, 0, 90));
                    }
                    break;

                case ItemType.SharedStone:
                    var projStoneShape = detectorObj.AddComponent<BoxShape>();
                    var projStoneCollider = detectorObj.AddComponent<BoxCollider>();
                    projStoneCollider.size = new Vector3(0.4f, 0.1f, 0.4f);
                    projStoneShape.CopySettingsFromCollider();
                    projStoneShape.SetCollisionMode(Shape.CollisionMode.Detector);
                    OrientDetector(Vector3.zero, Quaternion.identity);
                    break;

                case ItemType.ConversationStone:
                    var convStoneShape = detectorObj.AddComponent<SphereShape>();
                    var convStoneCollider = detectorObj.AddComponent<SphereCollider>();
                    convStoneCollider.center = new Vector3(0, 0.12f, 0);
                    convStoneCollider.radius = 0.24f;
                    convStoneShape.CopySettingsFromCollider();
                    convStoneShape.SetCollisionMode(Shape.CollisionMode.Detector);

                    var convStoneColliderObj = new GameObject("YeetCollider");
                    var convStonePhysicalCollider = convStoneColliderObj.AddComponent<SphereCollider>();
                    convStonePhysicalCollider.center = new Vector3(0, 0.12f, 0);
                    convStonePhysicalCollider.radius = 0.3f;
                    convStoneColliderObj.transform.SetParent(detectorObj.transform, false);
                    convStoneColliderObj.layer = LayerMask.NameToLayer("Ignore Raycast");

                    OrientDetector(new Vector3(0, -0.12f, 0), Quaternion.identity);
                    break;

                case ItemType.Lantern:
                    var lanternCapsuleShape = detectorObj.AddComponent<CapsuleShape>();
                    var lanternCapsuleCollider = detectorObj.AddComponent<CapsuleCollider>();
                    lanternCapsuleCollider.center = new Vector3(0, 0.15f, 0);
                    lanternCapsuleCollider.height = 0.4f;
                    lanternCapsuleCollider.radius = 0.2f;
                    lanternCapsuleShape.CopySettingsFromCollider();
                    lanternCapsuleShape.height += 2 * lanternCapsuleShape.radius;
                    lanternCapsuleShape.SetCollisionMode(Shape.CollisionMode.Detector);

                    var lanternColliderObj = new GameObject("YeetCollider");
                    var lanternColliderBase = lanternColliderObj.AddComponent<MeshCollider>();
                    lanternColliderBase.sharedMesh = cylinderMesh;
                    lanternColliderBase.convex = true;
                    lanternColliderObj.transform.localPosition = new Vector3(0, -0.23f, 0);
                    lanternColliderObj.transform.localScale = new Vector3(0.4f, 0.03f, 0.4f);
                    lanternColliderObj.transform.SetParent(detectorObj.transform, false);
                    lanternColliderObj.layer = LayerMask.NameToLayer("Ignore Raycast");

                    OrientDetector(new Vector3(0, 0.25f, 0), Quaternion.identity);
                    break;

                case ItemType.SlideReel:
                    var reelShape = detectorObj.AddComponent<CapsuleShape>();
                    reelShape.height = 5;
                    reelShape.SetCollisionMode(Shape.CollisionMode.Detector);

                    var reelCollider = detectorObj.AddComponent<MeshCollider>();
                    reelCollider.sharedMesh = cylinderMesh;
                    reelCollider.convex = true;
                    detectorObj.transform.localScale = new Vector3(0.8f, 0.2f, 0.8f);

                    OrientDetector(new Vector3(0, 0.2f, 0), Quaternion.identity);
                    break;

                case ItemType.DreamLantern:
                    var dreamLantern = attachedItem as DreamLanternItem;
                    var nonfunctioning = (dreamLantern.GetLanternType() == DreamLanternType.Nonfunctioning);

                    var dreamLanternShape = detectorObj.AddComponent<CapsuleShape>();
                    dreamLanternShape.height = nonfunctioning ? 4 : 5;
                    dreamLanternShape.SetCollisionMode(Shape.CollisionMode.Detector);

                    var dreamLanternCollider = detectorObj.AddComponent<MeshCollider>();
                    dreamLanternCollider.sharedMesh = cylinderMesh;
                    dreamLanternCollider.convex = true;
                    detectorObj.transform.localScale = nonfunctioning ? new Vector3(0.42f, 0.25f, 0.4f) : new Vector3(0.7f, 0.2f, 0.7f);
                    if (nonfunctioning)
                    {
                        dreamLanternShape.center = new Vector3(0, 0.7f, 0);
                        var dreamLanternCapsuleCollider = detectorObj.AddComponent<CapsuleCollider>();
                        dreamLanternCapsuleCollider.center = new Vector3(0, 1.63f, 0);
                    }
                    else
                    {
                        var dreamLanternColliderObj = new GameObject("YeetCollider");
                        var dreamLanternolliderBase = dreamLanternColliderObj.AddComponent<BoxCollider>();
                        dreamLanternolliderBase.center = new Vector3(0.12f, 0, 0);
                        dreamLanternolliderBase.size = new Vector3(1.25f, 1.2f, 0.4f);
                        if (dreamLantern.GetLanternType() == DreamLanternType.Functioning)
                        {
                            var dreamLanternColliderFront = dreamLanternColliderObj.AddComponent<BoxCollider>();
                            dreamLanternColliderFront.center = new Vector3(0, 0, 0.6f);
                            dreamLanternColliderFront.size = new Vector3(0.8f, 3.2f, 0.2f);
                        }
                        dreamLanternColliderObj.transform.SetParent(detectorObj.transform, false);
                        dreamLanternColliderObj.layer = LayerMask.NameToLayer("Ignore Raycast");
                    }

                    var pos = nonfunctioning ? new Vector3(0, 0.21f, 0) : new Vector3(0, 0.35f, 0);
                    var rot = nonfunctioning ? Quaternion.identity : Quaternion.Euler(0, 0, 90);
                    OrientDetector(pos, rot);
                    break;

                case ItemType.VisionTorch:
                    var torchCapsuleShape = detectorObj.AddComponent<CapsuleShape>();
                    var torchSphereShape = detectorObj.AddComponent<SphereShape>();
                    var torchCapsuleCollider = detectorObj.AddComponent<CapsuleCollider>();
                    var torchSphereCollider = detectorObj.AddComponent<SphereCollider>();
                    torchCapsuleCollider.center = new Vector3(0, -0.45f, -0.05f);
                    torchCapsuleCollider.height = 1.8f;
                    torchCapsuleCollider.radius = 0.1f;
                    torchSphereCollider.center = new Vector3(0, 0.45f, 0);
                    torchSphereCollider.radius = 0.4f;
                    torchCapsuleShape.CopySettingsFromCollider();
                    torchCapsuleShape.height += 2 * torchCapsuleShape.radius;
                    torchCapsuleShape.SetCollisionMode(Shape.CollisionMode.Detector);
                    torchSphereShape.CopySettingsFromCollider();
                    torchSphereShape.SetCollisionMode(Shape.CollisionMode.Detector);
                    OrientDetector(new Vector3(0, 0.45f, 0.05f), Quaternion.identity);
                    break;

                default:
                    detectorObj.layer = LayerMask.NameToLayer("AdvancedDetector");
                    switchedFromInteractible = attachedItem.gameObject.layer == LayerMask.NameToLayer("Interactible");
                    if (switchedFromInteractible) attachedItem.gameObject.layer = LayerMask.NameToLayer("Default");
                    detectorObj.AddComponent<SphereShape>().SetCollisionMode(Shape.CollisionMode.Detector);
                    detectorObj.AddComponent<SphereCollider>();
                    break;
            }
        }

        private void OrientDetector(Vector3 localPos, Quaternion localRot)
        {
            detectorObj.transform.localPosition = localPos;
            detectorObj.transform.localRotation = localRot;
        }

        private void OnPickUpItem(OWItem item)
        {
            attachedItem.onPickedUp -= OnPickUpItem;
            if (switchedFromInteractible) attachedItem.gameObject.layer = LayerMask.NameToLayer("Interactible");
            Destroy(gameObject);
            Destroy(detectorObj);
        }
    }
}
