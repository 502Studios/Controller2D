﻿using UnityEngine;

namespace raia.characterController
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Controller2D : MonoBehaviour
    {
        [SerializeField]
        [Range(0.01f, 0.05f)]
        protected float skinWidth = 0.01f;
        [SerializeField]
        [Range(0.001f, 0.1f)]
        protected float minimumMoveDistance = 0.001f;
        [SerializeField]
        protected LayerMask solidMask;
        [SerializeField]
        protected bool manageSlopes = false;
        [SerializeField]
        [Range(10f, 90f)]
        protected float maxSlopeAngle = 45f;
        protected float stepsMaximumHeight = 0.01f;
        [SerializeField]
        protected bool logCollisions = false;
        protected BoxCollider2D boxCollider2D;
        private CollisionState collisionState, lastCollisionState;
        private Bounds boundingBox;
        private Vector2 lastStep;
        public delegate void TriggerEvent(Collider2D collision);
        public TriggerEvent onTriggerEnter, onTriggerStay, onTriggerExit;

        void Awake()
        {
            boxCollider2D = GetComponent<BoxCollider2D>();
            collisionState = lastCollisionState = new CollisionState();
            collisionState.Reset();
            UpdateCollisionBoundaries();
        }

        protected RaycastHit2D CastBox(Vector2 origin, Vector2 size, Vector2 direction, float distance, LayerMask mask, float angle = 0)
        {
            Vector2 compensatedOrigin = new Vector2(origin.x - size.x * 0.5f, origin.y + size.y * 0.5f);
            DebugDrawRectangle(compensatedOrigin, size, angle != 0 ? Color.yellow : Color.red);
            DebugDrawRectangle(compensatedOrigin + direction * distance, size, angle != 0 ? Color.yellow : Color.red);
            RaycastHit2D hit = Physics2D.BoxCast(origin, size, angle, direction, distance, mask);
            if (hit)
            {
                Vector2 newOrigin = new Vector2(hit.centroid.x - size.x * 0.5f, hit.centroid.y + size.y * 0.5f);
                DebugDrawRectangle(newOrigin, size, angle != 0 ? Color.green : Color.cyan);
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

        private void VerticalCollision(Vector2 deltaStep, Bounds boundingBox, out float step, out float angle)
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
                step = deltaStep.y;
                angle = 0;
            } else
            {
                collisionState.above = direction > 0 ? true : false;
                collisionState.below = direction < 0 ? true : false;

                angle = Vector2.Angle(hit.normal, Vector3.up);

                float distance = hit.distance * direction;
                if (Mathf.Abs(distance) < minimumMoveDistance)
                {
                    step = 0;
                }
                float compensatedDistance = distance + skinWidth * direction;

                step = Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;
            }
        }

        private void HorizontalCollision(Vector2 deltaStep, Bounds boundingBox, out float step, out float angle)
        {
            float direction = Mathf.Sign(deltaStep.x);
            float castLength = CastLenght(Mathf.Abs(deltaStep.x));
            float extends = boundingBox.extents.x;
            float extendsY = boundingBox.extents.y;
            float halfExtends = extends * 0.5f;
            float initialDistance = halfExtends * direction;
            float edgeY = transform.position.y - extendsY;
            Vector2 size = new Vector2(extends + skinWidth, boundingBox.size.y - skinWidth);
            RaycastHit2D hit = CastBox(transform.position + new Vector3(initialDistance, 0), size, Vector2.right * direction, castLength, solidMask);

            if (!hit)
            {
                step = deltaStep.x;
                angle = 0;
            } else
            {
                collisionState.right = direction > 0 ? true : false;
                collisionState.left = direction < 0 ? true : false;

                angle = Vector2.Angle(hit.normal, Vector3.up);

                float distance = (hit.distance * direction);

                if (Mathf.Abs(distance) < minimumMoveDistance)
                {
                    step = 0;
                }
                float compensatedDistance = distance + skinWidth * direction;

                step = Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;
            }
        }

        private void DiagonalCollision(Vector2 deltaStep, Bounds boundingBox, float movingAngle, out Vector2 step)
        {
            float direction = Mathf.Sign(deltaStep.x);
            float castLength = CastLenght(Mathf.Abs(deltaStep.x));
            float extends = boundingBox.extents.x;
            float extendsY = boundingBox.extents.y;
            float halfExtends = extends * 0.5f;
            float halfExtendsY = extendsY * 0.5f;
            float initialDistance = extends * direction;
            float edgeY = transform.position.y - extendsY;
            float edgeX = transform.position.x + extends * direction;
            float edgeCompensatedX = transform.position.x + halfExtends * direction;
            float edgeCompensatedY = transform.position.y - halfExtendsY;
            Vector2 size = new Vector2(boundingBox.size.x - skinWidth, boundingBox.size.y - skinWidth);
            size *= 0.9f;
            Vector3 directionVector = Quaternion.AngleAxis(movingAngle * direction, Vector3.forward) * (Vector2.right * direction);

            for (float y = 0; y < 3; y ++) {
                Debug.DrawRay(new Vector2(edgeCompensatedX, edgeY + y * skinWidth), (Vector2.right * direction), Color.magenta);
                RaycastHit2D ray = Physics2D.Raycast(new Vector2(edgeCompensatedX, edgeY + y * skinWidth), (Vector2.right * direction), boundingBox.size.x, solidMask);
                if (ray)
                {
                    Vector2 newPos = transform.position;
                    newPos.y = ray.point.y + extendsY + skinWidth;
                    newPos.x = ray.point.x - ((extends + skinWidth) * direction);
                    transform.position = newPos;
                    collisionState.below = true;
                }
            }
            RaycastHit2D hit = CastBox(transform.position + new Vector3(0, skinWidth), size, directionVector, castLength, solidMask, movingAngle);
            if (!hit)
            {
                step = Quaternion.AngleAxis(movingAngle * direction, Vector3.forward) * deltaStep;
                step.y += skinWidth;
            } else {
                float distance = (hit.distance * direction);
                if (Mathf.Abs(distance) < minimumMoveDistance)
                {
                    step = Vector2.zero;
                }
                float compensatedDistance = distance + skinWidth * direction;
                float newDistance = Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;
                step = Quaternion.AngleAxis(movingAngle * direction, Vector3.forward) * (Vector2.right * direction * newDistance);
                step.y += skinWidth;
            }
        }

        public Vector2 Move(Vector3 deltaStep)
        {
            bool noCollisionLastTime = lastCollisionState.NoCollision();
            collisionState.Reset();

            if (deltaStep.y != 0)
            {
                VerticalCollision(deltaStep, boundingBox, out float step, out float angle);
                transform.Translate(new Vector3(0, step));
            }

            if (deltaStep.x != 0)
            {
                HorizontalCollision(deltaStep, boundingBox, out float step, out float angle);
                if (manageSlopes && angle != 0 && angle <= maxSlopeAngle)
                {
                    DiagonalCollision(deltaStep, boundingBox, angle, out Vector2 resultStep);
                    transform.Translate(resultStep);
                } else {
                    transform.Translate(new Vector3(step, 0));
                }
            }

            lastCollisionState = collisionState;
            if (logCollisions)
            {
                collisionState.Log();
            }

            return lastStep;
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