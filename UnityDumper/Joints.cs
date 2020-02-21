using UnityEngine;

namespace Splawft
{
    public partial class UnityDumper
    {
        private void AddHingeJoint(HingeJoint joint)
        {
            if (!dumped.Add(joint))
                return;

            sb.AppendLine($@"--- !u!153 &{joint.GetInstanceID()}
ConfigurableJoint:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInternal: {{fileID: 0}}
  m_GameObject: {{fileID: {joint.gameObject.GetInstanceID()}}}
  m_ConnectedBody: {{fileID: {joint.connectedBody?.GetInstanceID() ?? 0}}}
  m_Anchor: {Stringify(joint.anchor)}
  m_Axis: {Stringify(joint.axis)}
  m_AutoConfigureConnectedAnchor: {(joint.autoConfigureConnectedAnchor ? 1 : 0)}
  m_ConnectedAnchor: {Stringify(joint.connectedAnchor)}
  m_UseSpring: {(joint.useSpring ? 1 : 0)}
  m_Spring:
    spring: {joint.spring.spring}
    damper: {joint.spring.damper}
    targetPosition: {joint.spring.targetPosition}
  m_UseMotor: {(joint.useMotor ? 1 : 0)}
  m_Motor:
    targetVelocity: {joint.motor.targetVelocity}
    force: {joint.motor.force}
    freeSpin: {(joint.motor.freeSpin ? 1 : 0)}
  m_UseLimits: {(joint.useLimits ? 1 : 0)}
  m_Limits:
    min: {joint.limits.min}
    max: {joint.limits.max}
    bounciness: {joint.limits.bounciness}
    bounceMinVelocity: {joint.limits.bounceMinVelocity}
    contactDistance: {joint.limits.contactDistance}
  m_BreakForce: {joint.breakForce}
  m_BreakTorque: {joint.breakTorque}
  m_EnableCollision: {(joint.enableCollision ? 1 : 0)}
  m_EnablePreprocessing: {(joint.enablePreprocessing ? 1 : 0)}
  m_MassScale: {joint.massScale}
  m_ConnectedMassScale: {joint.connectedMassScale}");
        }

        private void AddConfigurableJoint(ConfigurableJoint cj)
        {
            if (!dumped.Add(cj))
                return;

            sb.AppendLine($@"--- !u!153 &{cj.GetInstanceID()}
ConfigurableJoint:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInternal: {{fileID: 0}}
  m_GameObject: {{fileID: {cj.gameObject.GetInstanceID()}}}
  m_ConnectedBody: {{fileID: {cj.connectedBody?.GetInstanceID() ?? 0}}}
  m_Anchor: {Stringify(cj.anchor)}
  m_Axis: {Stringify(cj.axis)}
  m_AutoConfigureConnectedAnchor: {(cj.autoConfigureConnectedAnchor ? 1 : 0)}
  m_ConnectedAnchor: {Stringify(cj.connectedAnchor)}
  serializedVersion: 2
  m_SecondaryAxis: {Stringify(cj.secondaryAxis)}
  m_XMotion: {(int)cj.xMotion}
  m_YMotion: {(int)cj.yMotion}
  m_ZMotion: {(int)cj.zMotion}
  m_AngularXMotion: {(int)cj.angularXMotion}
  m_AngularYMotion: {(int)cj.angularYMotion}
  m_AngularZMotion: {(int)cj.angularZMotion}
  m_LinearLimitSpring:
    spring: {cj.linearLimitSpring.spring}
    damper: {cj.linearLimitSpring.damper}
  m_LinearLimit:
    limit: {cj.linearLimit.limit}
    bounciness: {cj.linearLimit.bounciness}
    contactDistance: {cj.linearLimit.contactDistance}
  m_AngularXLimitSpring:
    spring: {cj.angularXLimitSpring.spring}
    damper: {cj.angularXLimitSpring.damper}
  m_LowAngularXLimit:
    limit: {cj.lowAngularXLimit.limit}
    bounciness: {cj.lowAngularXLimit.bounciness}
    contactDistance: {cj.lowAngularXLimit.contactDistance}
  m_HighAngularXLimit:
    limit: {cj.highAngularXLimit.limit}
    bounciness: {cj.highAngularXLimit.bounciness}
    contactDistance: {cj.highAngularXLimit.contactDistance}
  m_AngularYZLimitSpring:
    spring: {cj.angularYZLimitSpring.spring}
    damper: {cj.angularYZLimitSpring.damper}
  m_AngularYLimit:
    limit: {cj.angularYLimit.limit}
    bounciness: {cj.angularYLimit.bounciness}
    contactDistance: {cj.angularYLimit.contactDistance}
  m_AngularZLimit:
    limit: {cj.angularZLimit.limit}
    bounciness: {cj.angularZLimit.bounciness}
    contactDistance: {cj.angularZLimit.contactDistance}
  m_TargetPosition: {Stringify(cj.targetPosition)}
  m_TargetVelocity: {Stringify(cj.targetVelocity)}
  m_XDrive:
    serializedVersion: 3
    positionSpring: {cj.xDrive.positionSpring}
    positionDamper: {cj.xDrive.positionDamper}
    maximumForce: {cj.xDrive.maximumForce}
  m_YDrive:
    serializedVersion: 3
    positionSpring: {cj.yDrive.positionSpring}
    positionDamper: {cj.yDrive.positionDamper}
    maximumForce: {cj.yDrive.maximumForce}
  m_ZDrive:
    serializedVersion: 3
    positionSpring: {cj.zDrive.positionSpring}
    positionDamper: {cj.zDrive.positionDamper}
    maximumForce: {cj.zDrive.maximumForce}
  m_TargetRotation: {Stringify(cj.targetRotation)}
  m_TargetAngularVelocity: {Stringify(cj.targetAngularVelocity)}
  m_RotationDriveMode: {(int)cj.rotationDriveMode}
  m_AngularXDrive:
    serializedVersion: 3
    positionSpring: {cj.angularXDrive.positionSpring}
    positionDamper: {cj.angularXDrive.positionDamper}
    maximumForce: {cj.angularXDrive.maximumForce}
  m_AngularYZDrive:
    serializedVersion: 3
    positionSpring: {cj.angularYZDrive.positionSpring}
    positionDamper: {cj.angularYZDrive.positionDamper}
    maximumForce: {cj.angularYZDrive.maximumForce}
  m_SlerpDrive:
    serializedVersion: 3
    positionSpring: {cj.slerpDrive.positionSpring}
    positionDamper: {cj.slerpDrive.positionDamper}
    maximumForce: {cj.slerpDrive.maximumForce}
  m_ProjectionMode: {(int)cj.projectionMode}
  m_ProjectionDistance: {cj.projectionDistance}
  m_ProjectionAngle: {cj.projectionAngle}
  m_ConfiguredInWorldSpace: {(cj.configuredInWorldSpace ? 1 : 0)}
  m_SwapBodies: {(cj.swapBodies ? 1 : 0)}
  m_BreakForce: {cj.breakForce}
  m_BreakTorque: {cj.breakTorque}
  m_EnableCollision: {(cj.enableCollision ? 1 : 0)}
  m_EnablePreprocessing: {(cj.enablePreprocessing ? 1 : 0)}
  m_MassScale: {cj.massScale}
  m_ConnectedMassScale: {cj.connectedMassScale}");
        }
    }
}
