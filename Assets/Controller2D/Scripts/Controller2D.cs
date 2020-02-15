﻿using UnityEngine;

namespace net.fiveotwo.characterController
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Controller2D : MonoBehaviour
    {
        [SerializeField]
        [Range(0.01f, 0.5f)]
        protected float skinWidth = 0.01f;
        [SerializeField]
        [Range(0.001f, 0.1f)]
        protected float minimumMoveDistance = 0.001f;
        [SerializeField]
        protected LayerMask solidMask;
        [SerializeField]
        protected bool logCollisions = false;
        protected BoxCollider2D boxCollider2D;
        private CollisionState collisionState, lastCollisionState;
        private Bounds boundingBox;
        public delegate void TriggerEvent(Collider2D collision);
        public TriggerEvent onTriggerEnter, onTriggerStay, onTriggerExit;

        void Awake()
        {
            boxCollider2D = GetComponent<BoxCollider2D>();
            collisionState = lastCollisionState = new CollisionState();
            collisionState.Reset();
            UpdateCollisionBoundaries();
        }

        protected RaycastHit2D CastBox(Vector2 origin, Vector2 size, Vector2 direction, float distance, LayerMask mask)
        {
            Vector2 compensatedOrigin = new Vector2(origin.x - size.x * 0.5f, origin.y + size.y * 0.5f);
            DebugDrawRectangle(compensatedOrigin, size, Color.red);
            DebugDrawRectangle(compensatedOrigin + direction * distance, size, Color.red);
            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0, direction, distance, mask);
            if (hit)
            {
                Vector2 newOrigin = new Vector2(hit.centroid.x - size.x * 0.5f, hit.centroid.y + size.y * 0.5f);
                DebugDrawRectangle(newOrigin, size, Color.cyan);
            }
            return hit;
        }

        private float CastLenght(float value)
        {
            if (Mathf.Abs(value) < skinWidth)
            {
                return 2 * skinWidth;
            }
            return value;
        }

        private float VerticalCollision(Vector2 deltaStep, Bounds boundingBox)
        {
            float direction = Mathf.Sign(deltaStep.y);
            float castLength = CastLenght(Mathf.Abs(deltaStep.y));
            float extends = boundingBox.extents.y;
            float halfExtends = boundingBox.extents.y * 0.5f;
            float initialDistance = halfExtends * direction;
            Vector2 size = new Vector2(boundingBox.size.x, extends + skinWidth);
            RaycastHit2D hit = CastBox(transform.position + new Vector3(0, initialDistance), size, Vector2.up * direction, castLength, solidMask);

            if (!hit)
            {
                return deltaStep.y;
            }

            collisionState.above = direction > 0 ? true : false;
            collisionState.below = direction < 0 ? true : false;

            float distance = hit.distance * direction;
            if (Mathf.Abs(distance) < minimumMoveDistance)
            {
                return 0;
            }
            float compensatedDistance = distance + skinWidth * direction;

            return Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;
        }

        private float HorizontalCollision(Vector2 deltaStep, Bounds boundingBox)
        {
            float direction = Mathf.Sign(deltaStep.x);
            float castLength = CastLenght(Mathf.Abs(deltaStep.x));
            float extends = boundingBox.extents.x;
            float halfExtends = extends * 0.5f;
            float initialDistance = halfExtends * direction;
            Vector2 size = new Vector2(extends + skinWidth, boundingBox.size.y);
            RaycastHit2D hit = CastBox(transform.position + new Vector3(initialDistance, 0), size, Vector2.right * direction, castLength, solidMask);

            if (!hit)
            {
                return deltaStep.x;
            }

            collisionState.right = direction > 0 ? true : false;
            collisionState.left = direction < 0 ? true : false;

            float distance = (hit.distance * direction);

            if (Mathf.Abs(distance) < minimumMoveDistance)
            {
                return 0;
            }
            float compensatedDistance = distance + skinWidth * direction;

            return Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;
        }

        public void Move(Vector3 deltaStep)
        {
            bool noCollisionLastTime = lastCollisionState.NoCollision();
            collisionState.Reset();

            if (deltaStep.y != 0)
            {
                deltaStep.y = VerticalCollision(deltaStep, boundingBox);
                transform.Translate(new Vector3(0, deltaStep.y));
            }

            if (deltaStep.x != 0)
            {
                deltaStep.x = HorizontalCollision(deltaStep, boundingBox);
                transform.Translate(new Vector3(deltaStep.x, 0));
            }

            lastCollisionState = collisionState;
            if (logCollisions)
            {
                collisionState.Log();
            }
        }

        public CollisionState CollisionState()
        {
            return collisionState;
        }

        public void UpdateCollisionBoundaries()
        {
            boundingBox = new Bounds(Vector3.zero, boxCollider2D.size);
            boundingBox.Expand(-2f * skinWidth);
        }

        private void DebugDrawRectangle(Vector3 position, Vector2 size, Color color)
        {
            Debug.DrawLine(position, new Vector3(position.x + size.x, position.y, position.z), color);
            Debug.DrawLine(position, new Vector3(position.x, position.y - size.y, position.z), color);
            Debug.DrawLine(new Vector3(position.x, position.y - size.y, position.z), new Vector3(position.x + size.x, position.y - size.y, position.z), color);
            Debug.DrawLine(new Vector3(position.x + size.x, position.y - size.y, position.z), new Vector3(position.x + size.x, position.y, position.z), color);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            onTriggerEnter?.Invoke(collision);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            onTriggerStay?.Invoke(collision);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            onTriggerExit?.Invoke(collision);
        }
    }
}