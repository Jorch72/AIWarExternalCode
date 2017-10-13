using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public static class UtilityFunctions_Formation
    {
        public static void Helper_SendMoveCommand( GameEntity entity, ArcenPoint Destination, bool isQueuedCommand )
        {
            GameCommand command = GameCommand.Create( GameCommandType.Move );
            command.ToBeQueued = isQueuedCommand;
            command.RelatedPoint = Destination;
            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( command, true );
        }

        public static void Helper_FindAndPlaceCoreUnit( ControlGroup Group, ArcenPoint MoveOrderPoint, out ArcenSparseLookup<GameEntity, ArcenPoint> _entitiesToPlace, out GameEntity coreUnit, out int shieldCoverageRadiusOrEquivalent, out int paddingAroundEachUnit, out ArcenRectangle firstUnitRect )
        {
            Planet localPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            
            ArcenSparseLookup<GameEntity, ArcenPoint> entitiesToPlace = _entitiesToPlace = new ArcenSparseLookup<GameEntity, ArcenPoint>();
            Group.DoForEntities( delegate ( GameEntity entity )
            {
                if ( entity.Combat.Planet != localPlanet )
                    return DelReturn.Continue;
                entitiesToPlace[entity] = ArcenPoint.OutOfRange;
                return DelReturn.Continue;
            } );

            coreUnit = null;
            GameEntity backupCoreUnit = null;

            for ( int i = 0; i < entitiesToPlace.GetPairCount(); i++ )
            {
                GameEntity entity = entitiesToPlace.GetPairByIndex( i ).Key;
                if ( entity.TypeData.ShieldRadius <= 0 )
                {
                    if ( coreUnit == null )
                    {
                        if ( backupCoreUnit != null )
                        {
                            if ( entity.TypeData.BalanceStats.StrengthPerSquad <= backupCoreUnit.TypeData.BalanceStats.StrengthPerSquad )
                                continue;
                        }
                        backupCoreUnit = entity;
                    }
                    continue;
                }
                if ( coreUnit != null )
                {
                    if ( entity.TypeData.ShieldRadius < coreUnit.TypeData.ShieldRadius )
                        continue;
                    if ( entity.TypeData.ShieldRadius == coreUnit.TypeData.ShieldRadius &&
                         entity.TypeData.BalanceStats.ShieldPoints <= coreUnit.TypeData.BalanceStats.ShieldPoints )
                        continue;
                }
                coreUnit = entity;
            }

            if ( coreUnit == null )
                backupCoreUnit = coreUnit;

            int initialCoreRadius = 4;
            shieldCoverageRadiusOrEquivalent = initialCoreRadius;
            if ( coreUnit != null )
            {
                entitiesToPlace[coreUnit] = MoveOrderPoint;
                initialCoreRadius = coreUnit.TypeData.Radius;
                shieldCoverageRadiusOrEquivalent = Math.Max( initialCoreRadius, coreUnit.GetCurrentShieldRadius() );
            }

            paddingAroundEachUnit = 20;
            firstUnitRect.Width = initialCoreRadius + initialCoreRadius + paddingAroundEachUnit;
            firstUnitRect.Height = firstUnitRect.Width;
            firstUnitRect.X = MoveOrderPoint.X - initialCoreRadius;
            firstUnitRect.Y = MoveOrderPoint.Y - initialCoreRadius;
        }

        public static void Helper_GetForeAndAftZoneEntities( ArcenSparseLookup<GameEntity, ArcenPoint> entitiesToPlace, out List<GameEntity> foreZoneEntities, out List<GameEntity> aftZoneEntities )
        {
            foreZoneEntities = new List<GameEntity>();
            for ( int i = 0; i < entitiesToPlace.GetPairCount(); i++ )
            {
                ArcenSparseLookupPair<GameEntity, ArcenPoint> pair = entitiesToPlace.GetPairByIndex( i );
                if ( pair.Value != ArcenPoint.OutOfRange )
                    continue;
                foreZoneEntities.Add( pair.Key );
            }

            // sorting these so that the first one we want to place is at the end, and working back from there
            foreZoneEntities.Sort( delegate ( GameEntity Left, GameEntity Right )
            {
                int val;

                int leftRange = 0;
                for ( int i = 0; i < Left.Systems.Count; i++ )
                    leftRange = Math.Max( leftRange, Left.Systems[i].BalanceStats.Range );

                int rightRange = 0;
                for ( int i = 0; i < Right.Systems.Count; i++ )
                    rightRange = Math.Max( rightRange, Right.Systems[i].BalanceStats.Range );

                val = leftRange.CompareTo( rightRange );
                if ( val != 0 ) return val;

                val = Right.TypeData.Radius.CompareTo( Left.TypeData.Radius );
                if ( val != 0 ) return val;

                val = Right.TypeData.BalanceStats.StrengthPerSquad.CompareTo( Left.TypeData.BalanceStats.StrengthPerSquad );
                if ( val != 0 ) return val;

                return Left.PrimaryKeyID.CompareTo( Right.PrimaryKeyID );
            } );

            aftZoneEntities = new List<GameEntity>();
            int entitiesToLeaveInFore = foreZoneEntities.Count / 2;
            for ( int i = foreZoneEntities.Count - 1; i > entitiesToLeaveInFore; i-- )
            {
                aftZoneEntities.Add( foreZoneEntities[i] );
                foreZoneEntities.RemoveAt( i );
            }
        }

        public static void Helper_RotatePointsAccordingToAngleFromCoreUnit( ArcenPoint MoveOrderPoint, bool isQueuedCommand, ArcenSparseLookup<GameEntity, ArcenPoint> entitiesToPlace, GameEntity coreUnit )
        {
            // rotate points according to angle from first unit to target
            Vector2 originPoint = coreUnit.WorldLocation.ToVector2();
            if ( isQueuedCommand )
            {
                for ( int i = 0; i < coreUnit.EntitySpecificOrders.QueuedOrders.Count; i++ )
                {
                    EntityOrder order = coreUnit.EntitySpecificOrders.QueuedOrders[i];
                    if ( order.TypeData.Type != EntityOrderType.Move )
                        continue;
                    originPoint = order.RelatedPoint.ToVector2();
                }
            }

            Vector2 moveOrderVectorPoint = MoveOrderPoint.ToVector2();

            NonSimAngleDegrees angle = originPoint.GetAngleToDegrees( moveOrderVectorPoint );
            NonSimAngleDegrees baseAngle = NonSimAngleDegrees.Create( 0 );
            NonSimAngleDegrees rotationAngle = angle.Add( baseAngle );
            for ( int i = 0; i < entitiesToPlace.GetPairCount(); i++ )
            {
                ArcenSparseLookupPair<GameEntity, ArcenPoint> pair = entitiesToPlace.GetPairByIndex( i );
                Vector2 destinationPoint = pair.Value.ToVector2();
                NonSimAngleDegrees subAngle = moveOrderVectorPoint.GetAngleToDegrees( destinationPoint );
                Vector2 movementVector = destinationPoint - moveOrderVectorPoint;
                float distance = movementVector.magnitude;//involves sqrt, is awful, but in interface code of this kind it's probably fine
                NonSimAngleDegrees finalAngle = rotationAngle.Add( subAngle );
                Vector2 rotatedPoint = moveOrderVectorPoint;
                rotatedPoint.x += (float)( distance * finalAngle.Cos() );
                rotatedPoint.y += (float)( distance * finalAngle.Sin() );
                entitiesToPlace[pair.Key] = rotatedPoint.ToArcenPoint();
            }
        }

        public static void Helper_ActuallyIssueMoveOrders( bool isQueuedCommand, ArcenSparseLookup<GameEntity, ArcenPoint> entitiesToPlace )
        {
            for ( int i = 0; i < entitiesToPlace.GetPairCount(); i++ )
            {
                ArcenSparseLookupPair<GameEntity, ArcenPoint> pair = entitiesToPlace.GetPairByIndex( i );
                if ( pair.Value == ArcenPoint.OutOfRange )
                    continue;
                UtilityFunctions_Formation.Helper_SendMoveCommand( pair.Key, pair.Value, isQueuedCommand );
            }
        }
    }

    public class Formation_Blob : IFormationImplementation
    {
        protected virtual bool Reverse {  get { return false; } }

        public bool HandleMovementOrder( ControlGroup Group, ArcenPoint MoveOrderPoint, bool isQueuedCommand )
        {
            ArcenSparseLookup<GameEntity, ArcenPoint> entitiesToPlace;
            GameEntity coreUnit;
            int shieldCoverageRadiusOrEquivalent, paddingAroundEachUnit;
            ArcenRectangle firstUnitRect;
            UtilityFunctions_Formation.Helper_FindAndPlaceCoreUnit( Group, MoveOrderPoint, out entitiesToPlace, out coreUnit, out shieldCoverageRadiusOrEquivalent, out paddingAroundEachUnit, out firstUnitRect );

            List<GameEntity> foreZoneEntities, aftZoneEntities;
            UtilityFunctions_Formation.Helper_GetForeAndAftZoneEntities( entitiesToPlace, out foreZoneEntities, out aftZoneEntities );

            int occupiedRadius = coreUnit.TypeData.Radius + paddingAroundEachUnit;

            int degreeBufferOnEachEndOfEachArc = 15;
            int arcWidth = 180 - ( degreeBufferOnEachEndOfEachArc * 2 );
            int foreArcCenterAngle = 0;
            int aftArcCenterAngle = foreArcCenterAngle + 180;

            if ( Reverse )
            {
                List<GameEntity> temp = foreZoneEntities;
                foreZoneEntities = aftZoneEntities;
                aftZoneEntities = temp;
            }

            occupiedRadius = Helper_PlaceRings( foreZoneEntities, entitiesToPlace, isQueuedCommand, paddingAroundEachUnit, MoveOrderPoint, occupiedRadius,
                NonSimAngleDegrees.Create( foreArcCenterAngle - ( arcWidth / 2 ) ), NonSimAngleDegrees.Create( arcWidth ) );

            // resetting this for the aft arc
            occupiedRadius = coreUnit.TypeData.Radius + paddingAroundEachUnit;

            occupiedRadius = Helper_PlaceRings( aftZoneEntities, entitiesToPlace, isQueuedCommand, paddingAroundEachUnit, MoveOrderPoint, occupiedRadius,
                NonSimAngleDegrees.Create( aftArcCenterAngle - ( arcWidth / 2 ) ), NonSimAngleDegrees.Create( arcWidth ) );

            UtilityFunctions_Formation.Helper_RotatePointsAccordingToAngleFromCoreUnit( MoveOrderPoint, isQueuedCommand, entitiesToPlace, coreUnit );

            UtilityFunctions_Formation.Helper_ActuallyIssueMoveOrders( isQueuedCommand, entitiesToPlace );

            return true;
        }

        private static int Helper_PlaceRings( List<GameEntity> entitiesToPlaceWithinZone, ArcenSparseLookup<GameEntity, ArcenPoint> overallEntitySet, bool isQueuedCommand, int paddingAroundEachUnit, ArcenPoint Center, int CurrentOccupiedRadius, NonSimAngleDegrees StartingAngle, NonSimAngleDegrees ArcWidth )
        {
            NonSimAngleRadians arcWidthRadians = ArcWidth.ToRadians();
            int unitIndex = 0;
            while ( unitIndex < entitiesToPlaceWithinZone.Count )
            {
                GameEntity firstEntityOnRing = entitiesToPlaceWithinZone[unitIndex];
                int firstUnitRadius = firstEntityOnRing.TypeData.Radius + paddingAroundEachUnit;

                int radiusOfNewRing = CurrentOccupiedRadius + firstUnitRadius;

                NonSimAngleRadians workingAngle = StartingAngle.ToRadians();
                NonSimAngleRadians sumOfArcIncreases = NonSimAngleRadians.Create( 0 );
                int lastUnitRadius = 0;
                ArcenPoint firstPointFound = ArcenPoint.OutOfRange;
                for ( ; unitIndex < entitiesToPlaceWithinZone.Count; unitIndex++ )
                {
                    GameEntity entity = entitiesToPlaceWithinZone[unitIndex];

                    int thisUnitRadius = entity.TypeData.Radius + paddingAroundEachUnit;

                    int distanceNeededFromPreviousPoint = 0;
                    if ( lastUnitRadius > 0 )
                        distanceNeededFromPreviousPoint = lastUnitRadius + thisUnitRadius;
                    lastUnitRadius = thisUnitRadius;

                    if ( distanceNeededFromPreviousPoint > 0 )
                    {
                        if ( distanceNeededFromPreviousPoint > radiusOfNewRing )
                            break;
                        if ( radiusOfNewRing <= 0 )
                            break;
                        float unitDistanceNeeded = (float)distanceNeededFromPreviousPoint / (float)radiusOfNewRing; //translating this to "distance" on the unit circle

                        //Given point A at angle M on a circle, increasing the angle by N results in another point at a distance of 2*sin(N/2) from point A
                        //D=2*sin(N/2)
                        //D/2=sin(N/2)
                        //arcsin(D/2)=N/2
                        //2*arcsin(D/2)=N
                        NonSimAngleRadians angleChangeNeeded = NonSimAngleRadians.Create( 2 * Mathf.Asin( unitDistanceNeeded / 2 ) );
                        sumOfArcIncreases = sumOfArcIncreases.Add( angleChangeNeeded );
                        if ( sumOfArcIncreases.Raw_GetIsGreaterThan( arcWidthRadians ) )
                            break; // if this would bring us past the ending angle, stop and go to next "ring" in arc
                        workingAngle = workingAngle.Add( angleChangeNeeded );
                    }

                    Vector2 pointOnCircle = Center.ToVector2();
                    pointOnCircle.x += (float)( radiusOfNewRing * workingAngle.Cos() );
                    pointOnCircle.y += (float)( radiusOfNewRing * workingAngle.Sin() );

                    ArcenPoint pointOnCircleAsArcenPoint = pointOnCircle.ToArcenPoint();

                    if ( firstPointFound == ArcenPoint.OutOfRange )
                        firstPointFound = pointOnCircleAsArcenPoint;
                    else if ( firstPointFound.GetDistanceTo( pointOnCircleAsArcenPoint, false ) < ( firstUnitRadius + thisUnitRadius ) )
                        break; // we've come full circle, and don't want to overlap
                    overallEntitySet[entity] = pointOnCircleAsArcenPoint;
                }

                CurrentOccupiedRadius = radiusOfNewRing + firstUnitRadius;
            }

            return CurrentOccupiedRadius;
        }
    }

    public class Formation_ReverseBlob : Formation_Blob
    {
        protected override bool Reverse
        {
            get
            {
                return true;
            }
        }
    }

    public class Formation_Lines : IFormationImplementation
    {
        public bool HandleMovementOrder( ControlGroup Group, ArcenPoint MoveOrderPoint, bool isQueuedCommand )
        {
            ArcenSparseLookup<GameEntity, ArcenPoint> entitiesToPlace;
            GameEntity coreUnit;
            int shieldCoverageRadiusOrEquivalent, paddingAroundEachUnit;
            ArcenRectangle firstUnitRect;
            UtilityFunctions_Formation.Helper_FindAndPlaceCoreUnit( Group, MoveOrderPoint, out entitiesToPlace, out coreUnit, out shieldCoverageRadiusOrEquivalent, out paddingAroundEachUnit, out firstUnitRect );

            List<GameEntity> foreZoneEntities, aftZoneEntities;
            UtilityFunctions_Formation.Helper_GetForeAndAftZoneEntities( entitiesToPlace, out foreZoneEntities, out aftZoneEntities );

            bool quadrantExpandsOnXAxis = false;
            bool quadrantExpandsInPositiveDirection = false;
            int quadrantMainAxisStart = firstUnitRect.Top;
            int quadrantOtherAxisStart = firstUnitRect.CenterX - shieldCoverageRadiusOrEquivalent;
            int quadrantOtherAxisEnd = firstUnitRect.CenterX + shieldCoverageRadiusOrEquivalent;

            Helper_DoPlacementWithinProjectedZone( foreZoneEntities, entitiesToPlace, isQueuedCommand, paddingAroundEachUnit,
                quadrantExpandsOnXAxis, quadrantExpandsInPositiveDirection, quadrantMainAxisStart, quadrantOtherAxisStart, quadrantOtherAxisEnd );

            quadrantExpandsInPositiveDirection = true;
            quadrantMainAxisStart = firstUnitRect.Bottom;

            Helper_DoPlacementWithinProjectedZone( aftZoneEntities, entitiesToPlace, isQueuedCommand, paddingAroundEachUnit,
                quadrantExpandsOnXAxis, quadrantExpandsInPositiveDirection, quadrantMainAxisStart, quadrantOtherAxisStart, quadrantOtherAxisEnd );

            UtilityFunctions_Formation.Helper_RotatePointsAccordingToAngleFromCoreUnit( MoveOrderPoint, isQueuedCommand, entitiesToPlace, coreUnit );
            
            UtilityFunctions_Formation.Helper_ActuallyIssueMoveOrders( isQueuedCommand, entitiesToPlace );

            return true;
        }

        private static void Helper_DoPlacementWithinProjectedZone( List<GameEntity> entitiesToPlaceWithinZone, ArcenSparseLookup<GameEntity, ArcenPoint> overallEntitySet, bool isQueuedCommand, int paddingAroundEachUnit, bool quadrantExpandsOnXAxis, bool quadrantExpandsInPositiveDirection, int quadrantMainAxisStart, int quadrantOtherAxisStart, int quadrantOtherAxisEnd )
        {
            ArcenPoint workingPoint;
            if ( quadrantExpandsOnXAxis )
            {
                workingPoint.X = quadrantMainAxisStart;
                workingPoint.Y = quadrantOtherAxisStart;
            }
            else
            {
                workingPoint.X = quadrantOtherAxisStart;
                workingPoint.Y = quadrantMainAxisStart;
            }

            int furthestPointAlongMainAxis = quadrantExpandsOnXAxis ? workingPoint.X : workingPoint.Y;
            for ( int i = 0; i < entitiesToPlaceWithinZone.Count; i++ )
            {
                GameEntity entity = entitiesToPlaceWithinZone[i];

                ArcenRectangle placementRect = ArcenRectangle.AllZerosInstance;
                placementRect.Width = entity.TypeData.Radius + entity.TypeData.Radius + paddingAroundEachUnit;
                placementRect.Height = placementRect.Width;
                placementRect.X = workingPoint.X;
                placementRect.Y = workingPoint.Y;

                int placementRectOtherAxisMax = quadrantExpandsOnXAxis ? placementRect.Bottom : placementRect.Right;
                if ( placementRectOtherAxisMax > quadrantOtherAxisEnd )
                {
                    if ( quadrantExpandsOnXAxis )
                    {
                        workingPoint.X = furthestPointAlongMainAxis;
                        workingPoint.Y = quadrantOtherAxisStart;
                    }
                    else
                    {
                        workingPoint.X = quadrantOtherAxisStart;
                        workingPoint.Y = furthestPointAlongMainAxis;
                    }
                    placementRect.X = workingPoint.X;
                    placementRect.Y = workingPoint.Y;
                    // no need to recheck, as by definition there will be room unless there's a single unit that doesn't fit within the other axis
                }

                if ( quadrantExpandsOnXAxis )
                {
                    if ( !quadrantExpandsInPositiveDirection )
                        placementRect.X -= placementRect.Width;
                }
                else
                {
                    if ( !quadrantExpandsInPositiveDirection )
                        placementRect.Y -= placementRect.Height;
                }
                
                overallEntitySet[entity] = placementRect.CalculateCenterPoint();

                if ( quadrantExpandsOnXAxis )
                {
                    if ( quadrantExpandsInPositiveDirection )
                        furthestPointAlongMainAxis = Math.Max( furthestPointAlongMainAxis, placementRect.Right );
                    else
                        furthestPointAlongMainAxis = Math.Min( furthestPointAlongMainAxis, placementRect.Left );
                    workingPoint.Y += placementRect.Height;
                }
                else
                {
                    if ( quadrantExpandsInPositiveDirection )
                        furthestPointAlongMainAxis = Math.Max( furthestPointAlongMainAxis, placementRect.Bottom );
                    else
                        furthestPointAlongMainAxis = Math.Min( furthestPointAlongMainAxis, placementRect.Top );
                    workingPoint.X += placementRect.Width;
                }
            }
        }
    }
}
