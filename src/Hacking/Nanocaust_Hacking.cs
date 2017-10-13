using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{

  public class Nanocaust_Hive_Hacking : BaseHackingImplementation
  {
    public override bool GetCanBeHacked( GameEntity Target, GameEntity Hacker )
    {
      if( ArcenStrings.ListContains( Target.TypeData.Tags, "NanobotHive" ))
        return true;
      return false;
    }
    public override void  DoOneSecondOfHackingLogic( GameEntity Target, GameEntity Hacker, ArcenSimContext Context )
    {
          GameEntityTypeData aberrationData = GameEntityTypeDataTable.Instance.GetRowByName( "Aberration", true, null );
          GameEntityTypeData abominationData = GameEntityTypeDataTable.Instance.GetRowByName( "Abomination", true, null );

          CombatSide cside = Target.Side;
//          ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
          ArcenPoint placementPoint = Target.neverWriteDirectly_worldLocation;
          int numToCreate = 1;
          /* How many ships to create? */
          if ( Hacker.ActiveHack_DurationThusFar >= this.GetTotalSecondsToHack( Target, Hacker ) )
            {
              if ( DoSuccessfulCompletionLogic( Target, Hacker, Context ) )
                {
                  //Set the toggle in NanocaustMgr

                  //it should probably cost hacking points. It does hurt the AI.
                  //Lore can claim that the AI is monitoring the Nanobot network and
                  //notices the trick you just used to get access to the Nanobots
                  SpecialFaction_Nanocaust.Instance.mgr.hasBeenHacked = true;
                  Hacker.Side.WorldSide.StoredHacking -= this.GetCostToHack( Target, Hacker );

                }
              }
            else
            {
                if ( Hacker.ActiveHack_DurationThusFar % 10 == 0 )
                  numToCreate = 3;
            }
            /* Create ships to fight hackers */
            for(int i = 0; i < numToCreate; i+= 2)
             {
               GameEntity.CreateNew( cside, aberrationData, placementPoint, Context );
               GameEntity.CreateNew( cside, abominationData, placementPoint, Context );
             }
        }
    public override int GetTotalSecondsToHack( GameEntity Target, GameEntity Hacker )
    {
      return 60;
    }

    public override FInt GetCostToHack( GameEntity Target, GameEntity Hacker )
    {
      //it should probably cost hacking points. It does hurt the AI.
      //Lore can claim that the AI is monitoring the Nanobot network and
      //notices the trick you just used to get access to the Nanobots
      return FInt.FromParts(000, 000);
//    return  (FInt)ExternalConstants.Instance.Balance_BaseHackingScale;
    }
    public override bool DoSuccessfulCompletionLogic( GameEntity Target, GameEntity Hacker, ArcenSimContext Context )
    {

      GameEntityTypeData hackedHiveData = GameEntityTypeDataTable.Instance.GetRowByName( "NanobotCenter_Hacked_Hive", true, null );

      //Planet hackingPlanet = SpecialFaction_Nanocaust.Instance.mgr.hivePlanet;
      CombatSide cside = Target.Side;

      //I don't see a great way to figure out the ArcenPoint of the soon-to-die target; maybe a GameEntity has an ArcenPoint? check this

      ArcenPoint placementPoint = Target.neverWriteDirectly_worldLocation;
      Target.Die( Context );
      GameEntity.CreateNew(cside, hackedHiveData, placementPoint, Context);
      return true;
    }
  }
}
