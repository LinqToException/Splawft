using UnityEngine;

namespace Splawft
{
    public partial class UnityDumper
    {
        private void AddBoxCollider(BoxCollider bc)
        {
            if (!dumped.Add(bc))
                return;

            sb.AppendLine($@"--- !u!65 &{bc.GetInstanceID()}
BoxCollider:
  m_ObjectHideFlags: {(int)bc.hideFlags}
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInternal: {{fileID: 0}}
  m_GameObject: {{fileID: {bc.gameObject.GetInstanceID()}}}
  m_Material: {{fileID: 0}}
  m_IsTrigger: {(bc.isTrigger ? 1 : 0)}
  m_Enabled: {(bc.enabled ? 1 : 0)}
  serializedVersion: 2
  m_Size: {Stringify(bc.size)}
  m_Center: {Stringify(bc.center)}");
            AddGameObject(bc.gameObject);
        }

        private void AddRigidbody(Rigidbody r)
        {
            if (!dumped.Add(r))
                return;

            sb.AppendLine($@"--- !u!54 &{r.GetInstanceID()}
Rigidbody:
  m_ObjectHideFlags: {(int)r.hideFlags}
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInternal: {{fileID: 0}}
  m_GameObject: {{fileID: {r.gameObject.GetInstanceID()}}}
  serializedVersion: 2
  m_Mass: {r.mass}
  m_Drag: {r.drag}
  m_AngularDrag: {r.angularDrag}
  m_UseGravity: {(r.useGravity ? 1 : 0)}
  m_IsKinematic: {(r.isKinematic ? 1 : 0)}
  m_Interpolate: {(int)r.interpolation}
  m_Constraints: {(int)r.constraints}
  m_CollisionDetection: {(int)r.collisionDetectionMode}");
            AddGameObject(r.gameObject);
        }

        private void AddCapsuleCollider(CapsuleCollider col)
        {
            if (!dumped.Add(col))
                return;

            sb.AppendLine($@"--- !u!136 &{col.GetInstanceID()}
CapsuleCollider:
  m_ObjectHideFlags: {(int)col.hideFlags}
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInternal: {{fileID: 0}}
  m_GameObject: {{fileID: {col.gameObject.GetInstanceID()}}}
  m_Material: {{fileID: 0}}
  m_IsTrigger: {(col.isTrigger ? 1 : 0)}
  m_Enabled: {(col.enabled ? 1 : 0)}
  m_Radius: {col.radius}
  m_Height: {col.height}
  m_Direction: {col.direction}
  m_Center: {Stringify(col.center)}");
        }
    }
}
