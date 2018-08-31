﻿using System;
using Playmode.Ennemy.BodyParts;
using Playmode.Ennemy.Strategies;
using Playmode.Entity.Destruction;
using Playmode.Entity.Senses;
using Playmode.Entity.Status;
using Playmode.Movement;
using Playmode.Util.Values;
using Playmode.Weapon;
using UnityEngine;

namespace Playmode.Ennemy
{
	public class EnnemyController : MonoBehaviour
	{
		[Header("Body Parts")] [SerializeField]
		private GameObject body;

		[SerializeField] private GameObject hand;
		[SerializeField] private GameObject sight;
		[SerializeField] private GameObject typeSign;
		[SerializeField] private GameObject hitParticlesPrefab;

		[Header("Type Images")] [SerializeField]
		private Sprite normalSprite;

		[SerializeField] private Sprite carefulSprite;
		[SerializeField] private Sprite cowboySprite;
		[SerializeField] private Sprite camperSprite;

		[Header("Weapons")] [SerializeField] private GameObject startingWeaponPrefab;
		[SerializeField] private GameObject shotgunPrefab;
		[SerializeField] private GameObject uziPrefab;

		[Header("Variables")] [SerializeField] public float outOfRangeRotationSpeed = 5f;

		[SerializeField] public float speed = 10f;
		public float senseRotation = 1f;

		private float randomBehaviour;
		public Health Health { get; set; }
		public bool IsUnderFire { get; set; }
		private Mover mover;
		private Destroyer destroyer;
		private EnnemySensor ennemySensor;
		private HitSensor hitSensor;
		private PickableSensor pickableSensor;
		private HandController handController;
		private SightController sightController;
		private Transform transformer;
		private TimedRotation timedRotation;
		private Vector3 vectorBetweenEnemy;

		private GameController gameController;
		private CameraController cameraController;

		private IEnnemyStrategy strategy;

		private void Awake()
		{
			ValidateSerialisedFields();
			InitializeComponent();
			CreateStartingWeapon();
		}

		private void ValidateSerialisedFields()
		{
			if (body == null)
				throw new ArgumentException("Body parts must be provided. Body is missing.");
			if (hand == null)
				throw new ArgumentException("Body parts must be provided. Hand is missing.");
			if (sight == null)
				throw new ArgumentException("Body parts must be provided. Sight is missing.");
			if (typeSign == null)
				throw new ArgumentException("Body parts must be provided. TypeSign is missing.");
			if (normalSprite == null)
				throw new ArgumentException("Type sprites must be provided. Normal is missing.");
			if (carefulSprite == null)
				throw new ArgumentException("Type sprites must be provided. Careful is missing.");
			if (cowboySprite == null)
				throw new ArgumentException("Type sprites must be provided. Cowboy is missing.");
			if (camperSprite == null)
				throw new ArgumentException("Type sprites must be provided. Camper is missing.");
			if (startingWeaponPrefab == null)
				throw new ArgumentException("StartingWeapon prefab must be provided.");
			if (shotgunPrefab == null)
				throw new ArgumentException("StartingWeapon prefab must be provided.");
			if (uziPrefab == null)
				throw new ArgumentException("StartingWeapon prefab must be provided.");
		}

		private void InitializeComponent()
		{
			Health = GetComponent<Health>();
			mover = GetComponent<RootMover>();
			destroyer = GetComponent<RootDestroyer>();
			transformer = transform.root;
			timedRotation = GetComponent<TimedRotation>();

			var rootTransform = transform.root;
			ennemySensor = rootTransform.GetComponentInChildren<EnnemySensor>();
			hitSensor = rootTransform.GetComponentInChildren<HitSensor>();
			pickableSensor = rootTransform.GetComponentInChildren<PickableSensor>();
			handController = hand.GetComponent<HandController>();
			sightController = sight.GetComponent<SightController>();
			gameController = GameObject.FindWithTag(Tags.GameController).GetComponent<GameController>();
			cameraController = GameObject.FindWithTag(Tags.MainCamera).GetComponent<CameraController>();
			
		}

		private void CreateStartingWeapon()
		{
			handController.Hold(Instantiate(
				startingWeaponPrefab,
				Vector3.zero,
				Quaternion.identity
			));
		}

		private void OnEnable()
		{
			timedRotation.OnRotationChanged += OnRotationChanged;
			hitSensor.OnHit += OnHit;
			Health.OnDeath += OnDeath;
			pickableSensor.OnPickUp += OnPickUp;
		}

		private void OnRotationChanged()
		{
			IsUnderFire = false;
			randomBehaviour = UnityEngine.Random.Range(-1, 2);
			senseRotation *= -1;
		}

		private void Update()
		{
			strategy.Act();
		}

		public void Roam()
		{
			mover.Move(new Vector3(0, speed * Time.deltaTime));
			handController.transform.rotation = transformer.rotation;
			sightController.transform.rotation = transformer.rotation;
		
			if (gameController.IsObjectOutOfMap(transformer.gameObject))
			{
				transformer.rotation = Quaternion.Slerp(transformer.rotation, RotationToGo(),
					Time.deltaTime * outOfRangeRotationSpeed);
			}
			else if (randomBehaviour > 0)
			{
				mover.Rotate(senseRotation);
			}
		}

		private Quaternion RotationToGo()
		{
			var rotationDown = Quaternion.Euler(0, 0, 180);
			var rotationUp = Quaternion.Euler(0, 0, 0);
			var rotationLeft = Quaternion.Euler(0, 0, 90);
			var rotationRight = Quaternion.Euler(0, 0, -90);

			if (transformer.position.y >= cameraController.CameraHalfHeight)
				return rotationDown;
			if (transformer.position.y <= -cameraController.CameraHalfHeight)
				return rotationUp;
			return transformer.position.x >= cameraController.CameraHalfWidth ? rotationLeft : rotationRight;
		}

		public void HitReact()
		{
			mover.Rotate(Mover.Clockwise);
		}

		private void OnDisable()
		{
			hitSensor.OnHit -= OnHit;
			Health.OnDeath -= OnDeath;
			pickableSensor.OnPickUp -= OnPickUp;
		}

		public void Configure(EnnemyStrategy strategy, Color color)
		{
			body.GetComponent<SpriteRenderer>().color = color;
			sight.GetComponent<SpriteRenderer>().color = color;

			switch (strategy)
			{
				case EnnemyStrategy.Careful:
					typeSign.GetComponent<SpriteRenderer>().sprite = carefulSprite;
					this.strategy = new CarefulStrategy(mover, ennemySensor, transformer, this,  gameController,handController, pickableSensor);
					break;
				case EnnemyStrategy.Cowboy:
					typeSign.GetComponent<SpriteRenderer>().sprite = cowboySprite;
					this.strategy = new CowboyStrategy(mover,ennemySensor, transformer, this, pickableSensor);
					break;
				case EnnemyStrategy.Camper:
					typeSign.GetComponent<SpriteRenderer>().sprite = camperSprite;
					this.strategy = new CamperStrategy(mover, ennemySensor, transformer, this,gameController,pickableSensor);
					break;
				default:
					typeSign.GetComponent<SpriteRenderer>().sprite = normalSprite;
					this.strategy = new NormalStrategy(mover, handController, ennemySensor, transformer, timedRotation, this);
					break;
			}
		}

		private void OnHit(int hitPoints)
		{
			Health.Hit(hitPoints);
			IsUnderFire = true;

			Instantiate(hitParticlesPrefab, transformer.position, transformer.rotation);
		}

		private void OnDeath()
		{
			destroyer.Destroy();
		}

		private void OnPickUp(GameObject pickable)
		{
			var type = pickable.GetComponentInChildren<PickableType>().GetType();

			Debug.Log("Item picked up: " + type);

			switch (type)
			{
				case PickableTypes.Shotgun:
					HoldWeapon(shotgunPrefab);
					break;
				case PickableTypes.Uzi:
					HoldWeapon(uziPrefab);
					break;
			}

			pickable.gameObject.GetComponentInChildren<PickableUse>().Use(gameObject);
			Destroy(pickable.gameObject);
		}

		public void ShootTowardsTarget(Transform target)
		{
			handController.AimTowards(target.gameObject);
			handController.Use();
		}

		private void HoldWeapon(GameObject weaponToHold)
		{
			handController.Hold(Instantiate(
				weaponToHold,
				Vector3.zero,
				Quaternion.identity
			));
		}
	}
}